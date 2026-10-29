namespace RatRaceEscape.Engine;

/// <summary>
/// The table's bank. A player borrows in increments of one thousand dollars.
/// Each increment adds one thousand dollars of cash, one thousand dollars of
/// principal, and one hundred dollars to the monthly payment. Repayment uses
/// the same increments and removes the loan when the principal is gone.
/// </summary>
public static class BankLoan
{
    public const string Name = "Bank";

    public static readonly Money Increment = Money.FromDollars(1_000);

    public static readonly Money MonthlyPaymentPerIncrement = Money.FromDollars(100);

    public static Player Borrow(Player player, int increments)
    {
        ArgumentNullException.ThrowIfNull(player);
        RequireIncrements(increments);

        Money principal = Times(Increment, increments);
        Money payment = Times(MonthlyPaymentPerIncrement, increments);
        (LoanLiability? bank, List<LoanLiability> others) = Split(player);

        Money balance = (bank?.Balance ?? Money.Zero) + principal;
        Money monthly = (bank?.MonthlyPayment ?? Money.Zero) + payment;
        others.Add(LoanLiability.Create(Name, balance, monthly));
        Player borrower = WithLoans(player, player.Cash + principal, others);
        CashFlow.RejectIfNegative(borrower, "That loan would leave monthly cash flow below zero.");
        return borrower;
    }

    public static Player Repay(Player player, int increments)
    {
        ArgumentNullException.ThrowIfNull(player);
        RequireIncrements(increments);

        (LoanLiability? bank, List<LoanLiability> others) = Split(player);
        if (bank is null)
        {
            throw new TurnException("There is no bank loan to repay.");
        }

        int outstanding = WholeIncrements(bank);
        if (increments > outstanding)
        {
            throw new TurnException("The repayment is larger than the bank loan.");
        }

        Money principal = Times(Increment, increments);
        if (player.Cash < principal)
        {
            throw new TurnException("Cash cannot cover the loan repayment.");
        }

        int left = outstanding - increments;
        if (left > 0)
        {
            others.Add(LoanLiability.Create(
                Name,
                Times(Increment, left),
                Times(MonthlyPaymentPerIncrement, left)));
        }

        return WithLoans(player, player.Cash - principal, others);
    }

    private static int WholeIncrements(LoanLiability bank)
    {
        if (bank.Balance.Cents % Increment.Cents != 0)
        {
            throw new TurnException("The bank loan is not in the usual increments.");
        }

        int increments = (int)(bank.Balance.Cents / Increment.Cents);
        Money expected = Times(MonthlyPaymentPerIncrement, increments);
        if (bank.MonthlyPayment != expected)
        {
            throw new TurnException("The bank loan is not in the usual increments.");
        }

        return increments;
    }

    private static (LoanLiability? Bank, List<LoanLiability> Others) Split(Player player)
    {
        LoanLiability? bank = null;
        List<LoanLiability> others = new(player.BalanceSheet.OtherLoans.Count);
        foreach (LoanLiability loan in player.BalanceSheet.OtherLoans)
        {
            if (loan.Name.Equals(Name, StringComparison.OrdinalIgnoreCase))
            {
                bank = loan;
                continue;
            }

            others.Add(loan);
        }

        return (bank, others);
    }

    private static Player WithLoans(Player player, Money savings, List<LoanLiability> loans)
    {
        BalanceSheet sheet = player.BalanceSheet;
        return player.WithBalanceSheet(BalanceSheet.Create(
            savings,
            sheet.Stocks,
            sheet.RealEstate,
            sheet.Businesses,
            sheet.Mortgage,
            sheet.CarLoan,
            sheet.CreditCard,
            loans));
    }

    private static void RequireIncrements(int increments)
    {
        if (increments <= 0)
        {
            throw new TurnException("Loan increments must be greater than zero.");
        }
    }

    private static Money Times(Money amount, int factor)
    {
        try
        {
            return amount * factor;
        }
        catch (OverflowException)
        {
            throw new TurnException("That loan is larger than the bank will make.");
        }
    }
}
