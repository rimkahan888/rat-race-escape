namespace RatRaceEscape.Engine;

/// <summary>
/// Rolls a six-sided die with <see cref="Random"/>. A seed builds the same
/// sequence every time, which keeps a whole game repeatable.
/// </summary>
public sealed class RandomDieRoller : IDieRoller
{
    private readonly Lock _gate = new();
    private readonly Random _random;

    public RandomDieRoller()
        : this(Random.Shared)
    {
    }

    public RandomDieRoller(int seed)
        : this(new Random(seed))
    {
    }

    public RandomDieRoller(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        _random = random;
    }

    public int Roll()
    {
        lock (_gate)
        {
            return _random.Next(Dice.MinimumFace, Dice.MaximumFace + 1);
        }
    }
}
