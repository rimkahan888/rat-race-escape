namespace RatRaceEscape.Engine;

/// <summary>
/// The card in front of the current player. Exactly one of the four decks
/// supplied it. The turn keeps it until the player moves on, then that deck
/// takes it back onto the discard pile.
/// </summary>
public sealed class DrawnCard
{
    private DrawnCard(SmallDeal? smallDeal, BigDeal? bigDeal, MarketCard? market, Doodad? doodad)
    {
        SmallDeal = smallDeal;
        BigDeal = bigDeal;
        Market = market;
        Doodad = doodad;
    }

    public DeckHook Deck =>
        SmallDeal is not null || BigDeal is not null ? DeckHook.Deal
        : Market is not null ? DeckHook.Market
        : DeckHook.Doodad;

    public string Id => SmallDeal?.Id ?? BigDeal?.Id ?? Market?.Id ?? Doodad!.Id;

    public string Title => SmallDeal?.Title ?? BigDeal?.Title ?? Market?.Title ?? Doodad!.Title;

    public string Summary => SmallDeal?.Summary ?? BigDeal?.Summary ?? Market?.Summary ?? Doodad!.Summary;

    public SmallDeal? SmallDeal { get; }

    public BigDeal? BigDeal { get; }

    public MarketCard? Market { get; }

    public Doodad? Doodad { get; }

    public static DrawnCard From(SmallDeal deal)
    {
        ArgumentNullException.ThrowIfNull(deal);
        return new DrawnCard(deal, null, null, null);
    }

    public static DrawnCard From(BigDeal deal)
    {
        ArgumentNullException.ThrowIfNull(deal);
        return new DrawnCard(null, deal, null, null);
    }

    public static DrawnCard From(MarketCard card)
    {
        ArgumentNullException.ThrowIfNull(card);
        return new DrawnCard(null, null, card, null);
    }

    public static DrawnCard From(Doodad doodad)
    {
        ArgumentNullException.ThrowIfNull(doodad);
        return new DrawnCard(null, null, null, doodad);
    }
}
