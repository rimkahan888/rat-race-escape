using System.Text.Json;

namespace RatRaceEscape.Engine.Tests;

public class SmallDealCatalogTests
{
    private static readonly string[] ExpectedIds =
    [
        "lumen-glass",
        "north-pier",
        "brine-barrel",
        "kite-press",
        "cedar-lane-studio",
        "dockside-room",
        "maple-court-cottage",
        "orchard-gate-shed",
    ];

    [Fact]
    public void All_loads_the_original_deck_in_order()
    {
        Assert.Equal(ExpectedIds, SmallDealCatalog.All.Select(deal => deal.Id));
    }

    [Fact]
    public void Each_card_passes_through_the_same_rules_as_the_factories()
    {
        Assert.Equal(8, SmallDealCatalog.All.Count);

        foreach (SmallDeal deal in SmallDealCatalog.All)
        {
            SmallDeal again = deal.Kind == SmallDealKind.Stock
                ? SmallDeal.Stock(deal.Id, deal.Title, deal.Summary, deal.Symbol!, deal.PricePerShare, deal.DividendPerShare)
                : SmallDeal.Property(deal.Id, deal.Title, deal.Summary, deal.DownPayment, deal.Mortgage, deal.MonthlyCashFlow);

            Assert.Equal(deal.Title, again.Title);
            Assert.Equal(deal.Kind, again.Kind);
            Assert.DoesNotContain("CASHFLOW", deal.Summary, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Rich Dad", deal.Title, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Lumen_glass_is_a_ten_dollar_share_with_no_dividend()
    {
        SmallDeal deal = SmallDealCatalog.GetById("lumen-glass");

        Assert.Equal(SmallDealKind.Stock, deal.Kind);
        Assert.Equal("Lumen Glass", deal.Title);
        Assert.Equal("LUMEN", deal.Symbol);
        Assert.Equal(Money.FromDollars(10), deal.PricePerShare);
        Assert.Equal(Money.Zero, deal.DividendPerShare);
        Assert.Equal(Money.Zero, deal.DownPayment);
    }

    [Fact]
    public void Cedar_lane_studio_names_the_down_payment_mortgage_and_cash_flow()
    {
        SmallDeal deal = SmallDealCatalog.GetById("cedar-lane-studio");

        Assert.Equal(SmallDealKind.Property, deal.Kind);
        Assert.Equal("Cedar Lane Studio", deal.Title);
        Assert.Null(deal.Symbol);
        Assert.Equal(Money.FromDollars(500), deal.DownPayment);
        Assert.Equal(Money.FromDollars(4_500), deal.Mortgage);
        Assert.Equal(Money.FromDollars(20), deal.MonthlyCashFlow);
    }

    [Fact]
    public void The_deck_includes_a_loss_making_cottage_and_a_shed_with_no_note()
    {
        SmallDeal cottage = SmallDealCatalog.GetById("maple-court-cottage");
        SmallDeal shed = SmallDealCatalog.GetById("orchard-gate-shed");

        Assert.Equal(Money.FromDollars(-10), cottage.MonthlyCashFlow);
        Assert.Equal(Money.FromDollars(1_200), cottage.DownPayment);
        Assert.Equal(Money.Zero, shed.Mortgage);
        Assert.Equal(Money.FromDollars(25), shed.MonthlyCashFlow);
    }

    [Fact]
    public void GetById_is_case_sensitive_and_rejects_a_missing_or_null_id()
    {
        Assert.Throws<KeyNotFoundException>(() => SmallDealCatalog.GetById("Lumen-Glass"));
        KeyNotFoundException missing = Assert.Throws<KeyNotFoundException>(() => SmallDealCatalog.GetById("missing"));
        Assert.Contains("No small deal with id 'missing'.", missing.Message, StringComparison.Ordinal);
        Assert.Throws<ArgumentNullException>(() => SmallDealCatalog.GetById(null!));
    }

    [Fact]
    public void CreateDeck_deals_catalog_order_unless_the_shuffle_says_otherwise()
    {
        Deck<SmallDeal> printed = SmallDealCatalog.CreateDeck(new IdentityShuffle());
        DeckDraw<SmallDeal> top = printed.Draw();

        Assert.Equal(SmallDealCatalog.All.Count, printed.DrawCount);
        Assert.Equal("lumen-glass", top.Card.Id);
        Assert.Equal(7, top.Deck.DrawCount);

        ScriptedShuffle shuffle = new([1, 0, 2, 3, 4, 5, 6, 7]);
        Deck<SmallDeal> cut = SmallDealCatalog.CreateDeck(shuffle);

        Assert.Equal("north-pier", cut.Draw().Card.Id);
        Assert.Equal(0, shuffle.Remaining);
    }

    [Fact]
    public void CreateDeck_rejects_a_null_shuffle()
    {
        Assert.Throws<ArgumentNullException>(() => SmallDealCatalog.CreateDeck(null!));
    }

    [Fact]
    public void Parse_rejects_a_duplicate_id()
    {
        string json = "[" + SampleStock("lumen-glass") + "," + SampleStock("lumen-glass") + "]";

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => SmallDealCatalog.Parse(json));

        Assert.Contains("duplicated", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_rejects_a_deal_that_fails_validation_or_has_no_kind()
    {
        string pricedAtNothing = SampleStock("lumen-glass").Replace("\"pricePerShareCents\": 1000", "\"pricePerShareCents\": 0", StringComparison.Ordinal);
        string missingKind = SampleStock("lumen-glass").Replace("\"kind\": \"stock\",", "", StringComparison.Ordinal);
        string weirdKind = SampleStock("lumen-glass").Replace("stock", "boat", StringComparison.Ordinal);

        Assert.Throws<CardValidationException>(() => SmallDealCatalog.Parse("[" + pricedAtNothing + "]"));
        Assert.Throws<JsonException>(() => SmallDealCatalog.Parse("[" + missingKind + "]"));
        JsonException unknown = Assert.Throws<JsonException>(() => SmallDealCatalog.Parse("[" + weirdKind + "]"));
        Assert.Contains("not stock or property", unknown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_rejects_an_empty_catalog_a_hole_and_null_json()
    {
        Assert.Throws<JsonException>(() => SmallDealCatalog.Parse("[]"));
        Assert.Throws<JsonException>(() => SmallDealCatalog.Parse("[null]"));
        Assert.Throws<ArgumentNullException>(() => SmallDealCatalog.Parse(null!));
    }

    [Fact]
    public void Parse_rejects_property_numbers_smuggled_onto_a_stock()
    {
        string json = """
            [
              {
                "id": "lumen-glass",
                "kind": "Stock",
                "title": "Lumen Glass",
                "summary": "A harbor glassworks is selling shares.",
                "symbol": "lumen",
                "pricePerShareCents": 1000,
                "dividendPerShareCents": 0,
                "mortgageCents": 100
              }
            ]
            """;

        CardValidationException exception = Assert.Throws<CardValidationException>(() => SmallDealCatalog.Parse(json));

        Assert.Contains("A stock deal does not have a mortgage.", exception.Errors);
    }

    private static string SampleStock(string id)
    {
        return $$"""
            {
              "id": "{{id}}",
              "kind": "stock",
              "title": "Lumen Glass",
              "summary": "A harbor glassworks is selling shares.",
              "symbol": "LUMEN",
              "pricePerShareCents": 1000,
              "dividendPerShareCents": 0
            }
            """;
    }
}
