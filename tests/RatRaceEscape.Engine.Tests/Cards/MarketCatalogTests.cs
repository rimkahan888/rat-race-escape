using System.Text.Json;

namespace RatRaceEscape.Engine.Tests;

public class MarketCatalogTests
{
    private static readonly string[] ExpectedIds =
    [
        "pier-bid",
        "lumen-bid",
        "cedar-lane-offer",
        "kiln-and-clay-offer",
        "extra-mill-shift",
        "storm-week",
        "harbor-festival",
        "slip-fee",
        "pier-split",
        "lumen-split",
    ];

    [Fact]
    public void All_loads_the_original_deck_in_order()
    {
        Assert.Equal(ExpectedIds, MarketCatalog.All.Select(card => card.Id));
    }

    [Fact]
    public void Offers_name_assets_the_other_decks_can_sell_and_events_move_everyone()
    {
        Assert.Equal(4, MarketCatalog.All.Count(card => card.Kind == MarketKind.Offer));
        Assert.Equal(4, MarketCatalog.All.Count(card => card.Kind == MarketKind.Event));
        Assert.Equal(2, MarketCatalog.All.Count(card => card.Kind == MarketKind.Split));

        foreach (MarketCard card in MarketCatalog.All)
        {
            Assert.DoesNotContain("CASHFLOW", card.Summary, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Rich Dad", card.Title, StringComparison.OrdinalIgnoreCase);

            if (card.Kind == MarketKind.Event)
            {
                Assert.False(card.CashDelta.IsZero);
                Assert.Null(card.AssetName);
                continue;
            }

            if (card.Kind == MarketKind.Split)
            {
                Assert.True(card.CashDelta.IsZero);
                Assert.True(card.Price.IsZero);
                Assert.False(card.PerShare);
                Assert.Contains(SmallDealCatalog.All, deal => deal.Symbol == card.AssetName);
                continue;
            }

            Assert.True(card.Price.IsPositive);
            Assert.True(card.CashDelta.IsZero);
            if (card.PerShare)
            {
                Assert.Contains(SmallDealCatalog.All, deal => deal.Symbol == card.AssetName);
            }
            else
            {
                bool known = SmallDealCatalog.All.Any(deal => deal.Title == card.AssetName)
                    || BigDealCatalog.All.Any(deal => deal.Title == card.AssetName);
                Assert.True(known);
            }
        }
    }

    [Fact]
    public void The_pier_bid_and_the_storm_use_the_authored_amounts()
    {
        MarketCard pier = MarketCatalog.GetById("pier-bid");
        MarketCard storm = MarketCatalog.GetById("storm-week");
        MarketCard studio = MarketCatalog.GetById("cedar-lane-offer");

        Assert.Equal("PIER", pier.AssetName);
        Assert.True(pier.PerShare);
        Assert.Equal(Money.FromDollars(40), pier.Price);
        Assert.Equal("Cedar Lane Studio", studio.AssetName);
        Assert.False(studio.PerShare);
        Assert.Equal(Money.FromDollars(2_500), studio.Price);
        Assert.Equal(Money.FromDollars(-150), storm.CashDelta);
    }

    [Fact]
    public void GetById_is_case_sensitive_and_rejects_a_missing_or_null_id()
    {
        Assert.Throws<KeyNotFoundException>(() => MarketCatalog.GetById("Pier-Bid"));
        KeyNotFoundException missing = Assert.Throws<KeyNotFoundException>(() => MarketCatalog.GetById("missing"));
        Assert.Contains("No market card with id 'missing'.", missing.Message, StringComparison.Ordinal);
        Assert.Throws<ArgumentNullException>(() => MarketCatalog.GetById(null!));
    }

    [Fact]
    public void CreateDeck_deals_catalog_order_unless_the_shuffle_says_otherwise()
    {
        Deck<MarketCard> printed = MarketCatalog.CreateDeck(new IdentityShuffle());

        Assert.Equal("pier-bid", printed.Draw().Card.Id);

        ScriptedShuffle shuffle = new([4, 1, 2, 3, 0, 5, 6, 7, 8, 9]);
        Deck<MarketCard> cut = MarketCatalog.CreateDeck(shuffle);

        Assert.Equal("extra-mill-shift", cut.Draw().Card.Id);
        Assert.Equal(0, shuffle.Remaining);
    }

    [Fact]
    public void CreateDeck_rejects_a_null_shuffle()
    {
        Assert.Throws<ArgumentNullException>(() => MarketCatalog.CreateDeck(null!));
    }

    [Fact]
    public void Parse_rejects_a_duplicate_a_bad_kind_an_empty_catalog_and_null_json()
    {
        string card = SampleOffer("pier-bid");

        InvalidOperationException duplicate = Assert.Throws<InvalidOperationException>(() =>
            MarketCatalog.Parse("[" + card + "," + card + "]"));
        JsonException kind = Assert.Throws<JsonException>(() =>
            MarketCatalog.Parse("[" + card.Replace("offer", "sale", StringComparison.Ordinal) + "]"));

        Assert.Contains("duplicated", duplicate.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not an offer, an event, or a split", kind.Message, StringComparison.Ordinal);
        Assert.Throws<JsonException>(() => MarketCatalog.Parse("[]"));
        Assert.Throws<JsonException>(() => MarketCatalog.Parse("[null]"));
        Assert.Throws<ArgumentNullException>(() => MarketCatalog.Parse(null!));
    }

    [Fact]
    public void Parse_rejects_an_event_that_does_not_move_cash()
    {
        string json = """
            [
              {
                "id": "storm-week",
                "kind": "Event",
                "title": "Storm Week",
                "summary": "A week of storms hits every household.",
                "cashDeltaCents": 0
              }
            ]
            """;

        CardValidationException exception = Assert.Throws<CardValidationException>(() => MarketCatalog.Parse(json));

        Assert.Contains("An event must change everyone's cash.", exception.Errors);
    }

    private static string SampleOffer(string id)
    {
        return $$"""
            {
              "id": "{{id}}",
              "kind": "offer",
              "title": "Bid for North Pier",
              "summary": "A locker company will pay forty dollars a share.",
              "assetName": "PIER",
              "perShare": true,
              "offerCents": 4000
            }
            """;
    }
}
