namespace RatRaceEscape.Engine;

/// <summary>
/// A token's walk around the rat race: where it started, the squares it
/// entered, and where it stopped. Paydays in <see cref="Visited"/> are also
/// listed on their own so a later payday can pay each one once.
/// </summary>
public sealed class TrackMove
{
    public TrackMove(int fromIndex, int steps, IReadOnlyList<TrackStep> visited)
    {
        ArgumentNullException.ThrowIfNull(visited);

        if (fromIndex < 0 || fromIndex >= RatRaceBoard.SpaceCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fromIndex),
                $"Board index must be from 0 to {RatRaceBoard.SpaceCount - 1}.");
        }

        if (steps < RatRaceTrack.MinimumSteps || steps > RatRaceTrack.MaximumSteps)
        {
            throw new ArgumentOutOfRangeException(
                nameof(steps),
                $"A move is from {RatRaceTrack.MinimumSteps} to {RatRaceTrack.MaximumSteps} spaces.");
        }

        if (visited.Count != steps)
        {
            throw new ArgumentException("Every step must be recorded.", nameof(visited));
        }

        TrackStep[] copy = new TrackStep[steps];
        List<TrackStep> paydays = new(steps);
        int expected = fromIndex;
        for (int index = 0; index < steps; index++)
        {
            TrackStep? step = visited[index];
            if (step is null)
            {
                throw new ArgumentException("A step is required.", nameof(visited));
            }

            expected = (expected + 1) % RatRaceBoard.SpaceCount;
            if (step.Index != expected)
            {
                throw new ArgumentException("Steps must follow the loop.", nameof(visited));
            }

            copy[index] = step;
            if (step.Kind == SpaceKind.Payday)
            {
                paydays.Add(step);
            }
        }

        FromIndex = fromIndex;
        Steps = steps;
        ToIndex = expected;
        Visited = copy;
        Paydays = paydays.ToArray();
    }

    public int FromIndex { get; }

    public int Steps { get; }

    public int ToIndex { get; }

    public IReadOnlyList<TrackStep> Visited { get; }

    /// <summary>
    /// Payday squares entered on this move, including the one the token lands on.
    /// </summary>
    public IReadOnlyList<TrackStep> Paydays { get; }
}
