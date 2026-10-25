namespace RatRaceEscape.Engine.Tests;

public class CashFlowTests
{
    [Fact]
    public void Monthly_is_total_income_minus_total_expenses_in_cents()
    {
        CashFlow cashFlow = CashFlow.From(Statement(
            salaryCents: 100,
            passiveCents: 50,
            taxesCents: 30,
            mortgageCents: 1,
            carCents: 1,
            cardCents: 1,
            otherCents: 1,
            perChildCents: 7,
            children: 2,
            otherLoansCents: 2));

        Assert.Equal(Money.FromCents(150), cashFlow.TotalIncome);
        Assert.Equal(Money.FromCents(50), cashFlow.TotalExpenses);
        Assert.Equal(Money.FromCents(100), cashFlow.Monthly);
        Assert.Equal(Money.FromCents(50), cashFlow.PassiveIncome);
        Assert.Equal(Money.Zero, cashFlow.PassiveSurplus);
        Assert.False(cashFlow.PassiveIncomeExceedsExpenses);
    }

    [Fact]
    public void Passive_income_equal_to_expenses_does_not_clear_the_escape_check()
    {
        CashFlow cashFlow = CashFlow.From(Statement(passiveCents: 100, taxesCents: 100));

        Assert.Equal(Money.Zero, cashFlow.PassiveSurplus);
        Assert.Equal(Money.Zero, cashFlow.Monthly);
        Assert.False(cashFlow.PassiveIncomeExceedsExpenses);
    }

    [Fact]
    public void Passive_income_one_cent_over_expenses_clears_the_escape_check()
    {
        CashFlow cashFlow = CashFlow.From(Statement(passiveCents: 101, taxesCents: 100));

        Assert.Equal(Money.FromCents(1), cashFlow.PassiveSurplus);
        Assert.Equal(Money.FromCents(1), cashFlow.Monthly);
        Assert.True(cashFlow.PassiveIncomeExceedsExpenses);
    }

    [Fact]
    public void Passive_income_one_cent_under_expenses_fails_the_escape_check()
    {
        CashFlow cashFlow = CashFlow.From(Statement(passiveCents: 99, taxesCents: 100));

        Assert.Equal(Money.FromCents(-1), cashFlow.PassiveSurplus);
        Assert.Equal(Money.FromCents(-1), cashFlow.Monthly);
        Assert.False(cashFlow.PassiveIncomeExceedsExpenses);
    }

    [Fact]
    public void Salary_does_not_count_toward_the_escape_check()
    {
        CashFlow cashFlow = CashFlow.From(Statement(salaryCents: 1_000_000, passiveCents: 99, taxesCents: 100));

        Assert.Equal(Money.FromCents(999_999), cashFlow.Monthly);
        Assert.True(cashFlow.Monthly.IsPositive);
        Assert.Equal(Money.FromCents(-1), cashFlow.PassiveSurplus);
        Assert.False(cashFlow.PassiveIncomeExceedsExpenses);
    }

    [Fact]
    public void Passive_income_can_clear_the_escape_check_with_no_salary()
    {
        CashFlow cashFlow = CashFlow.From(Statement(passiveCents: 101, taxesCents: 100));

        Assert.True(cashFlow.TotalIncome == cashFlow.PassiveIncome);
        Assert.True(cashFlow.PassiveIncomeExceedsExpenses);
        Assert.Equal(cashFlow.PassiveSurplus, cashFlow.Monthly);
    }

    [Fact]
    public void Monthly_is_negative_when_expenses_exceed_income()
    {
        CashFlow cashFlow = CashFlow.From(Statement(salaryCents: 40, taxesCents: 100));

        Assert.Equal(Money.FromCents(-60), cashFlow.Monthly);
        Assert.True(cashFlow.Monthly.IsNegative);
        Assert.False(cashFlow.PassiveIncomeExceedsExpenses);
    }

    [Fact]
    public void Negative_passive_income_widens_the_shortfall()
    {
        CashFlow cashFlow = CashFlow.From(Statement(passiveCents: -50, taxesCents: 10));

        Assert.Equal(Money.FromCents(-60), cashFlow.Monthly);
        Assert.Equal(Money.FromCents(-60), cashFlow.PassiveSurplus);
        Assert.False(cashFlow.PassiveIncomeExceedsExpenses);
    }

    [Fact]
    public void Zero_income_and_zero_expenses_do_not_clear_the_escape_check()
    {
        CashFlow cashFlow = CashFlow.From(Statement());

        Assert.True(cashFlow.Monthly.IsZero);
        Assert.True(cashFlow.PassiveSurplus.IsZero);
        Assert.False(cashFlow.PassiveIncomeExceedsExpenses);
    }

    [Fact]
    public void A_child_can_remove_a_one_cent_surplus()
    {
        CashFlow before = CashFlow.From(Statement(passiveCents: 200, taxesCents: 100));
        CashFlow after = CashFlow.From(Statement(passiveCents: 200, taxesCents: 100, perChildCents: 101, children: 1));

        Assert.True(before.PassiveIncomeExceedsExpenses);
        Assert.Equal(Money.FromCents(100), before.Monthly);
        Assert.False(after.PassiveIncomeExceedsExpenses);
        Assert.Equal(Money.FromCents(-1), after.Monthly);
        Assert.Equal(Money.FromCents(-1), after.PassiveSurplus);
    }

    [Fact]
    public void The_smallest_passive_income_is_kept_when_there_are_no_expenses()
    {
        CashFlow cashFlow = CashFlow.From(Statement(passiveCents: long.MinValue));

        Assert.Equal(Money.FromCents(long.MinValue), cashFlow.Monthly);
        Assert.Equal(Money.FromCents(long.MinValue), cashFlow.PassiveSurplus);
        Assert.False(cashFlow.PassiveIncomeExceedsExpenses);
    }

    [Fact]
    public void Subtracting_a_bill_from_the_smallest_income_overflows()
    {
        IncomeStatement statement = Statement(passiveCents: long.MinValue, taxesCents: 1);

        Assert.Throws<OverflowException>(() => CashFlow.From(statement));
    }

    [Fact]
    public void For_a_new_baker_matches_the_profession_and_does_not_escape()
    {
        Profession baker = ProfessionCatalog.GetById("night-baker");
        Player player = Player.Create("Ava", baker, TokenColour.Amber);

        CashFlow cashFlow = CashFlow.For(player);

        Assert.Equal(baker.MonthlyCashFlow(0), cashFlow.Monthly);
        Assert.Equal(Money.FromDollars(630), cashFlow.Monthly);
        Assert.Equal(Money.Zero, cashFlow.PassiveIncome);
        Assert.True(cashFlow.PassiveSurplus.IsNegative);
        Assert.False(cashFlow.PassiveIncomeExceedsExpenses);
    }

    [Fact]
    public void Three_children_make_the_baker_cash_flow_negative()
    {
        Profession baker = ProfessionCatalog.GetById("night-baker");
        Player player = Player.Create("Ava", baker, TokenColour.Slate).WithChildCount(3);

        CashFlow cashFlow = CashFlow.For(player);

        Assert.Equal(baker.MonthlyCashFlow(3), cashFlow.Monthly);
        Assert.Equal(Money.FromCents(-21_000), cashFlow.Monthly);
        Assert.True(cashFlow.Monthly.IsNegative);
        Assert.False(cashFlow.PassiveIncomeExceedsExpenses);
    }

    [Fact]
    public void Asset_income_increases_monthly_cash_flow_by_the_net_amount()
    {
        Profession baker = ProfessionCatalog.GetById("night-baker");
        Player player = Player.Create("Ava", baker, TokenColour.Violet);
        StockHolding park = StockHolding.Create("PARK", 1, Money.FromDollars(10), Money.FromDollars(10));
        BalanceSheet sheet = BalanceSheet.Create(
            baker.StartingSavings,
            [park],
            [],
            [],
            baker.Liabilities.Mortgage,
            baker.Liabilities.CarLoan,
            baker.Liabilities.CreditCard,
            []);

        CashFlow cashFlow = CashFlow.For(player.WithBalanceSheet(sheet));

        Assert.Equal(Money.FromDollars(10), cashFlow.PassiveIncome);
        Assert.Equal(baker.MonthlyCashFlow(0) + Money.FromDollars(10), cashFlow.Monthly);
        Assert.False(cashFlow.PassiveIncomeExceedsExpenses);
    }

    [Fact]
    public void From_rejects_a_null_statement()
    {
        Assert.Throws<ArgumentNullException>(() => CashFlow.From(null!));
    }

    [Fact]
    public void For_rejects_a_null_player()
    {
        Assert.Throws<ArgumentNullException>(() => CashFlow.For(null!));
    }

    private static IncomeStatement Statement(
        long salaryCents = 0,
        long passiveCents = 0,
        long taxesCents = 0,
        long mortgageCents = 0,
        long carCents = 0,
        long cardCents = 0,
        long otherCents = 0,
        long perChildCents = 0,
        int children = 0,
        long otherLoansCents = 0)
    {
        return IncomeStatement.Create(
            Money.FromCents(salaryCents),
            Money.FromCents(passiveCents),
            Money.FromCents(taxesCents),
            Money.FromCents(mortgageCents),
            Money.FromCents(carCents),
            Money.FromCents(cardCents),
            Money.FromCents(otherCents),
            Money.FromCents(perChildCents),
            children,
            Money.FromCents(otherLoansCents));
    }
}
