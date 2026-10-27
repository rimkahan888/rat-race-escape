namespace RatRaceEscape.Engine;

/// <summary>
/// Buys shares for cash. The shares become an asset at the price paid, and
/// their dividend is passive income. The player is unchanged when the cash
/// cannot cover the purchase.
/// </summary>
public static class StockPurchase
{
    public static Player Buy(Player player, string symbol, int shares, Money pricePerShare, Money dividendPerShare)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(symbol);

        if (shares <= 0)
        {
            throw new TurnException("Stock shares must be greater than zero.");
        }

        Money cost = pricePerShare * shares;
        if (player.Cash < cost)
        {
            throw new TurnException("Cash cannot cover the stock purchase.");
        }

        StockHolding holding = StockHolding.Create(symbol, shares, pricePerShare, dividendPerShare);
        List<StockHolding> stocks = new(player.BalanceSheet.Stocks.Count + 1);
        stocks.AddRange(player.BalanceSheet.Stocks);
        stocks.Add(holding);

        BalanceSheet sheet = player.BalanceSheet;
        return player.WithBalanceSheet(BalanceSheet.Create(
            player.Cash - cost,
            stocks,
            sheet.RealEstate,
            sheet.Businesses,
            sheet.Mortgage,
            sheet.CarLoan,
            sheet.CreditCard,
            sheet.OtherLoans));
    }
}
