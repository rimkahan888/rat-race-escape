namespace RatRaceEscape.Engine;

/// <summary>
/// A die roller parked after the faces a seeded game has already used.
/// The same seed and the same count always continue with the same face.
/// </summary>
public static class DiceSequence
{
    public static IDieRoller After(int seed, int consumed)
    {
        if (consumed < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(consumed), "Consumed dice cannot be negative.");
        }

        RandomDieRoller roller = new(seed);
        for (int index = 0; index < consumed; index++)
        {
            roller.Roll();
        }

        return roller;
    }
}
