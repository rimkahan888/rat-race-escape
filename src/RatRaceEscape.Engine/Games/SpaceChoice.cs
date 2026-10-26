namespace RatRaceEscape.Engine;

/// <summary>
/// A decision a square can ask for. Charity asks the player to donate or decline.
/// Other squares take no choice.
/// </summary>
public enum SpaceChoice
{
    None,
    Donate,
    Decline,
}
