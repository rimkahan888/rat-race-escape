namespace RatRaceEscape.Engine;

/// <summary>
/// A decision a square can ask for. Charity asks the player to donate or decline.
/// An offer asks for a small deal or a big deal. A dream listing asks for a
/// claim or a decline. Other squares take no choice.
/// </summary>
public enum SpaceChoice
{
    None,
    Donate,
    Decline,
    SmallDeal,
    BigDeal,
    ClaimDream,
}
