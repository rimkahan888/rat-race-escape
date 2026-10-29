namespace RatRaceEscape.Engine;

/// <summary>
/// Hours cut collects the player's total expenses, as they stand before any
/// loan taken to pay this bill, and makes them miss the next two times the
/// dice would reach them. The turn they landed on still finishes. When cash
/// cannot cover the expenses, nothing is taken and the turn stays open for
/// settlement.
/// </summary>
public static class Downsized
{
    public const int SkippedTurns = 2;

    public static Player Apply(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        Money due = IncomeStatement.For(player).TotalExpenses;
        if (player.Cash < due)
        {
            throw new TurnException("Cash cannot cover the downsized expenses.");
        }

        return player
            .WithCash(player.Cash - due)
            .WithSkipTurnsRemaining(player.SkipTurnsRemaining + SkippedTurns);
    }
}
