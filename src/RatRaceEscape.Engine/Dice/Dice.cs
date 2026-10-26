namespace RatRaceEscape.Engine;

/// <summary>
/// Rolls one or two dice and checks that every face is on the cube.
/// </summary>
public static class Dice
{
    public const int MinimumFace = 1;

    public const int MaximumFace = 6;

    public const int MinimumCount = 1;

    public const int MaximumCount = 2;

    public static DiceRoll Roll(IDieRoller roller, int count)
    {
        ArgumentNullException.ThrowIfNull(roller);

        if (count is < MinimumCount or > MaximumCount)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Roll one or two dice.");
        }

        int[] faces = new int[count];
        int total = 0;
        for (int index = 0; index < count; index++)
        {
            int face = roller.Roll();
            if (face is < MinimumFace or > MaximumFace)
            {
                throw new InvalidOperationException($"A die face must be from {MinimumFace} to {MaximumFace}.");
            }

            faces[index] = face;
            total += face;
        }

        return new DiceRoll(faces, total);
    }
}
