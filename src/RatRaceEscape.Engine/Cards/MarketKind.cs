namespace RatRaceEscape.Engine;

/// <summary>
/// What a market card asks the table to do. An offer names a price for an
/// asset a player may own. An event changes every player's cash.
/// </summary>
public enum MarketKind
{
    Offer,
    Event,
}
