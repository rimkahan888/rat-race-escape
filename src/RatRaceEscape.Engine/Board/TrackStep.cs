namespace RatRaceEscape.Engine;

/// <summary>
/// One square a token enters. The square it leaves is not a step.
/// </summary>
public sealed class TrackStep
{
    public TrackStep(int index, SpaceKind kind)
    {
        BoardSpace space = RatRaceBoard.At(index);
        if (space.Kind != kind)
        {
            throw new ArgumentException("Kind does not match the space on the loop.", nameof(kind));
        }

        Index = index;
        Kind = kind;
    }

    public int Index { get; }

    public SpaceKind Kind { get; }
}
