namespace RatRaceEscape.Engine.Tests;

public class FastTrackTests
{
    private static readonly Guid GameId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    [Fact]
    public void Leaving_puts_the_token_on_the_fast_track_and_the_other_player_stays()
    {
        Game bought = Bought(twoPlayers: true);
        Player before = bought.CurrentPlayer;

        Game left = bought.LeaveRatRace();

        Assert.Equal(BoardTrack.FastTrack, left.CurrentPlayer.Track);
        Assert.Equal(0, left.CurrentPlayer.BoardIndex);
        Assert.Equal(before.Cash, left.CurrentPlayer.Cash);
        Assert.Equal(Money.FromDollars(10_000), IncomeStatement.For(left.CurrentPlayer).PassiveIncome);
        Assert.Equal(TurnPhase.AwaitingEnd, left.TurnPhase);
        Assert.Equal(BoardTrack.RatRace, left.Players[1].Track);
        Assert.Equal(0, left.Players[1].BoardIndex);
        Assert.Equal(BoardTrack.RatRace, bought.CurrentPlayer.Track);
        Assert.Equal(2, bought.CurrentPlayer.BoardIndex);
        TurnException again = Assert.Throws<TurnException>(() => left.LeaveRatRace());
        Assert.Contains("This player has already left the rat race.", again.Errors);
    }

    [Fact]
    public void A_roll_on_the_fast_track_pays_passive_income_and_not_salary()
    {
        Game rolled = Bought().LeaveRatRace().EndTurn().Roll(new ScriptedDieRoller([4]));

        Assert.Equal(BoardTrack.FastTrack, rolled.CurrentPlayer.Track);
        Assert.Equal(4, rolled.CurrentPlayer.BoardIndex);
        Assert.Equal(FastTrackKind.Payday, FastTrackBoard.At(rolled.CurrentPlayer.BoardIndex).Kind);
        Assert.Equal(4, rolled.LastRoll!.ToIndex);
        Assert.Equal(0, rolled.LastRoll.FromIndex);
        Assert.Equal(Money.FromDollars(10_000), rolled.LastRoll.PaydayCash);
        Assert.Equal(Money.FromDollars(899) + Money.FromDollars(10_000), rolled.CurrentPlayer.Cash);

        Game resolved = rolled.Resolve();

        Assert.Equal(TurnPhase.AwaitingEnd, resolved.TurnPhase);
        Assert.Equal(rolled.CurrentPlayer.Cash, resolved.CurrentPlayer.Cash);
        Assert.Equal("Distribution Desk", FastTrackBoard.At(resolved.CurrentPlayer.BoardIndex).Title);
    }

    [Fact]
    public void A_windfall_pays_once_and_a_dry_dock_collects_once()
    {
        Game bonus = Bought().LeaveRatRace().EndTurn().Roll(new ScriptedDieRoller([1])).Resolve();
        Game dock = bonus.EndTurn().Roll(new ScriptedDieRoller([1])).Resolve();

        Assert.Equal(FastTrackKind.Windfall, FastTrackBoard.At(bonus.CurrentPlayer.BoardIndex).Kind);
        Assert.Equal(Money.FromDollars(899) + FastTrackBoard.Windfall, bonus.CurrentPlayer.Cash);
        Assert.Equal(Money.Zero, bonus.LastRoll!.PaydayCash);
        Assert.Equal(1, bonus.CurrentPlayer.BoardIndex);
        Assert.Equal(FastTrackKind.Setback, FastTrackBoard.At(dock.CurrentPlayer.BoardIndex).Kind);
        Assert.Equal(bonus.CurrentPlayer.Cash - FastTrackBoard.Setback, dock.CurrentPlayer.Cash);
        Assert.Equal(Money.Zero, dock.LastRoll!.PaydayCash);
    }

    [Fact]
    public void A_dream_listing_asks_for_a_claim_or_a_decline()
    {
        Game landed = Bought().LeaveRatRace().EndTurn().Roll(new ScriptedDieRoller([3]));

        Assert.Equal(FastTrackKind.Dream, FastTrackBoard.At(landed.CurrentPlayer.BoardIndex).Kind);
        TurnException missing = Assert.Throws<TurnException>(() => landed.Resolve());

        Assert.Contains("A dream listing needs a claim or a decline.", missing.Errors);
        Assert.Equal(TurnPhase.AwaitingResolve, landed.TurnPhase);
        Assert.Equal(Money.FromDollars(899), landed.CurrentPlayer.Cash);
    }

    [Fact]
    public void A_dry_dock_the_player_cannot_cover_can_be_settled_from_the_business()
    {
        Game landed = Bought(Money.FromDollars(700)).LeaveRatRace().EndTurn().Roll(new ScriptedDieRoller([2]));

        Assert.Equal(Money.FromDollars(200), landed.CurrentPlayer.Cash);
        Assert.Equal(FastTrackKind.Setback, FastTrackBoard.At(landed.CurrentPlayer.BoardIndex).Kind);
        TurnException unpaid = Assert.Throws<TurnException>(() => landed.Resolve());
        Game settled = landed.Settle();

        Assert.Contains("Cash cannot cover the dry dock.", unpaid.Errors);
        Assert.False(settled.CurrentPlayer.IsBankrupt);
        Assert.Empty(settled.CurrentPlayer.BalanceSheet.Businesses);
        Assert.Equal(Money.FromDollars(200) + Money.FromDollars(700) - FastTrackBoard.Setback, settled.CurrentPlayer.Cash);
        Assert.Equal(TurnPhase.AwaitingEnd, settled.TurnPhase);
        Assert.Equal(Money.FromDollars(10_000), IncomeStatement.For(landed.CurrentPlayer).PassiveIncome);
        Assert.Equal(Money.Zero, IncomeStatement.For(settled.CurrentPlayer).PassiveIncome);
    }

    [Fact]
    public void Leaving_before_the_income_or_mid_resolution_is_rejected()
    {
        Game waiting = Game.Create(GameId, [new PlayerSetup("Ava", "night-baker", TokenColour.Amber)]);
        Game rolled = waiting.Roll(new ScriptedDieRoller([2]));

        TurnException early = Assert.Throws<TurnException>(() => waiting.LeaveRatRace());
        TurnException open = Assert.Throws<TurnException>(() => rolled.LeaveRatRace());

        Assert.Contains("Passive income does not yet cover the bills.", early.Errors);
        Assert.Contains("Resolve the space before leaving the rat race.", open.Errors);
        Assert.Equal(BoardTrack.RatRace, waiting.CurrentPlayer.Track);
        Assert.Equal(TurnPhase.AwaitingResolve, rolled.TurnPhase);
    }

    private static Game Bought(bool twoPlayers = false)
    {
        return Bought(Money.FromDollars(1), twoPlayers);
    }

    private static Game Bought(Money downPayment, bool twoPlayers = false)
    {
        BigDeal deal = BigDeal.Business(
            "tide-pool",
            "Tide Pool",
            "A pool that pays far more than the bills.",
            downPayment,
            Money.Zero,
            Money.FromDollars(10_000));
        CardPiles piles = CardPiles.Create(new IdentityShuffle())
            .WithBigDeals(Deck<BigDeal>.Create([deal], new IdentityShuffle()));
        List<PlayerSetup> seats = [new PlayerSetup("Ava", "night-baker", TokenColour.Amber)];
        if (twoPlayers)
        {
            seats.Add(new PlayerSetup("Bo", "municipal-archivist", TokenColour.Cobalt));
        }

        return Game.Create(GameId, seats, piles)
            .Roll(new ScriptedDieRoller([2]))
            .Resolve(SpaceChoice.BigDeal)
            .Buy(shares: null);
    }
}
