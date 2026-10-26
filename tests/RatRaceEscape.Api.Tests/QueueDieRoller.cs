using RatRaceEscape.Engine;

namespace RatRaceEscape.Api.Tests;

/// <summary>
/// Faces queued by an API test. The host rolls them in order.
/// </summary>
public sealed class QueueDieRoller : IDieRoller
{
    private readonly Lock _gate = new();
    private readonly Queue<int> _faces = new();

    public void Enqueue(params int[] faces)
    {
        ArgumentNullException.ThrowIfNull(faces);

        lock (_gate)
        {
            foreach (int face in faces)
            {
                if (face is < Dice.MinimumFace or > Dice.MaximumFace)
                {
                    throw new ArgumentOutOfRangeException(nameof(faces), "A die face must be from 1 to 6.");
                }

                _faces.Enqueue(face);
            }
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _faces.Clear();
        }
    }

    public int Roll()
    {
        lock (_gate)
        {
            if (_faces.Count == 0)
            {
                throw new InvalidOperationException("No scripted faces remain.");
            }

            return _faces.Dequeue();
        }
    }
}
