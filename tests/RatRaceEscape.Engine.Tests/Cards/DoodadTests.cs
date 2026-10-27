namespace RatRaceEscape.Engine.Tests;

public class DoodadTests
{
    [Fact]
    public void Create_trims_the_wording_and_keeps_the_cost()
    {
        Doodad doodad = Doodad.Create(
            "cracked-oven-door",
            "  Cracked Oven Door  ",
            "  The bakery oven door cracks on a Tuesday.  ",
            Money.FromDollars(180));

        Assert.Equal("cracked-oven-door", doodad.Id);
        Assert.Equal("Cracked Oven Door", doodad.Title);
        Assert.Equal("The bakery oven door cracks on a Tuesday.", doodad.Summary);
        Assert.Equal(Money.FromDollars(180), doodad.Cost);
    }

    [Fact]
    public void Create_reports_wording_and_a_missing_cost_together()
    {
        CardValidationException exception = Assert.Throws<CardValidationException>(() =>
            Doodad.Create("NOPE", " ", "too short", Money.Zero));

        Assert.Contains("Id must be a lowercase kebab-case slug.", exception.Errors);
        Assert.Contains("Title must be between 2 and 60 characters.", exception.Errors);
        Assert.Contains("Summary must be between 12 and 240 characters.", exception.Errors);
        Assert.Contains("Doodad cost must be greater than zero.", exception.Errors);
    }

    [Fact]
    public void Create_rejects_a_negative_cost_and_null_wording()
    {
        CardValidationException negative = Assert.Throws<CardValidationException>(() =>
            Doodad.Create("phone-screen", "Phone Screen", "A dropped phone needs a new screen.", Money.FromDollars(-1)));

        Assert.Contains("Doodad cost must be greater than zero.", negative.Errors);
        Assert.Throws<ArgumentNullException>(() => Doodad.Create(null!, "Title", "A long enough summary.", Money.FromDollars(1)));
        Assert.Throws<ArgumentNullException>(() => Doodad.Create("phone-screen", null!, "A long enough summary.", Money.FromDollars(1)));
        Assert.Throws<ArgumentNullException>(() => Doodad.Create("phone-screen", "Title", null!, Money.FromDollars(1)));
    }
}
