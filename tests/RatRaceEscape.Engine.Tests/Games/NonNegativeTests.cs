namespace RatRaceEscape.Engine.Tests;

public class NonNegativeTests
{
    [Fact]
    public void A_loan_that_would_make_monthly_cash_flow_negative_is_refused()
    {
        Player baker = Baker();
        Assert.Equal(6, CashFlow.BankIncrementsAllowed(baker));

        Player room = BankLoan.Borrow(baker, 6);
        TurnException refused = Assert.Throws<TurnException>(() => BankLoan.Borrow(baker, 7));
        TurnException tighter = Assert.Throws<TurnException>(() => BankLoan.Borrow(room, 1));

        Assert.Contains("That loan would leave monthly cash flow below zero.", refused.Errors);
        Assert.Contains("That loan would leave monthly cash flow below zero.", tighter.Errors);
        Assert.Empty(baker.BalanceSheet.OtherLoans);
        Assert.Equal(Money.FromDollars(900), baker.Cash);
        Assert.Equal(Money.FromDollars(30), CashFlow.For(room).Monthly);
        Assert.Equal(0, CashFlow.BankIncrementsAllowed(room));
        Assert.False(CashFlow.For(room).Monthly.IsNegative);
    }

    [Fact]
    public void A_purchase_that_would_make_monthly_cash_flow_negative_is_refused()
    {
        Player tight = BankLoan.Borrow(Baker(), 6);

        TurnException refused = Assert.Throws<TurnException>(() => AssetPurchase.BuyRealEstate(
            tight,
            "Lantern Yard",
            Money.FromDollars(1),
            Money.FromDollars(1),
            Money.FromDollars(-80)));

        Assert.Contains("That purchase would leave monthly cash flow below zero.", refused.Errors);
        Assert.Empty(tight.BalanceSheet.RealEstate);
        Assert.Equal(Money.FromDollars(6_900), tight.Cash);
        Assert.False(CashFlow.For(tight).Monthly.IsNegative);
    }

    private static Player Baker()
    {
        return Player.Create("Ava", ProfessionCatalog.GetById("night-baker"), TokenColour.Amber);
    }
}
