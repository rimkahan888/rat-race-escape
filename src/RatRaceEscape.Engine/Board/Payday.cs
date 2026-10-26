namespace RatRaceEscape.Engine;

/// <summary>
/// Pays or collects monthly cash flow for each payday a token lands on or passes.
/// The square the token was already standing on does not count.
/// </summary>
public static class Payday
{
    public static Player Collect(Player player, int paydayCount)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (paydayCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(paydayCount), "Payday count cannot be negative.");
        }

        if (paydayCount == 0)
        {
            return player;
        }

        Money change = CashFlow.For(player).Monthly * paydayCount;
        if (change.IsNegative && player.Cash < change.Negate())
        {
            throw new TurnException("Cash cannot cover the payday shortfall.");
        }

        return player.WithCash(player.Cash + change);
    }
}
