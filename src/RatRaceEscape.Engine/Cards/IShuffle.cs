namespace RatRaceEscape.Engine;

/// <summary>
/// Reorders a list of cards in place. Callers inject this so a deck can use a
/// seeded shuffle at the table and a fixed order in tests.
/// </summary>
public interface IShuffle
{
    /// <summary>
    /// Reorders <paramref name="cards"/>. Implementations may leave a list of
    /// fewer than two cards as it is.
    /// </summary>
    void Shuffle<T>(IList<T> cards);
}
