namespace RatRaceEscape.Engine;

public sealed partial class Game
{
    /// <summary>
    /// Leaves the rat race when passive income is strictly greater than the
    /// bills. The token moves to the opening square of the fast track. The
    /// player can leave while waiting to roll, or after the space is resolved.
    /// </summary>
    public Game LeaveRatRace()
    {
        RequireAction();

        if (TurnPhase == TurnPhase.AwaitingResolve)
        {
            throw new TurnException("Resolve the space before leaving the rat race.");
        }

        Player left = RatRaceExit.Leave(CurrentPlayer);
        return WithPlayer(left, TurnPhase, LastRoll, PendingDeck, Drawn, Piles);
    }
}
