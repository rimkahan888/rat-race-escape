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

    /// <summary>
    /// The least expensive dream. The other listings cost more, so a player
    /// who just left the rat race needs several laps of paydays to buy one.
    /// </summary>
    public static readonly Money DreamPrice = Money.FromDollars(100_000);

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

    public static IReadOnlyList<FastTrackDeal> Deals { get; } =
    [
        FastTrackDeal.Create(
            "slip-lease",
            "Slip Lease",
            "A leased slip beside the harbor office. It pays every month.",
            Money.FromDollars(8_000),
            Money.FromDollars(500)),
        FastTrackDeal.Create(
            "charter-book",
            "Charter Book",
            "A season of weekend charters, paid for up front.",
            Money.FromDollars(20_000),
            Money.FromDollars(1_200)),
        FastTrackDeal.Create(
            "marina-share",
            "Marina Share",
            "A share of the marina office and its mooring fees.",
            Money.FromDollars(45_000),
            Money.FromDollars(2_500)),
    ];

    public static FastTrackDeal? FindDeal(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        foreach (FastTrackDeal deal in Deals)
        {
            if (deal.Id.Equals(id.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return deal;
            }
        }

        return null;
    }

    public static Money PriceAt(int index)
    {
        if (At(index).Kind != FastTrackKind.Dream)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "That space is not a dream listing.");
        }

        return index switch
        {
            3 => Money.FromDollars(100_000),
            7 => Money.FromDollars(150_000),
            11 => Money.FromDollars(200_000),
            _ => throw new InvalidOperationException("A dream listing is missing its price."),
        };
    }

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
