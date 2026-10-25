namespace RatRaceEscape.Engine.Tests;

public class IncomeStatementTests
{
    [Fact]
    public void For_a_new_player_matches_the_profession_with_no_children()
    {
        Profession baker = ProfessionCatalog.GetById("night-baker");
        Player player = Player.Create("Ava", baker, TokenColour.Amber);

        IncomeStatement statement = IncomeStatement.For(player);

        Assert.Equal(0, player.ChildCount);
        Assert.Equal(baker.MonthlySalary, statement.Salary);
        Assert.Equal(Money.Zero, statement.PassiveIncome);
        Assert.Equal(baker.MonthlySalary, statement.TotalIncome);
        Assert.Equal(baker.MonthlyTaxes, statement.Taxes);
        Assert.Equal(baker.Expenses.Mortgage, statement.MortgagePayment);
        Assert.Equal(baker.Expenses.CarLoan, statement.CarLoanPayment);
        Assert.Equal(baker.Expenses.CreditCard, statement.CreditCardPayment);
        Assert.Equal(Money.Zero, statement.OtherLiabilityPayments);
        Assert.Equal(
            baker.Expenses.Mortgage + baker.Expenses.CarLoan + baker.Expenses.CreditCard,
            statement.LiabilityPayments);
        Assert.Equal(Money.FromDollars(890), statement.LiabilityPayments);
        Assert.Equal(baker.Expenses.Other, statement.OtherExpenses);
        Assert.Equal(baker.Expenses.PerChild, statement.PerChildExpense);
        Assert.Equal(0, statement.ChildCount);
        Assert.Equal(Money.Zero, statement.ChildExpenses);
        Assert.Equal(baker.MonthlyTaxes + baker.Expenses.Total(0), statement.TotalExpenses);
        Assert.Equal(baker.MonthlyCashFlow(0), statement.TotalIncome - statement.TotalExpenses);
        Assert.Equal(Money.FromDollars(630), statement.TotalIncome - statement.TotalExpenses);
    }

    [Fact]
    public void For_a_player_with_children_adds_the_per_child_expense()
    {
        Profession baker = ProfessionCatalog.GetById("night-baker");
        Player player = Player.Create("Ava", baker, TokenColour.Amber);

        Player parent = player.WithChildCount(2);
        IncomeStatement statement = IncomeStatement.For(parent);

        Assert.Equal(2, parent.ChildCount);
        Assert.Equal(0, player.ChildCount);
        Assert.Equal(baker.Expenses.PerChild * 2, statement.ChildExpenses);
        Assert.Equal(Money.FromDollars(560), statement.ChildExpenses);
        Assert.Equal(baker.MonthlyTaxes + baker.Expenses.Total(2), statement.TotalExpenses);
        Assert.Equal(baker.MonthlyCashFlow(2), statement.TotalIncome - statement.TotalExpenses);
        Assert.Equal(Money.FromDollars(70), statement.TotalIncome - statement.TotalExpenses);
    }

    [Fact]
    public void For_includes_passive_income_and_other_loan_payments()
    {
        Profession baker = ProfessionCatalog.GetById("night-baker");
        Player player = Player.Create("Ava", baker, TokenColour.Cobalt);
        StockHolding park = StockHolding.Create("PARK", 4, Money.FromDollars(20), Money.FromCents(25));
        RealEstateHolding flat = RealEstateHolding.Create(
            "Elm Street Flat",
            Money.FromDollars(500),
            Money.FromDollars(20_000),
            Money.FromDollars(15));
        BusinessHolding cart = BusinessHolding.Create(
            "Harbor Cart",
            Money.FromDollars(100),
            Money.Zero,
            Money.FromDollars(-4));
        LoanLiability note = LoanLiability.Create("Bank note", Money.FromDollars(200), Money.FromDollars(12));
        BalanceSheet sheet = BalanceSheet.Create(
            baker.StartingSavings,
            [park],
            [flat],
            [cart],
            baker.Liabilities.Mortgage,
            baker.Liabilities.CarLoan,
            baker.Liabilities.CreditCard,
            [note]);

        IncomeStatement statement = IncomeStatement.For(player.WithBalanceSheet(sheet));

        Money passive = Money.FromDollars(1) + Money.FromDollars(15) + Money.FromDollars(-4);
        Assert.Equal(Money.FromDollars(12), passive);
        Assert.Equal(passive, statement.PassiveIncome);
        Assert.Equal(baker.MonthlySalary + passive, statement.TotalIncome);
        Assert.Equal(Money.FromDollars(12), statement.OtherLiabilityPayments);
        Assert.Equal(
            baker.Expenses.Mortgage + baker.Expenses.CarLoan + baker.Expenses.CreditCard + Money.FromDollars(12),
            statement.LiabilityPayments);
        Assert.Equal(baker.MonthlyTaxes + baker.Expenses.Total(0) + Money.FromDollars(12), statement.TotalExpenses);
        Assert.Equal(flat.Mortgage, sheet.RealEstate[0].Mortgage);
        Assert.Equal(baker.Expenses.Mortgage, statement.MortgagePayment);
    }

    [Fact]
    public void For_rejects_a_null_player()
    {
        Assert.Throws<ArgumentNullException>(() => IncomeStatement.For(null!));
    }

    [Fact]
    public void Create_accepts_an_empty_statement()
    {
        IncomeStatement statement = IncomeStatement.Create(
            Money.Zero,
            Money.Zero,
            Money.Zero,
            Money.Zero,
            Money.Zero,
            Money.Zero,
            Money.Zero,
            Money.Zero,
            0,
            Money.Zero);

        Assert.True(statement.TotalIncome.IsZero);
        Assert.True(statement.TotalExpenses.IsZero);
        Assert.True(statement.PassiveIncome.IsZero);
        Assert.True(statement.ChildExpenses.IsZero);
    }

    [Fact]
    public void Create_allows_passive_income_to_be_negative()
    {
        IncomeStatement statement = IncomeStatement.Create(
            Money.FromDollars(100),
            Money.FromDollars(-40),
            Money.Zero,
            Money.Zero,
            Money.Zero,
            Money.Zero,
            Money.Zero,
            Money.Zero,
            0,
            Money.Zero);

        Assert.Equal(Money.FromDollars(-40), statement.PassiveIncome);
        Assert.Equal(Money.FromDollars(60), statement.TotalIncome);
        Assert.True(statement.TotalExpenses.IsZero);
    }

    [Fact]
    public void Create_reports_every_negative_amount_together()
    {
        IncomeStatementValidationException exception = Assert.Throws<IncomeStatementValidationException>(() =>
            IncomeStatement.Create(
                Money.FromDollars(-1),
                Money.FromDollars(-5),
                Money.FromDollars(-1),
                Money.FromDollars(-1),
                Money.FromDollars(-1),
                Money.FromDollars(-1),
                Money.FromDollars(-1),
                Money.FromDollars(-1),
                -1,
                Money.FromDollars(-1)));

        Assert.Contains("Salary cannot be negative.", exception.Errors);
        Assert.Contains("Taxes cannot be negative.", exception.Errors);
        Assert.Contains("Mortgage payment cannot be negative.", exception.Errors);
        Assert.Contains("Car loan payment cannot be negative.", exception.Errors);
        Assert.Contains("Credit card payment cannot be negative.", exception.Errors);
        Assert.Contains("Other expenses cannot be negative.", exception.Errors);
        Assert.Contains("Per-child expense cannot be negative.", exception.Errors);
        Assert.Contains("Child count cannot be negative.", exception.Errors);
        Assert.Contains("Other liability payments cannot be negative.", exception.Errors);
        Assert.Equal(9, exception.Errors.Count);
    }

    [Fact]
    public void Create_throws_when_child_expenses_overflow()
    {
        Assert.Throws<OverflowException>(() => IncomeStatement.Create(
            Money.Zero,
            Money.Zero,
            Money.Zero,
            Money.Zero,
            Money.Zero,
            Money.Zero,
            Money.Zero,
            Money.FromCents(long.MaxValue),
            2,
            Money.Zero));
    }

    [Fact]
    public void Create_throws_when_total_income_overflows()
    {
        Assert.Throws<OverflowException>(() => IncomeStatement.Create(
            Money.FromCents(long.MaxValue),
            Money.FromCents(1),
            Money.Zero,
            Money.Zero,
            Money.Zero,
            Money.Zero,
            Money.Zero,
            Money.Zero,
            0,
            Money.Zero));
    }

    [Fact]
    public void WithChildCount_rejects_a_negative_count_and_leaves_the_player_unchanged()
    {
        Player player = Player.Create("Ava", ProfessionCatalog.GetById("night-baker"), TokenColour.Fern);

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => player.WithChildCount(-1));

        Assert.Equal("childCount", exception.ParamName);
        Assert.Equal(0, player.ChildCount);
    }

    [Fact]
    public void WithBalanceSheet_keeps_the_child_count()
    {
        Profession baker = ProfessionCatalog.GetById("night-baker");
        Player parent = Player.Create("Ava", baker, TokenColour.Fern).WithChildCount(1);
        BalanceSheet sheet = BalanceSheet.FromProfession(baker);

        Player updated = parent.WithBalanceSheet(sheet);

        Assert.Equal(1, updated.ChildCount);
        Assert.Equal(1, IncomeStatement.For(updated).ChildCount);
        Assert.Same(sheet, updated.BalanceSheet);
    }
}
