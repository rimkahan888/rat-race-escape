namespace RatRaceEscape.Engine;

/// <summary>
/// Leaves cards in the order they were handed in. A printed deck dealt from
/// the top, and any test that wants that order, injects this.
/// </summary>
public sealed class IdentityShuffle : IShuffle
{
    public void Shuffle<T>(IList<T> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);
    }
}
