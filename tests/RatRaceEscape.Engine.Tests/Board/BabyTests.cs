namespace RatRaceEscape.Engine.Tests;

public class BabyTests
{
    private static readonly Guid GameId = Guid.Parse("66666666-6666-6666-6666-666666666666");

    [Fact]
    public void A_new_arrival_adds_one_child_and_the_per_child_expense()
    {
        Player baker = Baker();

        Player parent = Baby.Arrive(baker);
        IncomeStatement statement = IncomeStatement.For(parent);

        Assert.Equal(0, baker.ChildCount);
        Assert.Equal(1, parent.ChildCount);
        Assert.Equal(baker.Profession.Expenses.PerChild, statement.ChildExpenses);
        Assert.Equal(Money.FromCents(28_000), statement.ChildExpenses);
        Assert.Equal(Money.FromCents(35_000), CashFlow.For(parent).Monthly);
        Assert.Equal(baker.Cash, parent.Cash);
        Assert.Equal(baker.BoardIndex, parent.BoardIndex);
    }

    [Fact]
    public void The_household_stops_at_three_children()
    {
        Player family = Baker();
        for (int child = 0; child < Player.MaximumChildren; child++)
        {
            family = Baby.Arrive(family);
        }

        Player capped = Baby.Arrive(family);

        Assert.Equal(3, Player.MaximumChildren);
        Assert.Equal(3, family.ChildCount);
        Assert.Same(family, capped);
        Assert.Equal(Money.FromCents(-21_000), CashFlow.For(capped).Monthly);
    }

    [Fact]
    public void Arrive_rejects_a_null_player()
    {
        Assert.Throws<ArgumentNullException>(() => Baby.Arrive(null!));
    }

    [Fact]
    public void Landing_on_a_new_arrival_adds_the_child_when_the_space_is_resolved()
    {
        Game start = Table();
        Game landed = start.Roll(new ScriptedDieRoller([5])).Resolve().EndTurn().Roll(new ScriptedDieRoller([3]));

        Assert.Equal(SpaceKind.Baby, RatRaceBoard.At(landed.CurrentPlayer.BoardIndex).Kind);
        Assert.Equal(0, landed.CurrentPlayer.ChildCount);
        Assert.Equal(Money.FromCents(63_000), landed.LastRoll!.PaydayCash);
        Assert.Equal(Money.FromCents(153_000), landed.CurrentPlayer.Cash);

        Game resolved = landed.Resolve();

        Assert.Equal(1, resolved.CurrentPlayer.ChildCount);
        Assert.Equal(Money.FromCents(35_000), CashFlow.For(resolved.CurrentPlayer).Monthly);
        Assert.Equal(Money.FromCents(153_000), resolved.CurrentPlayer.Cash);
        Assert.Equal(0, start.CurrentPlayer.ChildCount);
        Assert.Null(resolved.PendingDeck);
        Assert.Equal(TurnPhase.AwaitingEnd, resolved.TurnPhase);
    }

    [Fact]
    public void A_new_arrival_does_not_add_a_child_for_the_other_players()
    {
        Game game = Game.Create(
            GameId,
            [
                new PlayerSetup("Ava", "night-baker", TokenColour.Amber),
                new PlayerSetup("Bo", "municipal-archivist", TokenColour.Cobalt),
            ]);

        Game resolved = game
            .Roll(new ScriptedDieRoller([5])).Resolve().EndTurn()
            .Roll(new ScriptedDieRoller([1])).Resolve().EndTurn()
            .Roll(new ScriptedDieRoller([3])).Resolve();

        Assert.Equal(1, resolved.Players[0].ChildCount);
        Assert.Equal(0, resolved.Players[1].ChildCount);
        Assert.Equal("Ava", resolved.CurrentPlayer.Name);
        Assert.Equal(Money.FromCents(140_000), resolved.Players[1].Cash);
    }

    [Fact]
    public void Further_arrivals_stop_after_the_third_child()
    {
        Game game = Table().Roll(new ScriptedDieRoller([6])).Resolve().EndTurn();
        game = game.Roll(new ScriptedDieRoller([2])).Resolve().EndTurn();
        Assert.Equal(1, game.CurrentPlayer.ChildCount);
        Assert.Equal(8, game.CurrentPlayer.BoardIndex);

        game = Lap(game);
        Assert.Equal(2, game.CurrentPlayer.ChildCount);

        game = Lap(game);
        Assert.Equal(3, game.CurrentPlayer.ChildCount);

        Game capped = Lap(game);

        Assert.Equal(3, capped.CurrentPlayer.ChildCount);
        Assert.Equal(8, capped.CurrentPlayer.BoardIndex);
        Assert.Equal(Money.FromCents(-21_000), CashFlow.For(capped.CurrentPlayer).Monthly);
        Assert.Equal(Money.FromCents(216_000), capped.CurrentPlayer.Cash);
    }

    [Fact]
    public void A_new_arrival_rejects_a_charity_choice()
    {
        Game landed = Table().Roll(new ScriptedDieRoller([6])).Resolve().EndTurn().Roll(new ScriptedDieRoller([2]));

        TurnException exception = Assert.Throws<TurnException>(() => landed.Resolve(SpaceChoice.Donate));

        Assert.Contains("This space does not take a choice.", exception.Errors);
        Assert.Equal(0, landed.CurrentPlayer.ChildCount);
    }

    private static Game Lap(Game game)
    {
        for (int turn = 0; turn < 4; turn++)
        {
            game = game.Roll(new ScriptedDieRoller([6])).Resolve().EndTurn();
        }

        return game;
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
