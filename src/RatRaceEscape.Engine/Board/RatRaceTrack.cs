namespace RatRaceEscape.Engine;

/// <summary>
/// Moves a token clockwise around <see cref="RatRaceBoard"/>. One step enters
/// the next square. The count stops at two dice, which is as far as a turn rolls.
/// </summary>
public static class RatRaceTrack
{
    public const int MinimumSteps = 1;

    public const int MaximumSteps = Dice.MaximumCount * Dice.MaximumFace;

    public static TrackMove Move(int fromIndex, int steps)
    {
        if (fromIndex < 0 || fromIndex >= RatRaceBoard.SpaceCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fromIndex),
                $"Board index must be from 0 to {RatRaceBoard.SpaceCount - 1}.");
        }

        if (steps < MinimumSteps || steps > MaximumSteps)
        {
            throw new ArgumentOutOfRangeException(
                nameof(steps),
                $"A move is from {MinimumSteps} to {MaximumSteps} spaces.");
        }

        TrackStep[] visited = new TrackStep[steps];
        int index = fromIndex;
        for (int step = 0; step < steps; step++)
        {
            index = (index + 1) % RatRaceBoard.SpaceCount;
            BoardSpace space = RatRaceBoard.At(index);
            visited[step] = new TrackStep(index, space.Kind);
        }

        return new TrackMove(fromIndex, steps, visited);
    }
}
