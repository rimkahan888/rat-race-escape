namespace RatRaceEscape.Engine.Tests;

public class StockSaleTests
{
    [Fact]
    public void Sell_pays_the_price_and_keeps_the_rest_of_the_lot()
    {
        Player baker = StockPurchase.Buy(Baker(), " pier ", 4, Money.FromDollars(25), Money.FromDollars(1));
        Money assets = baker.BalanceSheet.TotalAssets;

        Player seller = StockSale.Sell(baker, "pier", 1, Money.FromDollars(40));

        StockHolding lot = Assert.Single(seller.BalanceSheet.Stocks);
        Assert.Equal("PIER", lot.Symbol);
        Assert.Equal(3, lot.Shares);
        Assert.Equal(Money.FromDollars(25), lot.CostPerShare);
        Assert.Equal(Money.FromDollars(1), lot.MonthlyDividendPerShare);
        Assert.Equal(baker.Cash + Money.FromDollars(40), seller.Cash);
        Assert.Equal(Money.FromDollars(3), IncomeStatement.For(seller).PassiveIncome);
        Assert.Equal(assets - Money.FromDollars(25) + Money.FromDollars(40), seller.BalanceSheet.TotalAssets);
        Assert.Equal(4, Assert.Single(baker.BalanceSheet.Stocks).Shares);
        Assert.Equal(baker.Cash, baker.Cash);
    }

    [Fact]
    public void Selling_the_whole_lot_removes_it_and_its_dividend()
    {
        Player baker = StockPurchase.Buy(Baker(), "PIER", 4, Money.FromDollars(25), Money.FromDollars(1));

        Player seller = StockSale.Sell(baker, "PIER", 4, Money.FromDollars(40));

        Assert.Empty(seller.BalanceSheet.Stocks);
        Assert.Equal(Money.Zero, IncomeStatement.For(seller).PassiveIncome);
        Assert.Equal(baker.Cash + Money.FromDollars(160), seller.Cash);
        Assert.Equal(baker.BalanceSheet.TotalLiabilities, seller.BalanceSheet.TotalLiabilities);
    }

    [Fact]
    public void Lots_of_the_same_symbol_are_sold_oldest_first()
    {
        Player baker = StockPurchase.Buy(
            StockPurchase.Buy(Baker(), "LUMEN", 2, Money.FromDollars(10), Money.Zero),
            "lumen",
            3,
            Money.FromDollars(12),
            Money.FromDollars(1));

        Player seller = StockSale.Sell(baker, "LUMEN", 4, Money.FromDollars(18));

        StockHolding lot = Assert.Single(seller.BalanceSheet.Stocks);
        Assert.Equal(1, lot.Shares);
        Assert.Equal(Money.FromDollars(12), lot.CostPerShare);
        Assert.Equal(Money.FromDollars(1), lot.MonthlyDividendPerShare);
        Assert.Equal(baker.Cash + Money.FromDollars(72), seller.Cash);
        Assert.Equal(Money.FromDollars(1), IncomeStatement.For(seller).PassiveIncome);
    }

    [Fact]
    public void A_different_symbol_is_left_alone()
    {
        Player baker = StockPurchase.Buy(
            StockPurchase.Buy(Baker(), "PIER", 2, Money.FromDollars(25), Money.FromDollars(1)),
            "LUMEN",
            3,
            Money.FromDollars(10),
            Money.Zero);

        Player seller = StockSale.Sell(baker, "PIER", 2, Money.FromDollars(40));

        Assert.Equal(["LUMEN"], seller.BalanceSheet.Stocks.Select(stock => stock.Symbol));
        Assert.Equal(3, Assert.Single(seller.BalanceSheet.Stocks).Shares);
    }

    [Fact]
    public void Sell_fails_when_the_player_does_not_have_the_shares()
    {
        Player baker = StockPurchase.Buy(Baker(), "PIER", 2, Money.FromDollars(25), Money.FromDollars(1));
        Player none = Baker();

        TurnException missing = Assert.Throws<TurnException>(() =>
            StockSale.Sell(none, "PIER", 1, Money.FromDollars(40)));
        TurnException shortLot = Assert.Throws<TurnException>(() =>
            StockSale.Sell(baker, "PIER", 3, Money.FromDollars(40)));

        Assert.Contains("The player does not own that stock.", missing.Errors);
        Assert.Contains("The player does not have that many shares.", shortLot.Errors);
        Assert.Equal(2, Assert.Single(baker.BalanceSheet.Stocks).Shares);
        Assert.Equal(baker.Cash, baker.Cash);
        Assert.Empty(none.BalanceSheet.Stocks);
    }

    [Fact]
    public void Sell_rejects_a_bad_count_price_symbol_and_null_arguments()
    {
        Player baker = StockPurchase.Buy(Baker(), "PIER", 2, Money.FromDollars(25), Money.Zero);

        TurnException none = Assert.Throws<TurnException>(() => StockSale.Sell(baker, "PIER", 0, Money.FromDollars(40)));
        TurnException negative = Assert.Throws<TurnException>(() => StockSale.Sell(baker, "PIER", -1, Money.FromDollars(40)));
        TurnException free = Assert.Throws<TurnException>(() => StockSale.Sell(baker, "PIER", 1, Money.Zero));
        TurnException symbol = Assert.Throws<TurnException>(() => StockSale.Sell(baker, "1BAD", 1, Money.FromDollars(40)));

        Assert.Contains("Stock shares must be greater than zero.", none.Errors);
        Assert.Contains("Stock shares must be greater than zero.", negative.Errors);
        Assert.Contains("Sale price must be greater than zero.", free.Errors);
        Assert.Contains("Stock symbol must be 1 to 8 letters or digits and start with a letter.", symbol.Errors);
        Assert.Throws<ArgumentNullException>(() => StockSale.Sell(null!, "PIER", 1, Money.FromDollars(1)));
        Assert.Throws<ArgumentNullException>(() => StockSale.Sell(baker, null!, 1, Money.FromDollars(1)));
        Assert.Equal(2, Assert.Single(baker.BalanceSheet.Stocks).Shares);
    }

    private static Player Baker()
    {
        return Player.Create("Ava", ProfessionCatalog.GetById("night-baker"), TokenColour.Amber);
    }
}
