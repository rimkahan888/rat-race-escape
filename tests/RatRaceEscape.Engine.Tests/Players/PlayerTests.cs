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

    [Fact]
    public void Create_starts_on_the_opening_space()
    {
        Player player = Player.Create("Ava", ProfessionCatalog.GetById("night-baker"), TokenColour.Amber);

        Assert.Equal(0, player.BoardIndex);
        Assert.Equal(SpaceKind.Deal, RatRaceBoard.At(player.BoardIndex).Kind);
    }

    [Fact]
    public void WithBoardIndex_moves_the_token_and_keeps_the_rest()
    {
        Player player = Player.Create("Ava", ProfessionCatalog.GetById("night-baker"), TokenColour.Amber)
            .WithChildCount(2);

        Player moved = player.WithBoardIndex(6);

        Assert.Equal(6, moved.BoardIndex);
        Assert.Equal(2, moved.ChildCount);
        Assert.Equal(player.Cash, moved.Cash);
        Assert.Equal(player.Name, moved.Name);
        Assert.Same(player.Profession, moved.Profession);
        Assert.Equal(TokenColour.Amber, moved.TokenColour);
        Assert.Equal(0, player.BoardIndex);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(24)]
    public void WithBoardIndex_rejects_an_index_off_the_loop(int index)
    {
        Player player = Player.Create("Ava", ProfessionCatalog.GetById("night-baker"), TokenColour.Amber);

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            player.WithBoardIndex(index));

        Assert.Equal("boardIndex", exception.ParamName);
        Assert.Contains("Board index must be from 0 to 23.", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, player.BoardIndex);
    }

    [Fact]
    public void Child_count_and_balance_sheet_keep_the_board_index()
    {
        Player player = Player.Create("Ava", ProfessionCatalog.GetById("night-baker"), TokenColour.Amber)
            .WithBoardIndex(12);

        Player withChild = player.WithChildCount(1);
        Player withSheet = player.WithBalanceSheet(player.BalanceSheet);

        Assert.Equal(12, withChild.BoardIndex);
        Assert.Equal(1, withChild.ChildCount);
        Assert.Equal(12, withSheet.BoardIndex);
        Assert.Equal(0, withSheet.ChildCount);
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
