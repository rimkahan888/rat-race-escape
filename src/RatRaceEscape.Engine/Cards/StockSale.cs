namespace RatRaceEscape.Engine;

/// <summary>
/// Sells shares for cash. Lots of the same symbol are sold oldest first.
/// A lot that is sold in full comes off the balance sheet. A partial sale
/// keeps that lot's cost and dividend and reduces the share count. Cash
/// increases by the sale price times the shares sold.
/// </summary>
public static class StockSale
{
    public static Player Sell(Player player, string symbol, int shares, Money pricePerShare)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(symbol);

        if (shares <= 0)
        {
            throw new TurnException("Stock shares must be greater than zero.");
        }

        if (!pricePerShare.IsPositive)
        {
            throw new TurnException("Sale price must be greater than zero.");
        }

        string trimmed = symbol.Trim().ToUpperInvariant();
        if (!IsSymbol(trimmed))
        {
            throw new TurnException("Stock symbol must be 1 to 8 letters or digits and start with a letter.");
        }

        int owned = Count(player, trimmed);
        if (owned < shares)
        {
            throw new TurnException(
                owned == 0
                    ? "The player does not own that stock."
                    : "The player does not have that many shares.");
        }

        int remaining = shares;
        List<StockHolding> stocks = new(player.BalanceSheet.Stocks.Count);
        foreach (StockHolding lot in player.BalanceSheet.Stocks)
        {
            if (remaining == 0 || lot.Symbol != trimmed)
            {
                stocks.Add(lot);
                continue;
            }

            if (lot.Shares <= remaining)
            {
                remaining -= lot.Shares;
                continue;
            }

            stocks.Add(StockHolding.Create(
                lot.Symbol,
                lot.Shares - remaining,
                lot.CostPerShare,
                lot.MonthlyDividendPerShare));
            remaining = 0;
        }

        Money proceeds = pricePerShare * shares;
        BalanceSheet sheet = player.BalanceSheet;
        return player.WithBalanceSheet(BalanceSheet.Create(
            player.Cash + proceeds,
            stocks,
            sheet.RealEstate,
            sheet.Businesses,
            sheet.Mortgage,
            sheet.CarLoan,
            sheet.CreditCard,
            sheet.OtherLoans));
    }

    private static int Count(Player player, string symbol)
    {
        int owned = 0;
        foreach (StockHolding lot in player.BalanceSheet.Stocks)
        {
            if (lot.Symbol == symbol)
            {
                owned += lot.Shares;
            }
        }

        return owned;
    }

    private static bool IsSymbol(string symbol)
    {
        if (symbol.Length is < 1 or > 8)
        {
            return false;
        }

        if (!char.IsAsciiLetter(symbol[0]))
        {
            return false;
        }

        for (int index = 1; index < symbol.Length; index++)
        {
            if (!char.IsAsciiLetterOrDigit(symbol[index]))
            {
                return false;
            }
        }

        return true;
    }
}
