namespace RatRaceEscape.Engine.Tests;

public class InMemoryGameRepositoryTests
{
    [Fact]
    public void Add_then_Find_returns_the_same_game()
    {
        InMemoryGameRepository repository = new();
        Game game = NewGame("Ava");

        repository.Add(game);

        Assert.Same(game, repository.Find(game.Id));
    }

    [Fact]
    public void Find_returns_null_when_the_id_is_unknown()
    {
        InMemoryGameRepository repository = new();
        repository.Add(NewGame("Ava"));

        Assert.Null(repository.Find(Guid.NewGuid()));
        Assert.Null(repository.Find(Guid.Empty));
    }

    [Fact]
    public void Games_stay_separate()
    {
        InMemoryGameRepository repository = new();
        Game first = NewGame("Ava");
        Game second = NewGame("Bo");

        repository.Add(first);
        repository.Add(second);

        Assert.Same(first, repository.Find(first.Id));
        Assert.Same(second, repository.Find(second.Id));
        Assert.NotSame(repository.Find(first.Id), repository.Find(second.Id));
    }

    [Fact]
    public void Add_rejects_a_null_game()
    {
        InMemoryGameRepository repository = new();

        Assert.Throws<ArgumentNullException>(() => repository.Add(null!));
    }

    [Fact]
    public void Add_rejects_a_duplicate_id_and_keeps_the_original()
    {
        InMemoryGameRepository repository = new();
        Game game = NewGame("Ava");
        repository.Add(game);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => repository.Add(game));

        Assert.Contains(game.Id.ToString(), exception.Message, StringComparison.Ordinal);
        Assert.Same(game, repository.Find(game.Id));
    }

    [Fact]
    public void Add_keeps_every_game_when_they_arrive_together()
    {
        InMemoryGameRepository repository = new();
        Game[] games = new Game[32];
        for (int index = 0; index < games.Length; index++)
        {
            games[index] = NewGame($"Player {index + 1}");
        }

        Parallel.ForEach(games, repository.Add);

        foreach (Game game in games)
        {
            Assert.Same(game, repository.Find(game.Id));
        }
    }

    [Fact]
    public void Save_replaces_the_stored_game()
    {
        InMemoryGameRepository repository = new();
        Game game = NewGame("Ava");
        repository.Add(game);

        Game rolled = game.Roll(new ScriptedDieRoller([3]));
        repository.Save(rolled);

        Game? stored = repository.Find(game.Id);
        Assert.Same(rolled, stored);
        Assert.NotSame(game, stored);
        Assert.Equal(3, stored!.CurrentPlayer.BoardIndex);
        Assert.Equal(TurnPhase.AwaitingResolve, stored.TurnPhase);
    }

    [Fact]
    public void Save_rejects_an_unknown_id_and_a_null_game()
    {
        InMemoryGameRepository repository = new();
        Game game = NewGame("Ava");

        InvalidOperationException missing = Assert.Throws<InvalidOperationException>(() => repository.Save(game));

        Assert.Contains(game.Id.ToString(), missing.Message, StringComparison.Ordinal);
        Assert.Null(repository.Find(game.Id));
        Assert.Throws<ArgumentNullException>(() => repository.Save(null!));
    }

    [Fact]
    public void Save_leaves_the_other_games_in_place()
    {
        InMemoryGameRepository repository = new();
        Game first = NewGame("Ava");
        Game second = NewGame("Bo");
        repository.Add(first);
        repository.Add(second);

        Game rolled = first.Roll(new ScriptedDieRoller([2]));
        repository.Save(rolled);

        Assert.Same(rolled, repository.Find(first.Id));
        Assert.Same(second, repository.Find(second.Id));
    }

    private static Game NewGame(string name)
    {
        return Game.Create([new PlayerSetup(name, "night-baker", TokenColour.Amber)]);
    }
}
