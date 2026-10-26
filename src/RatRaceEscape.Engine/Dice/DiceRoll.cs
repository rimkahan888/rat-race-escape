namespace RatRaceEscape.Engine;

/// <summary>
/// The faces from one throw, and their total, which is how far a token moves.
/// </summary>
public sealed class DiceRoll
{
    public DiceRoll(IReadOnlyList<int> faces, int total)
    {
        ArgumentNullException.ThrowIfNull(faces);

        if (faces.Count is < Dice.MinimumCount or > Dice.MaximumCount)
        {
            throw new ArgumentException("A roll has one or two faces.", nameof(faces));
        }

        int[] copy = new int[faces.Count];
        int sum = 0;
        for (int index = 0; index < faces.Count; index++)
        {
            int face = faces[index];
            if (face is < Dice.MinimumFace or > Dice.MaximumFace)
            {
                throw new ArgumentOutOfRangeException(nameof(faces), "A die face must be from 1 to 6.");
            }

            copy[index] = face;
            sum += face;
        }

        if (sum != total)
        {
            throw new ArgumentException("Total must equal the faces.", nameof(total));
        }

        Faces = copy;
        Total = total;
    }

    public IReadOnlyList<int> Faces { get; }

    public int Total { get; }
}
