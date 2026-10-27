namespace RatRaceEscape.Engine;

/// <summary>
/// Shuffles with <see cref="Random"/> using Fisher-Yates. A seed builds the
/// same order every time, which keeps a whole game repeatable.
/// </summary>
public sealed class RandomShuffle : IShuffle
{
    private readonly Lock _gate = new();
    private readonly Random _random;

    public RandomShuffle()
        : this(Random.Shared)
    {
    }

    public RandomShuffle(int seed)
        : this(new Random(seed))
    {
    }

    public RandomShuffle(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        _random = random;
    }

    public void Shuffle<T>(IList<T> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);

        lock (_gate)
        {
            for (int index = cards.Count - 1; index > 0; index--)
            {
                int swap = _random.Next(index + 1);
                (cards[index], cards[swap]) = (cards[swap], cards[index]);
            }
        }
    }
}
