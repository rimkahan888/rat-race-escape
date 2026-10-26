namespace RatRaceEscape.Engine.Tests;

public class TurnTests
{
    private static readonly Guid GameId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public void A_new_game_waits_for_the_first_player_to_roll()
    {
        Game game = Solo();

        Assert.Equal(TurnPhase.AwaitingRoll, game.TurnPhase);
        Assert.Equal(0, game.CurrentTurnIndex);
        Assert.Equal(0, game.CurrentPlayer.BoardIndex);
        Assert.Null(game.LastRoll);
        Assert.Null(game.PendingDeck);
        Assert.Equal(GamePhase.RatRace, game.Phase);
    }

    [Fact]
    public void Roll_moves_the_current_player_and_waits_for_the_space()
    {
        Game game = TwoPlayers();
        Money cash = game.CurrentPlayer.Cash;

        Game rolled = game.Roll(new ScriptedDieRoller([4]));

        Assert.Equal(0, game.CurrentPlayer.BoardIndex);
        Assert.Equal(TurnPhase.AwaitingRoll, game.TurnPhase);
        Assert.Equal(TurnPhase.AwaitingResolve, rolled.TurnPhase);
        Assert.Equal(0, rolled.CurrentTurnIndex);
        Assert.Equal("Ava", rolled.CurrentPlayer.Name);
        Assert.Equal(4, rolled.CurrentPlayer.BoardIndex);
        Assert.Equal(0, rolled.Players[1].BoardIndex);
        Assert.Equal(cash, rolled.CurrentPlayer.Cash);
        Assert.NotNull(rolled.LastRoll);
        TurnRoll roll = rolled.LastRoll;
        Assert.Equal([4], roll.Faces);
        Assert.Equal(4, roll.Total);
        Assert.Equal(0, roll.FromIndex);
        Assert.Equal(4, roll.ToIndex);
        Assert.Equal(SpaceKind.Deal, RatRaceBoard.At(roll.ToIndex).Kind);
        Assert.Null(rolled.PendingDeck);
    }

    [Fact]
    public void Resolve_asks_for_the_deck_on_a_deal_market_or_doodad()
    {
        Game doodad = Solo().Roll(new ScriptedDieRoller([1])).Resolve();
        Game deal = Solo().Roll(new ScriptedDieRoller([2])).Resolve();
        Game market = Solo().Roll(new ScriptedDieRoller([5])).Resolve();

        Assert.Equal(DeckHook.Doodad, doodad.PendingDeck);
        Assert.Equal(1, doodad.CurrentPlayer.BoardIndex);
        Assert.Equal(DeckHook.Deal, deal.PendingDeck);
        Assert.Equal(DeckHook.Market, market.PendingDeck);
        Assert.Equal(TurnPhase.AwaitingEnd, market.TurnPhase);
    }

    [Fact]
    public void Resolve_acknowledges_a_square_that_has_no_deck()
    {
        Game start = Solo();
        Game charity = start.Roll(new ScriptedDieRoller([3])).Resolve(SpaceChoice.Decline);
        Game rolledPayday = start.Roll(new ScriptedDieRoller([6]));
        Game payday = rolledPayday.Resolve();

        Assert.Equal(SpaceKind.Charity, RatRaceBoard.At(charity.CurrentPlayer.BoardIndex).Kind);
        Assert.Null(charity.PendingDeck);
        Assert.Equal(start.Players[0].Cash, charity.Players[0].Cash);
        Assert.Equal(SpaceKind.Payday, RatRaceBoard.At(payday.CurrentPlayer.BoardIndex).Kind);
        Assert.Null(payday.PendingDeck);
        Assert.Equal(rolledPayday.CurrentPlayer.Cash, payday.CurrentPlayer.Cash);
    }

    [Fact]
    public void A_solo_player_can_walk_the_loop_one_turn_at_a_time()
    {
        Game game = Solo();
        Money cash = game.CurrentPlayer.Cash;
        Money payday = CashFlow.For(game.CurrentPlayer).Monthly;

        game = Finish(game, 1);
        Assert.Equal(1, game.CurrentPlayer.BoardIndex);
        Assert.Equal(0, game.CurrentTurnIndex);
        Assert.Equal(TurnPhase.AwaitingRoll, game.TurnPhase);
        Assert.Null(game.LastRoll);
        Assert.Null(game.PendingDeck);

        game = Finish(game, 4);
        Assert.Equal(SpaceKind.Market, RatRaceBoard.At(game.CurrentPlayer.BoardIndex).Kind);

        game = Finish(game, 1);
        Assert.Equal(SpaceKind.Payday, RatRaceBoard.At(game.CurrentPlayer.BoardIndex).Kind);

        game = Finish(game, 2);
        Assert.Equal(SpaceKind.Baby, RatRaceBoard.At(game.CurrentPlayer.BoardIndex).Kind);

        game = Finish(game, 5);
        Assert.Equal(SpaceKind.Downsized, RatRaceBoard.At(game.CurrentPlayer.BoardIndex).Kind);

        Game landed = game.Roll(new ScriptedDieRoller([1]));
        Game resolved = landed.Resolve();
        Assert.Equal(DeckHook.Deal, resolved.PendingDeck);
        game = resolved.EndTurn();

        Assert.Equal(14, game.CurrentPlayer.BoardIndex);
        Assert.Equal(cash + (payday * 2), game.CurrentPlayer.Cash);
        Assert.Equal(0, game.Players[0].ChildCount);
    }

    [Fact]
    public void End_turn_gives_the_dice_to_the_next_player_and_comes_back()
    {
        Game game = TwoPlayers();

        game = Finish(game, 4);
        Assert.Equal(1, game.CurrentTurnIndex);
        Assert.Equal("Bo", game.CurrentPlayer.Name);
        Assert.Equal(0, game.CurrentPlayer.BoardIndex);
        Assert.Equal(4, game.Players[0].BoardIndex);

        game = Finish(game, 1);
        Assert.Equal(0, game.CurrentTurnIndex);
        Assert.Equal("Ava", game.CurrentPlayer.Name);
        Assert.Equal(1, game.Players[1].BoardIndex);

        game = Finish(game, 2);
        Assert.Equal(1, game.CurrentTurnIndex);
        Assert.Equal(6, game.Players[0].BoardIndex);
        Assert.Equal(SpaceKind.Payday, RatRaceBoard.At(game.Players[0].BoardIndex).Kind);
        Assert.Equal(1, game.Players[1].BoardIndex);
    }

    [Fact]
    public void Roll_does_not_spend_a_die_when_the_turn_is_not_ready()
    {
        ScriptedDieRoller dice = new([4, 2]);
        Game rolled = Solo().Roll(dice);

        TurnException exception = Assert.Throws<TurnException>(() => rolled.Roll(dice));

        Assert.Contains("Resolve the space before rolling again.", exception.Errors);
        Assert.Equal(1, dice.Remaining);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void Roll_rejects_a_die_count_outside_one_or_two(int dieCount)
    {
        ScriptedDieRoller dice = new([1, 1, 1]);

        TurnException exception = Assert.Throws<TurnException>(() => Solo().Roll(dice, dieCount));

        Assert.Contains("Roll one or two dice.", exception.Errors);
        Assert.Equal(3, dice.Remaining);
    }

    [Fact]
    public void Roll_rejects_a_null_roller()
    {
        Assert.Throws<ArgumentNullException>(() => Solo().Roll(null!));
    }

    [Fact]
    public void Actions_follow_roll_then_resolve_then_end()
    {
        Game waiting = Solo();
        TurnException resolveFirst = Assert.Throws<TurnException>(() => waiting.Resolve());
        TurnException endFirst = Assert.Throws<TurnException>(() => waiting.EndTurn());

        Game rolled = waiting.Roll(new ScriptedDieRoller([2, 2]));
        TurnException endEarly = Assert.Throws<TurnException>(() => rolled.EndTurn());
        Game resolved = rolled.Resolve();
        TurnException resolveAgain = Assert.Throws<TurnException>(() => resolved.Resolve());
        Game ended = resolved.EndTurn();
        TurnException resolveAfter = Assert.Throws<TurnException>(() => ended.Resolve());

        Assert.Contains("Roll the dice before resolving the space.", resolveFirst.Errors);
        Assert.Contains("Roll the dice before ending the turn.", endFirst.Errors);
        Assert.Contains("Resolve the space before ending the turn.", endEarly.Errors);
        Assert.Contains("The space is already resolved.", resolveAgain.Errors);
        Assert.Contains("Roll the dice before resolving the space.", resolveAfter.Errors);
        Assert.Equal(TurnPhase.AwaitingRoll, ended.TurnPhase);
        Assert.Equal(2, ended.CurrentPlayer.BoardIndex);
    }

    [Fact]
    public void End_turn_then_roll_again_rejects_a_second_roll_until_the_space_is_resolved()
    {
        Game again = Finish(Solo(), 2).Roll(new ScriptedDieRoller([6]));

        TurnException exception = Assert.Throws<TurnException>(() => again.EndTurn());

        Assert.Contains("Resolve the space before ending the turn.", exception.Errors);
        Game resolved = again.Resolve();
        TurnException rolledTwice = Assert.Throws<TurnException>(() => resolved.Roll(new ScriptedDieRoller([1])));
        Assert.Contains("End the turn before rolling again.", rolledTwice.Errors);
    }

    [Fact]
    public void Turn_exception_requires_an_error()
    {
        Assert.Throws<ArgumentNullException>(() => new TurnException((IReadOnlyList<string>)null!));
        Assert.Throws<ArgumentException>(() => new TurnException(new List<string>()));
    }

    private static Game Finish(Game game, int face)
    {
        return game.Roll(new ScriptedDieRoller([face])).Resolve().EndTurn();
    }

    private static Game Solo()
    {
        return Game.Create(GameId, [new PlayerSetup("Ava", "night-baker", TokenColour.Amber)]);
    }

    private static Game TwoPlayers()
    {
        return Game.Create(
            GameId,
            [
                new PlayerSetup("Ava", "night-baker", TokenColour.Amber),
                new PlayerSetup("Bo", "municipal-archivist", TokenColour.Cobalt),
            ]);
    }
}
