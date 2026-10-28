namespace RatRaceEscape.Engine;

/// <summary>
/// Doubles a stock two for one. Every lot of that symbol doubles its shares.
/// The cost per share is halved, dropping a leftover cent, so the cash put in
/// stays the same when the old cost splits evenly. The dividend per share
/// stays, so the monthly income doubles. A player who does not hold the stock
/// is unchanged.
/// </summary>
public static class StockSplit
{
    public static Player Apply(Player player, string symbol)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(symbol);

        string trimmed = symbol.Trim().ToUpperInvariant();
        if (!IsSymbol(trimmed))
        {
            throw new TurnException("A stock split needs a stock symbol.");
        }

        bool held = false;
        List<StockHolding> stocks = new(player.BalanceSheet.Stocks.Count);
        foreach (StockHolding lot in player.BalanceSheet.Stocks)
        {
            if (lot.Symbol != trimmed)
            {
                stocks.Add(lot);
                continue;
            }

            held = true;
            stocks.Add(StockHolding.Create(
                lot.Symbol,
                Doubled(lot.Shares),
                Money.FromCents(lot.CostPerShare.Cents / 2),
                lot.MonthlyDividendPerShare));
        }

        if (!held)
        {
            return player;
        }

        BalanceSheet sheet = player.BalanceSheet;
        return player.WithBalanceSheet(BalanceSheet.Create(
            sheet.Savings,
            stocks,
            sheet.RealEstate,
            sheet.Businesses,
            sheet.Mortgage,
            sheet.CarLoan,
            sheet.CreditCard,
            sheet.OtherLoans));
    }

    public static Player[] ApplyAll(IReadOnlyList<Player> players, string symbol)
    {
        ArgumentNullException.ThrowIfNull(players);

        Player[] next = new Player[players.Count];
        for (int index = 0; index < players.Count; index++)
        {
            next[index] = Apply(players[index], symbol);
        }

        return next;
    }

    private static int Doubled(int shares)
    {
        try
        {
            return checked(shares * 2);
        }
        catch (OverflowException)
        {
            throw new TurnException("That stock split is larger than the sheet can hold.");
        }
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
