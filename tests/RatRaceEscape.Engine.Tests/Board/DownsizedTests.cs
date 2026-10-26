namespace RatRaceEscape.Engine.Tests;

public class DownsizedTests
{
    private static readonly Guid GameId = Guid.Parse("77777777-7777-7777-7777-777777777777");

    [Fact]
    public void Hours_cut_collects_total_expenses_and_skips_two_turns()
    {
        Player baker = Baker();
        Money due = IncomeStatement.For(baker).TotalExpenses;
        Assert.Equal(Money.FromCents(217_000), due);

        Player benched = Downsized.Apply(baker.WithCash(Money.FromCents(300_000)));

        Assert.Equal(Money.FromCents(83_000), benched.Cash);
        Assert.Equal(Downsized.SkippedTurns, benched.SkipTurnsRemaining);
        Assert.Equal(2, benched.SkipTurnsRemaining);
        Assert.Equal(baker.BoardIndex, benched.BoardIndex);
    }

    [Fact]
    public void A_child_increases_the_amount_due()
    {
        Player parent = Baker().WithChildCount(1).WithCash(Money.FromCents(245_000));

        Player benched = Downsized.Apply(parent);

        Assert.Equal(Money.FromCents(245_000), IncomeStatement.For(parent).TotalExpenses);
        Assert.Equal(Money.Zero, benched.Cash);
        Assert.Equal(2, benched.SkipTurnsRemaining);
        Assert.Equal(1, benched.ChildCount);
    }

    [Fact]
    public void Another_downsize_adds_two_more_skipped_turns()
    {
        Player baker = Baker().WithCash(Money.FromCents(500_000)).WithSkipTurnsRemaining(1);
        Money due = IncomeStatement.For(baker).TotalExpenses;

        Player benched = Downsized.Apply(baker);

        Assert.Equal(3, benched.SkipTurnsRemaining);
        Assert.Equal(baker.Cash - due, benched.Cash);
    }

    [Fact]
    public void Apply_fails_when_cash_cannot_cover_the_expenses()
    {
        Player baker = Baker();

        TurnException exception = Assert.Throws<TurnException>(() => Downsized.Apply(baker));

        Assert.Contains("Cash cannot cover the downsized expenses.", exception.Errors);
        Assert.Equal(Money.FromCents(90_000), baker.Cash);
        Assert.Equal(0, baker.SkipTurnsRemaining);
    }

    [Fact]
    public void Apply_rejects_a_null_player()
    {
        Assert.Throws<ArgumentNullException>(() => Downsized.Apply(null!));
    }

    [Fact]
    public void Landing_short_of_the_expenses_leaves_the_turn_open()
    {
        Game game = Table().Roll(new ScriptedDieRoller([6])).Resolve().EndTurn();
        game = game.Roll(new ScriptedDieRoller([6])).Resolve().EndTurn();
        Game landed = game.Roll(new ScriptedDieRoller([1]));

        Assert.Equal(SpaceKind.Downsized, RatRaceBoard.At(landed.CurrentPlayer.BoardIndex).Kind);
        Assert.Equal(Money.FromCents(216_000), landed.CurrentPlayer.Cash);

        TurnException exception = Assert.Throws<TurnException>(() => landed.Resolve());

        Assert.Contains("Cash cannot cover the downsized expenses.", exception.Errors);
        Assert.Equal(TurnPhase.AwaitingResolve, landed.TurnPhase);
        Assert.Equal(0, landed.CurrentPlayer.SkipTurnsRemaining);
        Assert.Equal(Money.FromCents(216_000), landed.CurrentPlayer.Cash);
    }

    [Fact]
    public void A_solo_player_pays_and_the_two_missed_turns_pass_at_once()
    {
        Game downsized = Reach().Resolve();

        Assert.Equal(13, downsized.CurrentPlayer.BoardIndex);
        Assert.Equal(Money.FromCents(188_000), downsized.CurrentPlayer.Cash);
        Assert.Equal(2, downsized.CurrentPlayer.SkipTurnsRemaining);
        Assert.Equal(TurnPhase.AwaitingEnd, downsized.TurnPhase);

        Game ready = downsized.EndTurn();

        Assert.Equal(0, ready.CurrentTurnIndex);
        Assert.Equal(0, ready.CurrentPlayer.SkipTurnsRemaining);
        Assert.Equal(TurnPhase.AwaitingRoll, ready.TurnPhase);
        Assert.Equal(14, ready.Roll(new ScriptedDieRoller([1])).CurrentPlayer.BoardIndex);
    }

    [Fact]
    public void The_other_player_takes_the_two_turns_that_were_skipped()
    {
        Game game = Game.Create(
            GameId,
            [
                new PlayerSetup("Ava", "night-baker", TokenColour.Amber),
                new PlayerSetup("Bo", "municipal-archivist", TokenColour.Cobalt),
            ]);

        for (int turn = 0; turn < 6; turn++)
        {
            game = Play(game, 6);
            game = Play(game, 1);
        }

        Game landed = game.Roll(new ScriptedDieRoller([1]));
        Assert.Equal("Ava", landed.CurrentPlayer.Name);
        Assert.Equal(SpaceKind.Downsized, RatRaceBoard.At(landed.CurrentPlayer.BoardIndex).Kind);

        Game downsized = landed.Resolve();
        Assert.Equal(Money.FromCents(188_000), downsized.Players[0].Cash);
        Assert.Equal(2, downsized.Players[0].SkipTurnsRemaining);
        Assert.Equal(0, downsized.Players[1].SkipTurnsRemaining);

        Game bo = downsized.EndTurn();
        Assert.Equal("Bo", bo.CurrentPlayer.Name);
        Assert.Equal(2, bo.Players[0].SkipTurnsRemaining);

        bo = Play(bo, 1);
        Assert.Equal("Bo", bo.CurrentPlayer.Name);
        Assert.Equal(1, bo.Players[0].SkipTurnsRemaining);

        bo = Play(bo, 1);
        Assert.Equal("Bo", bo.CurrentPlayer.Name);
        Assert.Equal(0, bo.Players[0].SkipTurnsRemaining);

        Game ava = Play(bo, 1);
        Assert.Equal("Ava", ava.CurrentPlayer.Name);
        Assert.Equal(0, ava.Players[0].SkipTurnsRemaining);
        Assert.Equal(TurnPhase.AwaitingRoll, ava.TurnPhase);
        Assert.Equal(Money.FromCents(188_000), ava.Players[0].Cash);
    }

    [Fact]
    public void Skipped_turns_stay_with_the_player()
    {
        Player benched = Baker().WithSkipTurnsRemaining(2);

        Assert.Equal(0, Baker().SkipTurnsRemaining);
        Assert.Equal(2, benched.WithBoardIndex(4).SkipTurnsRemaining);
        Assert.Equal(2, benched.WithChildCount(1).SkipTurnsRemaining);
        Assert.Equal(2, benched.WithCash(benched.Cash).SkipTurnsRemaining);
        Assert.Equal(2, benched.WithExtraDieTurns(1).SkipTurnsRemaining);

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            benched.WithSkipTurnsRemaining(-1));

        Assert.Equal("skipTurnsRemaining", exception.ParamName);
        Assert.Contains("Skipped turns cannot be negative.", exception.Message, StringComparison.Ordinal);
        Assert.Equal(2, benched.SkipTurnsRemaining);
    }

    private static Game Reach()
    {
        Game game = Table();
        for (int turn = 0; turn < 6; turn++)
        {
            game = game.Roll(new ScriptedDieRoller([6])).Resolve().EndTurn();
        }

        return game.Roll(new ScriptedDieRoller([1]));
    }

    private static Game Play(Game game, int face)
    {
        Game rolled = game.Roll(new ScriptedDieRoller([face]));
        Game resolved = RatRaceBoard.At(rolled.CurrentPlayer.BoardIndex).Kind == SpaceKind.Charity
            ? rolled.Resolve(SpaceChoice.Decline)
            : rolled.Resolve();
        return resolved.EndTurn();
    }

    private static Game Table()
    {
        return Game.Create(GameId, [new PlayerSetup("Ava", "night-baker", TokenColour.Amber)]);
    }

    private static Player Baker()
    {
        return Player.Create("Ava", ProfessionCatalog.GetById("night-baker"), TokenColour.Amber);
    }
}
