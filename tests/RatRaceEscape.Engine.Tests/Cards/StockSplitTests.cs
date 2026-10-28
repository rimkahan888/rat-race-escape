namespace RatRaceEscape.Engine.Tests;

public class StockSplitTests
{
    [Fact]
    public void A_split_doubles_shares_and_income_and_halves_an_even_cost()
    {
        Player baker = StockPurchase.Buy(Baker(), "PIER", 4, Money.FromDollars(25), Money.FromDollars(1));
        Money assets = baker.BalanceSheet.TotalAssets;

        Player split = StockSplit.Apply(baker, " pier ");

        StockHolding lot = Assert.Single(split.BalanceSheet.Stocks);
        Assert.Equal(8, lot.Shares);
        Assert.Equal(Money.FromCents(1_250), lot.CostPerShare);
        Assert.Equal(Money.FromDollars(1), lot.MonthlyDividendPerShare);
        Assert.Equal(Money.FromDollars(100), lot.Cost);
        Assert.Equal(Money.FromDollars(8), IncomeStatement.For(split).PassiveIncome);
        Assert.Equal(baker.Cash, split.Cash);
        Assert.Equal(assets, split.BalanceSheet.TotalAssets);
        Assert.Equal(4, Assert.Single(baker.BalanceSheet.Stocks).Shares);
    }

    [Fact]
    public void An_odd_cost_drops_the_leftover_cent_on_each_old_share()
    {
        Player baker = StockPurchase.Buy(Baker(), "KITE", 3, Money.FromCents(125), Money.Zero);

        Player split = StockSplit.Apply(baker, "KITE");

        StockHolding lot = Assert.Single(split.BalanceSheet.Stocks);
        Assert.Equal(6, lot.Shares);
        Assert.Equal(Money.FromCents(62), lot.CostPerShare);
        Assert.Equal(Money.FromCents(372), lot.Cost);
        Assert.Equal(baker.BalanceSheet.TotalAssets - Money.FromCents(3), split.BalanceSheet.TotalAssets);
    }

    [Fact]
    public void Other_symbols_and_players_without_the_stock_stay_put()
    {
        Player holder = StockPurchase.Buy(
            StockPurchase.Buy(Baker(), "PIER", 2, Money.FromDollars(25), Money.FromDollars(1)),
            "LUMEN",
            3,
            Money.FromDollars(10),
            Money.Zero);
        Player bystander = Baker();

        Player[] split = StockSplit.ApplyAll([holder, bystander], "LUMEN");

        Assert.Equal(2, split[0].BalanceSheet.Stocks.Count);
        StockHolding pier = split[0].BalanceSheet.Stocks[0];
        StockHolding lumen = split[0].BalanceSheet.Stocks[1];
        Assert.Equal(2, pier.Shares);
        Assert.Equal(Money.FromDollars(25), pier.CostPerShare);
        Assert.Equal(6, lumen.Shares);
        Assert.Equal(Money.FromDollars(5), lumen.CostPerShare);
        Assert.Same(bystander, split[1]);
        Assert.Empty(bystander.BalanceSheet.Stocks);
    }

    [Fact]
    public void Apply_rejects_a_bad_symbol_and_null_arguments()
    {
        Player baker = StockPurchase.Buy(Baker(), "PIER", 1, Money.FromDollars(25), Money.Zero);

        TurnException symbol = Assert.Throws<TurnException>(() => StockSplit.Apply(baker, "1BAD"));

        Assert.Contains("A stock split needs a stock symbol.", symbol.Errors);
        Assert.Throws<ArgumentNullException>(() => StockSplit.Apply(null!, "PIER"));
        Assert.Throws<ArgumentNullException>(() => StockSplit.Apply(baker, null!));
        Assert.Throws<ArgumentNullException>(() => StockSplit.ApplyAll(null!, "PIER"));
        Assert.Equal(1, Assert.Single(baker.BalanceSheet.Stocks).Shares);
    }

    private static Player Baker()
    {
        return Player.Create("Ava", ProfessionCatalog.GetById("night-baker"), TokenColour.Amber);
    }
}
