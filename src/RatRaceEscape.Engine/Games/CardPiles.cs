namespace RatRaceEscape.Engine;

/// <summary>
/// The four decks on the table. Building them shuffles each catalog.
/// </summary>
public sealed class CardPiles
{
    private CardPiles(
        Deck<SmallDeal> smallDeals,
        Deck<BigDeal> bigDeals,
        Deck<MarketCard> market,
        Deck<Doodad> doodads)
    {
        SmallDeals = smallDeals;
        BigDeals = bigDeals;
        Market = market;
        Doodads = doodads;
    }

    public Deck<SmallDeal> SmallDeals { get; }

    public Deck<BigDeal> BigDeals { get; }

    public Deck<MarketCard> Market { get; }

    public Deck<Doodad> Doodads { get; }

    public static CardPiles Create(IShuffle shuffle)
    {
        ArgumentNullException.ThrowIfNull(shuffle);
        return new CardPiles(
            SmallDealCatalog.CreateDeck(shuffle),
            BigDealCatalog.CreateDeck(shuffle),
            MarketCatalog.CreateDeck(shuffle),
            DoodadCatalog.CreateDeck(shuffle));
    }

    public CardPiles WithSmallDeals(Deck<SmallDeal> smallDeals)
    {
        ArgumentNullException.ThrowIfNull(smallDeals);
        return new CardPiles(smallDeals, BigDeals, Market, Doodads);
    }

    public CardPiles WithBigDeals(Deck<BigDeal> bigDeals)
    {
        ArgumentNullException.ThrowIfNull(bigDeals);
        return new CardPiles(SmallDeals, bigDeals, Market, Doodads);
    }

    public CardPiles WithMarket(Deck<MarketCard> market)
    {
        ArgumentNullException.ThrowIfNull(market);
        return new CardPiles(SmallDeals, BigDeals, market, Doodads);
    }

    public CardPiles WithDoodads(Deck<Doodad> doodads)
    {
        ArgumentNullException.ThrowIfNull(doodads);
        return new CardPiles(SmallDeals, BigDeals, Market, doodads);
    }
}
