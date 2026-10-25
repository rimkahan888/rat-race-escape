namespace RatRaceEscape.Engine;

/// <summary>
/// A seat requested for a new game: the player's name, the profession they
/// chose, and the token they will move.
/// </summary>
public readonly record struct PlayerSetup(string Name, string ProfessionId, TokenColour TokenColour);
