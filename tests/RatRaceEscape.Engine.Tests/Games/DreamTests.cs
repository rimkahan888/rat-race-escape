namespace RatRaceEscape.Engine.Tests;

public class DreamTests
{
    private static readonly Guid GameId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

    [Fact]
    public void Buying_the_dream_pays_the_price_and_wins_the_game()
    {
        Game landed = Ready().Roll(new ScriptedDieRoller([3]));
        Money cash = landed.CurrentPlayer.Cash;

        Assert.Equal(7, landed.CurrentPlayer.BoardIndex);
        Assert.Equal(FastTrackKind.Dream, FastTrackBoard.At(7).Kind);
        Assert.True(cash >= FastTrackBoard.DreamPrice);

        Game won = landed.Resolve(SpaceChoice.ClaimDream);

        Assert.Equal(GamePhase.Won, won.Phase);
        Assert.Equal(0, won.WinnerIndex);
        Assert.Equal(cash - FastTrackBoard.DreamPrice, won.CurrentPlayer.Cash);
        Assert.Equal(BoardTrack.FastTrack, won.CurrentPlayer.Track);
        Assert.Equal(TurnPhase.AwaitingEnd, won.TurnPhase);
        Assert.Equal(GamePhase.RatRace, landed.Phase);
        Assert.Null(landed.WinnerIndex);
        Assert.Equal(cash, landed.CurrentPlayer.Cash);
        TurnException ended = Assert.Throws<TurnException>(() => won.EndTurn());
        TurnException rolled = Assert.Throws<TurnException>(() => won.Roll(new ScriptedDieRoller([1])));
        Assert.Contains("The game is already over.", ended.Errors);
        Assert.Contains("The game is already over.", rolled.Errors);
    }

    [Fact]
    public void Declining_the_dream_keeps_the_cash_and_the_game()
    {
        Game landed = Ready().Roll(new ScriptedDieRoller([3]));

        Game declined = landed.Resolve(SpaceChoice.Decline);

        Assert.Equal(GamePhase.RatRace, declined.Phase);
        Assert.Null(declined.WinnerIndex);
        Assert.Equal(landed.CurrentPlayer.Cash, declined.CurrentPlayer.Cash);
        Assert.Equal(TurnPhase.AwaitingEnd, declined.TurnPhase);
        Assert.Equal(7, declined.CurrentPlayer.BoardIndex);
    }

    [Fact]
    public void A_dream_the_player_cannot_afford_stays_on_the_table()
    {
        Game landed = Bought().LeaveRatRace().EndTurn().Roll(new ScriptedDieRoller([3]));

        TurnException poor = Assert.Throws<TurnException>(() => landed.Resolve(SpaceChoice.ClaimDream));

        Assert.Contains("Cash cannot cover the dream.", poor.Errors);
        Assert.Equal(Money.FromDollars(899), landed.CurrentPlayer.Cash);
        Assert.Equal(GamePhase.RatRace, landed.Phase);
        Assert.Null(landed.WinnerIndex);
        Assert.Equal(TurnPhase.AwaitingResolve, landed.TurnPhase);
        Assert.Equal(Money.FromDollars(5_000), FastTrackBoard.DreamPrice);
    }

    [Fact]
    public void The_other_player_does_not_win_when_the_dream_is_bought()
    {
        Game bought = Bought(twoPlayers: true).LeaveRatRace();
        Game won = PlayToDream(bought).Resolve(SpaceChoice.ClaimDream);

        Assert.Equal(0, won.WinnerIndex);
        Assert.Equal("Ava", won.Players[won.WinnerIndex!.Value].Name);
        Assert.Equal(BoardTrack.RatRace, won.Players[1].Track);
        Assert.Equal(GamePhase.Won, won.Phase);
        TurnException bo = Assert.Throws<TurnException>(() => won.EndTurn());
        Assert.Contains("The game is already over.", bo.Errors);
    }

    private static Game Ready()
    {
        return Bought().LeaveRatRace().EndTurn().Roll(new ScriptedDieRoller([4])).Resolve().EndTurn();
    }

    private static Game PlayToDream(Game left)
    {
        Game ava = FinishRatRace(left.EndTurn());
        Game afterPayday = FinishRatRace(ava.Roll(new ScriptedDieRoller([4])).Resolve().EndTurn());
        return afterPayday.Roll(new ScriptedDieRoller([3]));
    }

    private static Game FinishRatRace(Game game)
    {
        Game rolled = game.Roll(new ScriptedDieRoller([1]));
        SpaceKind kind = RatRaceBoard.At(rolled.CurrentPlayer.BoardIndex).Kind;
        SpaceChoice choice = kind switch
        {
            SpaceKind.Charity => SpaceChoice.Decline,
            SpaceKind.Deal => SpaceChoice.SmallDeal,
            _ => SpaceChoice.None,
        };
        Game resolved = rolled.Resolve(choice);
        if (resolved.Drawn is not null)
        {
            resolved = resolved.Pass();
        }

        return resolved.EndTurn();
    }

    private static Game Bought(bool twoPlayers = false)
    {
        BigDeal deal = BigDeal.Business(
            "tide-pool",
            "Tide Pool",
            "A pool that pays far more than the bills.",
            Money.FromDollars(1),
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
