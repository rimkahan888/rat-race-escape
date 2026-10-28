namespace RatRaceEscape.Engine;

/// <summary>
/// Leaves the rat race when passive income is strictly greater than total
/// expenses. The token moves to the opening square of the fast track. Salary
/// no longer decides the comparison: only passive income does.
/// </summary>
public static class RatRaceExit
{
    public static bool CanLeave(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        return player.Track == BoardTrack.RatRace
            && !player.IsBankrupt
            && CashFlow.For(player).PassiveIncomeExceedsExpenses;
    }

    public static Player Leave(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (player.IsBankrupt)
        {
            throw new TurnException("A bankrupt player cannot leave the rat race.");
        }

        if (player.Track != BoardTrack.RatRace)
        {
            throw new TurnException("This player has already left the rat race.");
        }

        if (!CashFlow.For(player).PassiveIncomeExceedsExpenses)
        {
            throw new TurnException("Passive income does not yet cover the bills.");
        }

        return player.WithTrack(BoardTrack.FastTrack, boardIndex: 0);
    }
}
