namespace RatRaceEscape.Engine.Tests;

public class StockPurchaseTests
{
    [Fact]
    public void Buy_moves_cash_into_a_share_lot_and_adds_the_dividend()
    {
        Player baker = Baker();
        Money beforeAssets = baker.BalanceSheet.TotalAssets;

        Player buyer = StockPurchase.Buy(baker, " pier ", 4, Money.FromDollars(25), Money.FromDollars(1));

        Assert.Equal(baker.Cash - Money.FromDollars(100), buyer.Cash);
        Assert.Equal(baker.Cash, baker.Cash);
        StockHolding lot = Assert.Single(buyer.BalanceSheet.Stocks);
        Assert.Equal("PIER", lot.Symbol);
        Assert.Equal(4, lot.Shares);
        Assert.Equal(Money.FromDollars(25), lot.CostPerShare);
        Assert.Equal(Money.FromDollars(1), lot.MonthlyDividendPerShare);
        Assert.Equal(Money.FromDollars(4), IncomeStatement.For(buyer).PassiveIncome);
        Assert.Equal(beforeAssets, buyer.BalanceSheet.TotalAssets);
        Assert.Equal(baker.BalanceSheet.TotalLiabilities, buyer.BalanceSheet.TotalLiabilities);
        Assert.Equal(CashFlow.For(baker).Monthly + Money.FromDollars(4), CashFlow.For(buyer).Monthly);
    }

    [Fact]
    public void A_second_lot_of_the_same_symbol_is_kept_separate()
    {
        Player baker = Baker();

        Player twice = StockPurchase.Buy(
            StockPurchase.Buy(baker, "LUMEN", 2, Money.FromDollars(10), Money.Zero),
            "lumen",
            3,
            Money.FromDollars(12),
            Money.FromDollars(1));

        Assert.Equal(2, twice.BalanceSheet.Stocks.Count);
        Assert.Equal(Money.FromDollars(3), IncomeStatement.For(twice).PassiveIncome);
        Assert.Equal(baker.Cash - Money.FromDollars(20) - Money.FromDollars(36), twice.Cash);
    }

    [Fact]
    public void Buy_fails_when_cash_is_short_and_leaves_the_player()
    {
        Player baker = Baker().WithCash(Money.FromDollars(99));

        TurnException exception = Assert.Throws<TurnException>(() =>
            StockPurchase.Buy(baker, "PIER", 4, Money.FromDollars(25), Money.FromDollars(1)));

        Assert.Contains("Cash cannot cover the stock purchase.", exception.Errors);
        Assert.Equal(Money.FromDollars(99), baker.Cash);
        Assert.Empty(baker.BalanceSheet.Stocks);
    }

    [Fact]
    public void Buy_rejects_no_shares_and_null_arguments()
    {
        Player baker = Baker();

        TurnException none = Assert.Throws<TurnException>(() =>
            StockPurchase.Buy(baker, "PIER", 0, Money.FromDollars(25), Money.Zero));
        TurnException negative = Assert.Throws<TurnException>(() =>
            StockPurchase.Buy(baker, "PIER", -1, Money.FromDollars(25), Money.Zero));

        Assert.Contains("Stock shares must be greater than zero.", none.Errors);
        Assert.Contains("Stock shares must be greater than zero.", negative.Errors);
        Assert.Throws<ArgumentNullException>(() => StockPurchase.Buy(null!, "PIER", 1, Money.FromDollars(1), Money.Zero));
        Assert.Throws<ArgumentNullException>(() => StockPurchase.Buy(baker, null!, 1, Money.FromDollars(1), Money.Zero));
        Assert.Empty(baker.BalanceSheet.Stocks);
    }

    private static Player Baker()
    {
        return Player.Create("Ava", ProfessionCatalog.GetById("night-baker"), TokenColour.Amber);
    }
}
