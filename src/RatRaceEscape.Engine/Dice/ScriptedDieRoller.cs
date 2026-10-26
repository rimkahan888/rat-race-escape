namespace RatRaceEscape.Engine;

/// <summary>
/// Returns faces in a fixed order. The sequence is the test double for movement
/// and turns: when it runs out, the next roll fails instead of inventing a face.
/// </summary>
public sealed class ScriptedDieRoller : IDieRoller
{
    private readonly Queue<int> _faces;

    public ScriptedDieRoller(IEnumerable<int> faces)
    {
        ArgumentNullException.ThrowIfNull(faces);

        Queue<int> pending = new();
        foreach (int face in faces)
        {
            if (face is < Dice.MinimumFace or > Dice.MaximumFace)
            {
                throw new ArgumentOutOfRangeException(nameof(faces), "A die face must be from 1 to 6.");
            }

            pending.Enqueue(face);
        }

        if (pending.Count == 0)
        {
            throw new ArgumentException("At least one scripted face is required.", nameof(faces));
        }

        _faces = pending;
    }

    public int Remaining => _faces.Count;

    public int Roll()
    {
        if (_faces.Count == 0)
        {
            throw new InvalidOperationException("No scripted faces remain.");
        }

        return _faces.Dequeue();
    }
}
