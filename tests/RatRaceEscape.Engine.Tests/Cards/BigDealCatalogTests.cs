using System.Text.Json;

namespace RatRaceEscape.Engine.Tests;

public class BigDealCatalogTests
{
    private static readonly string[] ExpectedIds =
    [
        "brickworks-lofts",
        "old-cannery",
        "ridge-orchard",
        "lantern-yard",
        "lantern-ferry",
        "kiln-and-clay",
        "night-market-hall",
        "tide-press-bindery",
    ];

    [Fact]
    public void All_loads_the_original_deck_in_order()
    {
        Assert.Equal(ExpectedIds, BigDealCatalog.All.Select(deal => deal.Id));
    }

    [Fact]
    public void Each_card_is_larger_than_a_small_property_and_uses_original_wording()
    {
        Money largestSmallDown = Money.Zero;
        foreach (SmallDeal small in SmallDealCatalog.All)
        {
            if (small.Kind == SmallDealKind.Property && small.DownPayment > largestSmallDown)
            {
                largestSmallDown = small.DownPayment;
            }
        }

        Assert.Equal(8, BigDealCatalog.All.Count);
        Assert.Equal(4, BigDealCatalog.All.Count(deal => deal.Kind == BigDealKind.Property));
        Assert.Equal(4, BigDealCatalog.All.Count(deal => deal.Kind == BigDealKind.Business));

        foreach (BigDeal deal in BigDealCatalog.All)
        {
            BigDeal again = deal.Kind == BigDealKind.Property
                ? BigDeal.Property(deal.Id, deal.Title, deal.Summary, deal.DownPayment, deal.Loan, deal.MonthlyCashFlow)
                : BigDeal.Business(deal.Id, deal.Title, deal.Summary, deal.DownPayment, deal.Loan, deal.MonthlyCashFlow);

            Assert.Equal(deal.Title, again.Title);
            Assert.True(deal.DownPayment > largestSmallDown);
            Assert.DoesNotContain("CASHFLOW", deal.Summary, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Rich Dad", deal.Title, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Brickworks_lofts_names_the_down_payment_mortgage_and_cash_flow()
    {
        BigDeal deal = BigDealCatalog.GetById("brickworks-lofts");

        Assert.Equal(BigDealKind.Property, deal.Kind);
        Assert.Equal("Brickworks Lofts", deal.Title);
        Assert.Equal(Money.FromDollars(4_000), deal.DownPayment);
        Assert.Equal(Money.FromDollars(28_000), deal.Loan);
        Assert.Equal(Money.FromDollars(220), deal.MonthlyCashFlow);
    }

    [Fact]
    public void The_deck_includes_a_ferry_note_a_debt_free_kiln_and_a_yard_that_loses_money()
    {
        BigDeal ferry = BigDealCatalog.GetById("lantern-ferry");
        BigDeal kiln = BigDealCatalog.GetById("kiln-and-clay");
        BigDeal yard = BigDealCatalog.GetById("lantern-yard");

        Assert.Equal(BigDealKind.Business, ferry.Kind);
        Assert.Equal(Money.FromDollars(5_000), ferry.DownPayment);
        Assert.Equal(Money.FromDollars(15_000), ferry.Loan);
        Assert.Equal(Money.FromDollars(320), ferry.MonthlyCashFlow);
        Assert.Equal(Money.Zero, kiln.Loan);
        Assert.Equal(Money.FromDollars(180), kiln.MonthlyCashFlow);
        Assert.Equal(BigDealKind.Property, yard.Kind);
        Assert.Equal(Money.FromDollars(-80), yard.MonthlyCashFlow);
    }

    [Fact]
    public void GetById_is_case_sensitive_and_rejects_a_missing_or_null_id()
    {
        Assert.Throws<KeyNotFoundException>(() => BigDealCatalog.GetById("Brickworks-Lofts"));
        KeyNotFoundException missing = Assert.Throws<KeyNotFoundException>(() => BigDealCatalog.GetById("missing"));
        Assert.Contains("No big deal with id 'missing'.", missing.Message, StringComparison.Ordinal);
        Assert.Throws<ArgumentNullException>(() => BigDealCatalog.GetById(null!));
    }

    [Fact]
    public void CreateDeck_deals_catalog_order_unless_the_shuffle_says_otherwise()
    {
        Deck<BigDeal> printed = BigDealCatalog.CreateDeck(new IdentityShuffle());
        DeckDraw<BigDeal> top = printed.Draw();

        Assert.Equal(BigDealCatalog.All.Count, printed.DrawCount);
        Assert.Equal("brickworks-lofts", top.Card.Id);

        ScriptedShuffle shuffle = new([1, 0, 2, 3, 4, 5, 6, 7]);
        Deck<BigDeal> cut = BigDealCatalog.CreateDeck(shuffle);

        Assert.Equal("old-cannery", cut.Draw().Card.Id);
        Assert.Equal(0, shuffle.Remaining);
    }

    [Fact]
    public void CreateDeck_rejects_a_null_shuffle()
    {
        Assert.Throws<ArgumentNullException>(() => BigDealCatalog.CreateDeck(null!));
    }

    [Fact]
    public void Parse_rejects_a_duplicate_a_bad_kind_an_empty_catalog_and_null_json()
    {
        string card = Sample("brickworks-lofts");

        InvalidOperationException duplicate = Assert.Throws<InvalidOperationException>(() =>
            BigDealCatalog.Parse("[" + card + "," + card + "]"));
        JsonException kind = Assert.Throws<JsonException>(() =>
            BigDealCatalog.Parse("[" + card.Replace("property", "stock", StringComparison.Ordinal) + "]"));

        Assert.Contains("duplicated", duplicate.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not property or business", kind.Message, StringComparison.Ordinal);
        Assert.Throws<JsonException>(() => BigDealCatalog.Parse("[]"));
        Assert.Throws<JsonException>(() => BigDealCatalog.Parse("[null]"));
        Assert.Throws<ArgumentNullException>(() => BigDealCatalog.Parse(null!));
    }

    [Fact]
    public void Parse_rejects_a_down_payment_of_zero()
    {
        string json = Sample("brickworks-lofts").Replace("\"downPaymentCents\": 400000", "\"downPaymentCents\": 0", StringComparison.Ordinal);

        CardValidationException exception = Assert.Throws<CardValidationException>(() => BigDealCatalog.Parse("[" + json + "]"));

        Assert.Contains("Down payment must be greater than zero.", exception.Errors);
    }

    private static string Sample(string id)
    {
        return $$"""
            {
              "id": "{{id}}",
              "kind": "property",
              "title": "Brickworks Lofts",
              "summary": "Four flats in the old brickworks.",
              "downPaymentCents": 400000,
              "loanCents": 2800000,
              "monthlyCashFlowCents": 22000
            }
            """;
    }
}
