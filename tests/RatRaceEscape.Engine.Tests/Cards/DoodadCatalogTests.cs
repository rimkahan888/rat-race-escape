using System.Text.Json;

namespace RatRaceEscape.Engine.Tests;

public class DoodadCatalogTests
{
    private static readonly string[] ExpectedIds =
    [
        "cracked-oven-door",
        "harbor-parking-fine",
        "school-trip-fund",
        "burst-water-heater",
        "phone-screen",
        "vet-visit",
        "train-fare-home",
        "roof-leak-patch",
    ];

    [Fact]
    public void All_loads_the_original_deck_in_order()
    {
        Assert.Equal(ExpectedIds, DoodadCatalog.All.Select(doodad => doodad.Id));
    }

    [Fact]
    public void Every_bill_is_an_original_cost_a_new_player_can_cover()
    {
        Money smallestSavings = ProfessionCatalog.All[0].StartingSavings;
        foreach (Profession profession in ProfessionCatalog.All)
        {
            if (profession.StartingSavings < smallestSavings)
            {
                smallestSavings = profession.StartingSavings;
            }
        }

        Assert.Equal(8, DoodadCatalog.All.Count);

        foreach (Doodad doodad in DoodadCatalog.All)
        {
            Doodad again = Doodad.Create(doodad.Id, doodad.Title, doodad.Summary, doodad.Cost);

            Assert.Equal(doodad.Title, again.Title);
            Assert.True(doodad.Cost.IsPositive);
            Assert.True(doodad.Cost <= smallestSavings);
            Assert.DoesNotContain("CASHFLOW", doodad.Summary, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Rich Dad", doodad.Title, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void The_oven_door_and_the_parking_fine_use_the_authored_costs()
    {
        Doodad oven = DoodadCatalog.GetById("cracked-oven-door");
        Doodad fine = DoodadCatalog.GetById("harbor-parking-fine");
        Doodad roof = DoodadCatalog.GetById("roof-leak-patch");

        Assert.Equal("Cracked Oven Door", oven.Title);
        Assert.Equal(Money.FromDollars(180), oven.Cost);
        Assert.Equal(Money.FromDollars(40), fine.Cost);
        Assert.Equal(Money.FromDollars(400), roof.Cost);
    }

    [Fact]
    public void GetById_is_case_sensitive_and_rejects_a_missing_or_null_id()
    {
        Assert.Throws<KeyNotFoundException>(() => DoodadCatalog.GetById("Cracked-Oven-Door"));
        KeyNotFoundException missing = Assert.Throws<KeyNotFoundException>(() => DoodadCatalog.GetById("missing"));
        Assert.Contains("No doodad with id 'missing'.", missing.Message, StringComparison.Ordinal);
        Assert.Throws<ArgumentNullException>(() => DoodadCatalog.GetById(null!));
    }

    [Fact]
    public void CreateDeck_deals_catalog_order_unless_the_shuffle_says_otherwise()
    {
        Deck<Doodad> printed = DoodadCatalog.CreateDeck(new IdentityShuffle());

        Assert.Equal("cracked-oven-door", printed.Draw().Card.Id);
        Assert.Equal(Money.FromDollars(180), printed.Draw().Card.Cost);

        ScriptedShuffle shuffle = new([1, 0, 2, 3, 4, 5, 6, 7]);
        Deck<Doodad> cut = DoodadCatalog.CreateDeck(shuffle);

        Assert.Equal("harbor-parking-fine", cut.Draw().Card.Id);
        Assert.Equal(0, shuffle.Remaining);
    }

    [Fact]
    public void CreateDeck_rejects_a_null_shuffle()
    {
        Assert.Throws<ArgumentNullException>(() => DoodadCatalog.CreateDeck(null!));
    }

    [Fact]
    public void Parse_rejects_a_duplicate_a_zero_cost_an_empty_catalog_and_null_json()
    {
        string card = Sample("cracked-oven-door");
        string free = card.Replace("\"costCents\": 18000", "\"costCents\": 0", StringComparison.Ordinal);

        InvalidOperationException duplicate = Assert.Throws<InvalidOperationException>(() =>
            DoodadCatalog.Parse("[" + card + "," + card + "]"));
        CardValidationException cost = Assert.Throws<CardValidationException>(() => DoodadCatalog.Parse("[" + free + "]"));

        Assert.Contains("duplicated", duplicate.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Doodad cost must be greater than zero.", cost.Errors);
        Assert.Throws<JsonException>(() => DoodadCatalog.Parse("[]"));
        Assert.Throws<JsonException>(() => DoodadCatalog.Parse("[null]"));
        Assert.Throws<ArgumentNullException>(() => DoodadCatalog.Parse(null!));
    }

    private static string Sample(string id)
    {
        return $$"""
            {
              "id": "{{id}}",
              "title": "Cracked Oven Door",
              "summary": "The bakery oven door cracks on a Tuesday.",
              "costCents": 18000
            }
            """;
    }
}
