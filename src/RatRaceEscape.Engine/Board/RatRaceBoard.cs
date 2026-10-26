namespace RatRaceEscape.Engine;

/// <summary>
/// The rat-race loop. Tokens start on the opening offer and move clockwise.
/// Pay windows sit six spaces apart, so a single die can land on one and a
/// longer roll can pass one.
/// </summary>
public static class RatRaceBoard
{
    public const int SpaceCount = 24;

    private const string OfferTitle = "Offer Table";

    private const string OfferSummary = "A venture is on offer for anyone who wants to buy it.";

    private const string BillTitle = "Odd Bill";

    private const string BillSummary = "An unplanned bill is waiting on the table.";

    private const string AidTitle = "Mutual Aid";

    private const string AidSummary = "Neighbors are collecting for someone who came up short.";

    private const string MarketTitle = "Corner Market";

    private const string MarketSummary = "Buyers on the square are naming prices for assets.";

    private const string PayTitle = "Pay Window";

    private const string PaySummary = "Wages for the month are paid out at the booth.";

    private const string ArrivalTitle = "New Arrival";

    private const string ArrivalSummary = "Another dependent joins the household.";

    private const string HoursTitle = "Hours Cut";

    private const string HoursSummary = "The shift list drops your hours, and the bills stay.";

    private static readonly (SpaceKind Kind, string Title, string Summary)[] Layout =
    [
        (SpaceKind.Deal, OfferTitle, OfferSummary),
        (SpaceKind.Doodad, BillTitle, BillSummary),
        (SpaceKind.Deal, OfferTitle, OfferSummary),
        (SpaceKind.Charity, AidTitle, AidSummary),
        (SpaceKind.Deal, OfferTitle, OfferSummary),
        (SpaceKind.Market, MarketTitle, MarketSummary),
        (SpaceKind.Payday, PayTitle, PaySummary),
        (SpaceKind.Deal, OfferTitle, OfferSummary),
        (SpaceKind.Baby, ArrivalTitle, ArrivalSummary),
        (SpaceKind.Deal, OfferTitle, OfferSummary),
        (SpaceKind.Doodad, BillTitle, BillSummary),
        (SpaceKind.Deal, OfferTitle, OfferSummary),
        (SpaceKind.Payday, PayTitle, PaySummary),
        (SpaceKind.Downsized, HoursTitle, HoursSummary),
        (SpaceKind.Deal, OfferTitle, OfferSummary),
        (SpaceKind.Market, MarketTitle, MarketSummary),
        (SpaceKind.Deal, OfferTitle, OfferSummary),
        (SpaceKind.Doodad, BillTitle, BillSummary),
        (SpaceKind.Payday, PayTitle, PaySummary),
        (SpaceKind.Charity, AidTitle, AidSummary),
        (SpaceKind.Deal, OfferTitle, OfferSummary),
        (SpaceKind.Market, MarketTitle, MarketSummary),
        (SpaceKind.Deal, OfferTitle, OfferSummary),
        (SpaceKind.Doodad, BillTitle, BillSummary),
    ];

    public static IReadOnlyList<BoardSpace> Spaces { get; } = Build();

    public static BoardSpace At(int index)
    {
        if (index < 0 || index >= SpaceCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index),
                $"Board index must be from 0 to {SpaceCount - 1}.");
        }

        return Spaces[index];
    }

    private static BoardSpace[] Build()
    {
        if (Layout.Length != SpaceCount)
        {
            throw new InvalidOperationException($"The rat race must have {SpaceCount} spaces.");
        }

        BoardSpace[] spaces = new BoardSpace[SpaceCount];
        HashSet<SpaceKind> kinds = [];
        for (int index = 0; index < Layout.Length; index++)
        {
            (SpaceKind kind, string title, string summary) = Layout[index];
            spaces[index] = BoardSpace.Create(index, kind, title, summary);
            kinds.Add(kind);
        }

        foreach (SpaceKind kind in Enum.GetValues<SpaceKind>())
        {
            if (!kinds.Contains(kind))
            {
                throw new InvalidOperationException($"The rat race is missing a {kind} space.");
            }
        }

        return spaces;
    }
}
