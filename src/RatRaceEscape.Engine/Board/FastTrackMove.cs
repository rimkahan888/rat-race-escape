namespace RatRaceEscape.Engine;

/// <summary>
/// A token's walk around the fast track. Paydays are counted, including the
/// square the token lands on. The square it was already standing on is not.
/// </summary>
public sealed class FastTrackMove
{
    public FastTrackMove(int fromIndex, int steps, int toIndex, int paydays)
    {
        if (fromIndex < 0 || fromIndex >= FastTrackBoard.SpaceCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fromIndex),
                $"Board index must be from 0 to {FastTrackBoard.SpaceCount - 1}.");
        }

        if (steps < RatRaceTrack.MinimumSteps || steps > RatRaceTrack.MaximumSteps)
        {
            throw new ArgumentOutOfRangeException(
                nameof(steps),
                $"A move is from {RatRaceTrack.MinimumSteps} to {RatRaceTrack.MaximumSteps} spaces.");
        }

        int expected = (fromIndex + steps) % FastTrackBoard.SpaceCount;
        if (toIndex != expected)
        {
            throw new ArgumentException("The stop must follow the loop.", nameof(toIndex));
        }

        if (paydays < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(paydays), "Payday count cannot be negative.");
        }

        FromIndex = fromIndex;
        Steps = steps;
        ToIndex = toIndex;
        Paydays = paydays;
    }

    public int FromIndex { get; }

    public int Steps { get; }

    public int ToIndex { get; }

    public int Paydays { get; }
}
