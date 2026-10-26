namespace RatRaceEscape.Engine;

/// <summary>
/// Where the current player is in their turn. Roll, then resolve the square,
/// then end the turn.
/// </summary>
public enum TurnPhase
{
    AwaitingRoll,
    AwaitingResolve,
    AwaitingEnd,
}
