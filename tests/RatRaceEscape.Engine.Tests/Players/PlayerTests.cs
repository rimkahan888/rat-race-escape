namespace RatRaceEscape.Engine.Tests;

public class PlayerTests
{
    [Fact]
    public void Create_stores_the_name_profession_cash_and_colour()
    {
        Profession baker = ProfessionCatalog.GetById("night-baker");

        Player player = Player.Create("  Ava Stone  ", baker, TokenColour.Coral);

        Assert.Equal("Ava Stone", player.Name);
        Assert.Same(baker, player.Profession);
        Assert.Equal("night-baker", player.Profession.Id);
        Assert.Equal(baker.StartingSavings, player.Cash);
        Assert.Equal(90_000, player.Cash.Cents);
        Assert.Equal(TokenColour.Coral, player.TokenColour);
    }

    [Fact]
    public void Create_uses_zero_when_the_profession_starts_with_no_savings()
    {
        Profession profession = WithSavings(Money.Zero);

        Player player = Player.Create("Bo", profession, TokenColour.Amber);

        Assert.Equal(Money.Zero, player.Cash);
        Assert.True(player.Cash.IsZero);
    }

    [Theory]
    [InlineData(TokenColour.Amber)]
    [InlineData(TokenColour.Cobalt)]
    [InlineData(TokenColour.Fern)]
    [InlineData(TokenColour.Coral)]
    [InlineData(TokenColour.Violet)]
    [InlineData(TokenColour.Slate)]
    public void Create_accepts_each_playing_colour(TokenColour colour)
    {
        Player player = Player.Create("Ava", ProfessionCatalog.GetById("night-baker"), colour);

        Assert.Equal(colour, player.TokenColour);
    }

    [Fact]
    public void Create_accepts_a_name_at_the_length_bounds()
    {
        Profession baker = ProfessionCatalog.GetById("night-baker");

        Assert.Equal("Bo", Player.Create("Bo", baker, TokenColour.Fern).Name);
        Assert.Equal(new string('A', 40), Player.Create(new string('A', 40), baker, TokenColour.Fern).Name);
    }

    [Fact]
    public void Create_rejects_a_null_name()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Player.Create(null!, ProfessionCatalog.GetById("night-baker"), TokenColour.Amber));
    }

    [Fact]
    public void Create_rejects_a_null_profession()
    {
        Assert.Throws<ArgumentNullException>(() => Player.Create("Ava", null!, TokenColour.Amber));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("A")]
    [InlineData("\t")]
    public void Create_rejects_a_name_outside_the_allowed_length(string name)
    {
        PlayerValidationException exception = Assert.Throws<PlayerValidationException>(() =>
            Player.Create(name, ProfessionCatalog.GetById("night-baker"), TokenColour.Amber));

        Assert.Contains("Name must be between 2 and 40 characters.", exception.Errors);
    }

    [Fact]
    public void Create_rejects_a_name_longer_than_40_characters()
    {
        PlayerValidationException exception = Assert.Throws<PlayerValidationException>(() =>
            Player.Create(new string('A', 41), ProfessionCatalog.GetById("night-baker"), TokenColour.Amber));

        Assert.Contains("Name must be between 2 and 40 characters.", exception.Errors);
    }

    [Fact]
    public void Create_rejects_a_name_with_a_control_character()
    {
        PlayerValidationException exception = Assert.Throws<PlayerValidationException>(() =>
            Player.Create("Bo\u0001", ProfessionCatalog.GetById("night-baker"), TokenColour.Amber));

        Assert.Contains("Name cannot contain control characters.", exception.Errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(-1)]
    public void Create_rejects_an_undefined_token_colour(int colour)
    {
        PlayerValidationException exception = Assert.Throws<PlayerValidationException>(() =>
            Player.Create("Ava", ProfessionCatalog.GetById("night-baker"), (TokenColour)colour));

        Assert.Contains("Token colour is not a playing colour.", exception.Errors);
    }

    [Fact]
    public void Create_reports_every_broken_rule_together()
    {
        PlayerValidationException exception = Assert.Throws<PlayerValidationException>(() =>
            Player.Create("\u0001", ProfessionCatalog.GetById("night-baker"), (TokenColour)0));

        Assert.Contains("Name must be between 2 and 40 characters.", exception.Errors);
        Assert.Contains("Name cannot contain control characters.", exception.Errors);
        Assert.Contains("Token colour is not a playing colour.", exception.Errors);
        Assert.Equal(3, exception.Errors.Count);
    }

    private static Profession WithSavings(Money savings)
    {
        Profession baker = ProfessionCatalog.GetById("night-baker");
        return Profession.Create(
            baker.Id,
            baker.Title,
            baker.Summary,
            baker.MonthlySalary,
            baker.MonthlyTaxes,
            baker.Expenses,
            savings,
            baker.Liabilities);
    }
}
