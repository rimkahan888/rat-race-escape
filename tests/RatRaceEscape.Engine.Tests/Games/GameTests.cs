namespace RatRaceEscape.Engine.Tests;

public class GameTests
{
    private static readonly Guid GameId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Create_seats_the_chosen_professions_with_the_first_player_on_turn()
    {
        Profession baker = ProfessionCatalog.GetById("night-baker");
        Profession archivist = ProfessionCatalog.GetById("municipal-archivist");

        Game game = Game.Create(
            GameId,
            [
                new PlayerSetup("  Ava Stone  ", baker.Id, TokenColour.Amber),
                new PlayerSetup("Bo", archivist.Id, TokenColour.Cobalt),
            ]);

        Assert.Equal(GameId, game.Id);
        Assert.Equal(0, game.CurrentTurnIndex);
        Assert.Equal(GamePhase.RatRace, game.Phase);
        Assert.Equal(2, game.Players.Count);
        Assert.Same(game.Players[0], game.CurrentPlayer);
        Assert.Equal("Ava Stone", game.Players[0].Name);
        Assert.Same(baker, game.Players[0].Profession);
        Assert.Equal(baker.StartingSavings, game.Players[0].Cash);
        Assert.Equal(90_000, game.Players[0].Cash.Cents);
        Assert.Equal(TokenColour.Amber, game.Players[0].TokenColour);
        Assert.Equal(0, game.Players[0].ChildCount);
        Assert.Equal("Bo", game.Players[1].Name);
        Assert.Same(archivist, game.Players[1].Profession);
        Assert.Equal(TokenColour.Cobalt, game.Players[1].TokenColour);
    }

    [Fact]
    public void Create_assigns_a_non_empty_id_when_none_is_given()
    {
        Game game = Game.Create([new PlayerSetup("Ava", "night-baker", TokenColour.Fern)]);

        Assert.NotEqual(Guid.Empty, game.Id);
        Assert.Equal(GamePhase.RatRace, game.Phase);
        Assert.Equal(0, game.CurrentTurnIndex);
    }

    [Fact]
    public void Create_allows_one_player_and_a_full_table()
    {
        Game solo = Game.Create(GameId, [new PlayerSetup("Ava", "night-baker", TokenColour.Amber)]);
        Assert.Single(solo.Players);

        PlayerSetup[] seats = new PlayerSetup[Game.MaximumPlayers];
        TokenColour[] colours =
        [
            TokenColour.Amber,
            TokenColour.Cobalt,
            TokenColour.Fern,
            TokenColour.Coral,
            TokenColour.Violet,
            TokenColour.Slate,
        ];
        for (int index = 0; index < seats.Length; index++)
        {
            seats[index] = new PlayerSetup($"Player {index + 1}", ProfessionCatalog.All[index].Id, colours[index]);
        }

        Game full = Game.Create(seats);

        Assert.Equal(6, full.Players.Count);
        Assert.Equal(ProfessionCatalog.All[5].Id, full.Players[5].Profession.Id);
        Assert.Equal(TokenColour.Amber, full.CurrentPlayer.TokenColour);
        Assert.Equal(TokenColour.Slate, full.Players[5].TokenColour);
    }

    [Fact]
    public void Create_copies_the_seat_list()
    {
        List<PlayerSetup> seats = [new PlayerSetup("Ava", "night-baker", TokenColour.Amber)];

        Game game = Game.Create(GameId, seats);
        seats.Clear();

        Assert.Single(game.Players);
    }

    [Fact]
    public void Create_allows_two_players_to_choose_the_same_profession()
    {
        Game game = Game.Create(
            GameId,
            [
                new PlayerSetup("Ava", "night-baker", TokenColour.Amber),
                new PlayerSetup("Bo", "night-baker", TokenColour.Cobalt),
            ]);

        Assert.Equal("night-baker", game.Players[0].Profession.Id);
        Assert.Equal("night-baker", game.Players[1].Profession.Id);
    }

    [Fact]
    public void Create_rejects_an_empty_id()
    {
        GameValidationException exception = Assert.Throws<GameValidationException>(() =>
            Game.Create(Guid.Empty, [new PlayerSetup("Ava", "night-baker", TokenColour.Amber)]));

        Assert.Contains("Game id cannot be empty.", exception.Errors);
    }

    [Fact]
    public void Create_rejects_a_null_seat_list()
    {
        Assert.Throws<ArgumentNullException>(() => Game.Create(null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void Create_rejects_a_table_outside_one_to_six_players(int count)
    {
        List<PlayerSetup> seats = new(count);
        for (int index = 0; index < count; index++)
        {
            seats.Add(new PlayerSetup($"Player {index + 1}", "night-baker", (TokenColour)(index + 1)));
        }

        GameValidationException exception = Assert.Throws<GameValidationException>(() => Game.Create(GameId, seats));

        if (count == 0)
        {
            Assert.Contains("A game needs at least one player.", exception.Errors);
        }
        else
        {
            Assert.Contains("A game cannot have more than 6 players.", exception.Errors);
        }
    }

    [Fact]
    public void Create_rejects_a_duplicated_name_ignoring_case()
    {
        GameValidationException exception = Assert.Throws<GameValidationException>(() =>
            Game.Create(
                GameId,
                [
                    new PlayerSetup("  Ava  ", "night-baker", TokenColour.Amber),
                    new PlayerSetup("ava", "municipal-archivist", TokenColour.Cobalt),
                ]));

        Assert.Contains("Player name 'ava' is used more than once.", exception.Errors);
    }

    [Fact]
    public void Create_rejects_a_duplicated_token_colour()
    {
        GameValidationException exception = Assert.Throws<GameValidationException>(() =>
            Game.Create(
                GameId,
                [
                    new PlayerSetup("Ava", "night-baker", TokenColour.Coral),
                    new PlayerSetup("Bo", "municipal-archivist", TokenColour.Coral),
                ]));

        Assert.Contains("Token colour Coral is used more than once.", exception.Errors);
    }

    [Fact]
    public void Create_rejects_an_unknown_or_blank_profession_id()
    {
        GameValidationException unknown = Assert.Throws<GameValidationException>(() =>
            Game.Create(GameId, [new PlayerSetup("Ava", "Night-Baker", TokenColour.Amber)]));
        Assert.Contains("No profession with id 'Night-Baker'.", unknown.Errors);

        GameValidationException blank = Assert.Throws<GameValidationException>(() =>
            Game.Create(GameId, [new PlayerSetup("Ava", "  ", TokenColour.Amber)]));
        Assert.Contains("Profession id is required.", blank.Errors);
    }

    [Fact]
    public void Create_rejects_a_null_name()
    {
        GameValidationException exception = Assert.Throws<GameValidationException>(() =>
            Game.Create(GameId, [new PlayerSetup(null!, "night-baker", TokenColour.Amber)]));

        Assert.Contains("Name is required.", exception.Errors);
    }

    [Fact]
    public void Create_reports_every_broken_rule_together()
    {
        GameValidationException exception = Assert.Throws<GameValidationException>(() =>
            Game.Create(
                Guid.Empty,
                [
                    new PlayerSetup("Ava", "night-baker", TokenColour.Amber),
                    new PlayerSetup("ava", "missing-job", TokenColour.Amber),
                    new PlayerSetup("A", null!, (TokenColour)0),
                ]));

        Assert.Contains("Game id cannot be empty.", exception.Errors);
        Assert.Contains("No profession with id 'missing-job'.", exception.Errors);
        Assert.Contains("Player name 'ava' is used more than once.", exception.Errors);
        Assert.Contains("Token colour Amber is used more than once.", exception.Errors);
        Assert.Contains("Profession id is required.", exception.Errors);
        Assert.Contains("Name must be between 2 and 40 characters.", exception.Errors);
        Assert.Contains("Token colour is not a playing colour.", exception.Errors);
        Assert.Equal(7, exception.Errors.Count);
    }
}
