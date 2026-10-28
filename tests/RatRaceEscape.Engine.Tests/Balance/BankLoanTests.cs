namespace RatRaceEscape.Engine.Tests;

public class BankLoanTests
{
    [Fact]
    public void Borrowing_adds_cash_principal_and_a_monthly_payment()
    {
        Player baker = Baker();
        Money assets = baker.BalanceSheet.TotalAssets;
        Money liabilities = baker.BalanceSheet.TotalLiabilities;
        Money expenses = IncomeStatement.For(baker).TotalExpenses;

        Player borrower = BankLoan.Borrow(baker, 2);

        LoanLiability loan = Assert.Single(borrower.BalanceSheet.OtherLoans);
        Assert.Equal(BankLoan.Name, loan.Name);
        Assert.Equal(Money.FromDollars(2_000), loan.Balance);
        Assert.Equal(Money.FromDollars(200), loan.MonthlyPayment);
        Assert.Equal(baker.Cash + Money.FromDollars(2_000), borrower.Cash);
        Assert.Equal(assets + Money.FromDollars(2_000), borrower.BalanceSheet.TotalAssets);
        Assert.Equal(liabilities + Money.FromDollars(2_000), borrower.BalanceSheet.TotalLiabilities);
        Assert.Equal(expenses + Money.FromDollars(200), IncomeStatement.For(borrower).TotalExpenses);
        Assert.Equal(Money.FromDollars(200), IncomeStatement.For(borrower).OtherLiabilityPayments);
        Assert.Equal(CashFlow.For(baker).Monthly - Money.FromDollars(200), CashFlow.For(borrower).Monthly);
        Assert.Empty(baker.BalanceSheet.OtherLoans);
    }

    [Fact]
    public void A_second_loan_stacks_on_the_same_bank_line()
    {
        Player baker = BankLoan.Borrow(Baker(), 1);

        Player again = BankLoan.Borrow(baker, 3);

        LoanLiability loan = Assert.Single(again.BalanceSheet.OtherLoans);
        Assert.Equal(Money.FromDollars(4_000), loan.Balance);
        Assert.Equal(Money.FromDollars(400), loan.MonthlyPayment);
        Assert.Equal(baker.Cash + Money.FromDollars(3_000), again.Cash);
    }

    [Fact]
    public void Repaying_reduces_the_principal_and_the_payment_together()
    {
        Player baker = BankLoan.Borrow(Baker(), 3);
        Money expenses = IncomeStatement.For(baker).TotalExpenses;

        Player paid = BankLoan.Repay(baker, 1);

        LoanLiability loan = Assert.Single(paid.BalanceSheet.OtherLoans);
        Assert.Equal(Money.FromDollars(2_000), loan.Balance);
        Assert.Equal(Money.FromDollars(200), loan.MonthlyPayment);
        Assert.Equal(baker.Cash - Money.FromDollars(1_000), paid.Cash);
        Assert.Equal(expenses - Money.FromDollars(100), IncomeStatement.For(paid).TotalExpenses);
        Assert.Equal(3, Assert.Single(baker.BalanceSheet.OtherLoans).Balance.Cents / BankLoan.Increment.Cents);
    }

    [Fact]
    public void Repaying_the_last_increment_removes_the_loan()
    {
        Player baker = BankLoan.Borrow(Baker(), 1);
        Money expenses = IncomeStatement.For(Baker()).TotalExpenses;

        Player paid = BankLoan.Repay(baker, 1);

        Assert.Empty(paid.BalanceSheet.OtherLoans);
        Assert.Equal(baker.Cash - BankLoan.Increment, paid.Cash);
        Assert.Equal(expenses, IncomeStatement.For(paid).TotalExpenses);
        Assert.Equal(Baker().BalanceSheet.TotalLiabilities, paid.BalanceSheet.TotalLiabilities);
    }

    [Fact]
    public void A_bank_loan_leaves_other_notes_alone()
    {
        Player baker = Baker();
        BalanceSheet sheet = baker.BalanceSheet;
        Player withNote = baker.WithBalanceSheet(BalanceSheet.Create(
            sheet.Savings,
            sheet.Stocks,
            sheet.RealEstate,
            sheet.Businesses,
            sheet.Mortgage,
            sheet.CarLoan,
            sheet.CreditCard,
            [LoanLiability.Create("Harbor note", Money.FromDollars(500), Money.FromDollars(50))]));

        Player borrower = BankLoan.Borrow(withNote, 1);
        Player paid = BankLoan.Repay(borrower, 1);

        Assert.Equal(2, borrower.BalanceSheet.OtherLoans.Count);
        Assert.Equal("Harbor note", borrower.BalanceSheet.OtherLoans[0].Name);
        Assert.Equal(BankLoan.Name, borrower.BalanceSheet.OtherLoans[1].Name);
        LoanLiability note = Assert.Single(paid.BalanceSheet.OtherLoans);
        Assert.Equal("Harbor note", note.Name);
        Assert.Equal(Money.FromDollars(500), note.Balance);
    }

    [Fact]
    public void Borrow_and_repay_reject_a_bad_count_a_missing_loan_and_short_cash()
    {
        Player baker = Baker();
        Player borrower = BankLoan.Borrow(baker, 1).WithCash(Money.FromDollars(500));

        TurnException none = Assert.Throws<TurnException>(() => BankLoan.Borrow(baker, 0));
        TurnException negative = Assert.Throws<TurnException>(() => BankLoan.Repay(borrower, -1));
        TurnException missing = Assert.Throws<TurnException>(() => BankLoan.Repay(baker, 1));
        TurnException over = Assert.Throws<TurnException>(() => BankLoan.Repay(borrower, 2));
        TurnException cash = Assert.Throws<TurnException>(() => BankLoan.Repay(borrower, 1));

        Assert.Contains("Loan increments must be greater than zero.", none.Errors);
        Assert.Contains("Loan increments must be greater than zero.", negative.Errors);
        Assert.Contains("There is no bank loan to repay.", missing.Errors);
        Assert.Contains("The repayment is larger than the bank loan.", over.Errors);
        Assert.Contains("Cash cannot cover the loan repayment.", cash.Errors);
        Assert.Throws<ArgumentNullException>(() => BankLoan.Borrow(null!, 1));
        Assert.Throws<ArgumentNullException>(() => BankLoan.Repay(null!, 1));
        Assert.Equal(Money.FromDollars(1_000), Assert.Single(borrower.BalanceSheet.OtherLoans).Balance);
    }

    private static Player Baker()
    {
        return Player.Create("Ava", ProfessionCatalog.GetById("night-baker"), TokenColour.Amber);
    }
}
