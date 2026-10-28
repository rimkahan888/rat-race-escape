namespace RatRaceEscape.Engine;

/// <summary>
/// Where the table is in the game. A new game starts in the rat race.
/// The table is over when every player is bankrupt, and won when a player
/// buys a dream on the fast track.
/// </summary>
public enum GamePhase
{
    RatRace,
    Over,
    Won,
}
