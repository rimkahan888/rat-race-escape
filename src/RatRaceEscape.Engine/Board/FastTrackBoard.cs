namespace RatRaceEscape.Engine;

/// <summary>
/// The fast track. A player who has left the rat race starts on the opening
/// distribution desk and moves clockwise. Paydays sit four spaces apart.
/// A windfall pays once, on the square the token lands on. A setback collects
/// once, the same way.
/// </summary>
public static class FastTrackBoard
{
    public const int SpaceCount = 12;

    public static readonly Money Windfall = Money.FromDollars(1_000);

    public static readonly Money Setback = Money.FromDollars(300);

    public static readonly Money DreamPrice = Money.FromDollars(5_000);

    private const string PayTitle = "Distribution Desk";

    private const string PaySummary = "Passive income is paid out for the month.";

    private const string BonusTitle = "Harbor Bonus";

    private const string BonusSummary = "A charter pays a one-time bonus to whoever is standing here.";

    private const string DockTitle = "Dry Dock";

    private const string DockSummary = "Repairs come due on a boat that is not even sailing.";

    private const string DreamTitle = "Dream Listing";

    private const string DreamSummary = "A life you wanted is for sale, if the cash is there.";

    private static readonly (FastTrackKind Kind, string Title, string Summary)[] Layout =
    [
        (FastTrackKind.Payday, PayTitle, PaySummary),
        (FastTrackKind.Windfall, BonusTitle, BonusSummary),
        (FastTrackKind.Setback, DockTitle, DockSummary),
        (FastTrackKind.Dream, DreamTitle, DreamSummary),
        (FastTrackKind.Payday, PayTitle, PaySummary),
        (FastTrackKind.Windfall, BonusTitle, BonusSummary),
        (FastTrackKind.Setback, DockTitle, DockSummary),
        (FastTrackKind.Dream, DreamTitle, DreamSummary),
        (FastTrackKind.Payday, PayTitle, PaySummary),
        (FastTrackKind.Windfall, BonusTitle, BonusSummary),
        (FastTrackKind.Setback, DockTitle, DockSummary),
        (FastTrackKind.Dream, DreamTitle, DreamSummary),
    ];

    public static IReadOnlyList<FastTrackSpace> Spaces { get; } = Build();

    public static FastTrackSpace At(int index)
    {
        if (index < 0 || index >= SpaceCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index),
                $"Board index must be from 0 to {SpaceCount - 1}.");
        }

        return Spaces[index];
    }

    public static FastTrackMove Move(int fromIndex, int steps)
    {
        if (fromIndex < 0 || fromIndex >= SpaceCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fromIndex),
                $"Board index must be from 0 to {SpaceCount - 1}.");
        }

        if (steps < RatRaceTrack.MinimumSteps || steps > RatRaceTrack.MaximumSteps)
        {
            throw new ArgumentOutOfRangeException(
                nameof(steps),
                $"A move is from {RatRaceTrack.MinimumSteps} to {RatRaceTrack.MaximumSteps} spaces.");
        }

        int index = fromIndex;
        int paydays = 0;
        for (int step = 0; step < steps; step++)
        {
            index = (index + 1) % SpaceCount;
            if (At(index).Kind == FastTrackKind.Payday)
            {
                paydays++;
            }
        }

        return new FastTrackMove(fromIndex, steps, index, paydays);
    }

    private static FastTrackSpace[] Build()
    {
        if (Layout.Length != SpaceCount)
        {
            throw new InvalidOperationException($"The fast track must have {SpaceCount} spaces.");
        }

        FastTrackSpace[] spaces = new FastTrackSpace[SpaceCount];
        HashSet<FastTrackKind> kinds = [];
        for (int index = 0; index < Layout.Length; index++)
        {
            (FastTrackKind kind, string title, string summary) = Layout[index];
            spaces[index] = FastTrackSpace.Create(index, kind, title, summary);
            kinds.Add(kind);
        }

        foreach (FastTrackKind kind in Enum.GetValues<FastTrackKind>())
        {
            if (!kinds.Contains(kind))
            {
                throw new InvalidOperationException($"The fast track is missing a {kind} space.");
            }
        }

        return spaces;
    }
}
