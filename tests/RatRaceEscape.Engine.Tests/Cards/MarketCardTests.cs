namespace RatRaceEscape.Engine.Tests;

public class MarketCardTests
{
    [Fact]
    public void A_share_offer_uppercases_the_symbol_and_does_not_move_cash()
    {
        MarketCard card = MarketCard.Offer(
            "pier-bid",
            "  Bid for North Pier  ",
            "  A locker company will pay forty dollars a share.  ",
            " pier ",
            perShare: true,
            Money.FromDollars(40));

        Assert.Equal(MarketKind.Offer, card.Kind);
        Assert.Equal("Bid for North Pier", card.Title);
        Assert.Equal("PIER", card.AssetName);
        Assert.True(card.PerShare);
        Assert.Equal(Money.FromDollars(40), card.Price);
        Assert.Equal(Money.Zero, card.CashDelta);
    }

    [Fact]
    public void An_event_can_pay_the_table_or_collect_from_it()
    {
        MarketCard paid = MarketCard.Event(
            "extra-mill-shift",
            "Extra Mill Shift",
            "The brickworks adds a shift this month.",
            Money.FromDollars(200));
        MarketCard billed = MarketCard.Event(
            "storm-week",
            "Storm Week",
            "A week of storms hits every household.",
            Money.FromDollars(-150));

        Assert.Equal(MarketKind.Event, paid.Kind);
        Assert.Null(paid.AssetName);
        Assert.False(paid.PerShare);
        Assert.Equal(Money.Zero, paid.Price);
        Assert.Equal(Money.FromDollars(200), paid.CashDelta);
        Assert.Equal(Money.FromDollars(-150), billed.CashDelta);
    }

    [Fact]
    public void An_offer_reports_a_bad_asset_price_and_cash_delta_together()
    {
        CardValidationException shares = Assert.Throws<CardValidationException>(() =>
            MarketCard.Create(
                "pier-bid",
                MarketKind.Offer,
                "Bid for North Pier",
                "A locker company will pay forty dollars a share.",
                "1BAD",
                perShare: true,
                Money.Zero,
                Money.FromDollars(10)));
        CardValidationException whole = Assert.Throws<CardValidationException>(() =>
            MarketCard.Create(
                "cedar-lane-offer",
                MarketKind.Offer,
                "Offer on Cedar Lane",
                "A cabinet maker wants the studio.",
                " ",
                perShare: false,
                Money.FromDollars(-1),
                Money.Zero));

        Assert.Contains("A share offer needs a stock symbol.", shares.Errors);
        Assert.Contains("An offer price must be greater than zero.", shares.Errors);
        Assert.Contains("An offer does not change everyone's cash.", shares.Errors);
        Assert.Contains("An offer needs the asset it wants to buy.", whole.Errors);
        Assert.Contains("An offer price must be greater than zero.", whole.Errors);
    }

    [Fact]
    public void An_event_rejects_an_asset_a_share_price_and_a_zero_delta()
    {
        CardValidationException exception = Assert.Throws<CardValidationException>(() =>
            MarketCard.Create(
                "storm-week",
                MarketKind.Event,
                "Storm Week",
                "A week of storms hits every household.",
                "PIER",
                perShare: true,
                Money.FromDollars(40),
                Money.Zero));

        Assert.Contains("An event does not name an asset.", exception.Errors);
        Assert.Contains("An event is not priced per share.", exception.Errors);
        Assert.Contains("An event does not name an offer price.", exception.Errors);
        Assert.Contains("An event must change everyone's cash.", exception.Errors);
    }

    [Fact]
    public void Create_rejects_null_wording_and_a_null_asset_on_the_offer_factory()
    {
        Assert.Throws<ArgumentNullException>(() =>
            MarketCard.Offer(null!, "Title", "A long enough summary.", "PIER", true, Money.FromDollars(1)));
        Assert.Throws<ArgumentNullException>(() =>
            MarketCard.Event("storm-week", null!, "A long enough summary.", Money.FromDollars(1)));
        Assert.Throws<ArgumentNullException>(() =>
            MarketCard.Event("storm-week", "Title", null!, Money.FromDollars(1)));
        Assert.Throws<ArgumentNullException>(() =>
            MarketCard.Offer("pier-bid", "Title", "A long enough summary.", null!, true, Money.FromDollars(1)));
    }
}
