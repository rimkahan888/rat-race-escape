namespace RatRaceEscape.Engine.Tests;

public class DreamTests
{
    private static readonly Guid GameId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

    [Fact]
    public void Buying_the_dream_pays_the_price_and_wins_the_game()
    {
        Game landed = Ready();
        Money cash = landed.CurrentPlayer.Cash;

        Assert.Equal(FastTrackKind.Dream, FastTrackBoard.At(landed.CurrentPlayer.BoardIndex).Kind);
        Money price = FastTrackBoard.PriceAt(landed.CurrentPlayer.BoardIndex);
        Assert.True(cash >= price);
        Assert.True(landed.LastRoll is not null);

        Game won = landed.Resolve(SpaceChoice.ClaimDream);

        Assert.Equal(GamePhase.Won, won.Phase);
        Assert.Equal(0, won.WinnerIndex);
        Assert.Equal(cash - price, won.CurrentPlayer.Cash);
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
        Game landed = Bought().LeaveRatRace().EndTurn().Roll(new ScriptedDieRoller([3]));

        Game declined = landed.Resolve(SpaceChoice.Decline);

        Assert.Equal(GamePhase.RatRace, declined.Phase);
        Assert.Null(declined.WinnerIndex);
        Assert.Equal(landed.CurrentPlayer.Cash, declined.CurrentPlayer.Cash);
        Assert.Equal(TurnPhase.AwaitingEnd, declined.TurnPhase);
        Assert.Equal(3, declined.CurrentPlayer.BoardIndex);
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
        Assert.Equal(Money.FromDollars(100_000), FastTrackBoard.PriceAt(3));
        Assert.Equal(Money.FromDollars(150_000), FastTrackBoard.PriceAt(7));
        Assert.Equal(Money.FromDollars(200_000), FastTrackBoard.PriceAt(11));
    }

    [Fact]
    public void The_other_player_does_not_win_when_the_dream_is_bought()
    {
        Game bought = Bought(twoPlayers: true).LeaveRatRace();
        Game won = PlayUntilAffordableDream(bought).Resolve(SpaceChoice.ClaimDream);

        Assert.Equal(0, won.WinnerIndex);
        Assert.Equal("Ava", won.Players[won.WinnerIndex!.Value].Name);
        Assert.Equal(BoardTrack.RatRace, won.Players[1].Track);
        Assert.Equal(GamePhase.Won, won.Phase);
        TurnException bo = Assert.Throws<TurnException>(() => won.EndTurn());
        Assert.Contains("The game is already over.", bo.Errors);
    }

    private static Game Ready()
    {
        return PlayUntilAffordableDream(Bought().LeaveRatRace());
    }

    private static Game PlayUntilAffordableDream(Game left)
    {
        Game game = left;
        int rolls = 0;
        int steps = 0;
        for (int guard = 0; guard < 200 && game.Phase == GamePhase.RatRace; guard++)
        {
            if (game.CurrentPlayer.Track != BoardTrack.FastTrack)
            {
                game = FinishRatRace(game);
                continue;
            }

            if (game.TurnPhase == TurnPhase.AwaitingRoll)
            {
                game = game.Roll(new ScriptedDieRoller([5]));
                rolls++;
                steps += 5;
                continue;
            }

            if (game.TurnPhase == TurnPhase.AwaitingResolve)
            {
                FastTrackKind kind = FastTrackBoard.At(game.CurrentPlayer.BoardIndex).Kind;
                if (kind == FastTrackKind.Dream
                    && game.CurrentPlayer.Cash >= FastTrackBoard.PriceAt(game.CurrentPlayer.BoardIndex))
                {
                    Assert.True(rolls >= 8, $"The dream arrived after {rolls} rolls.");
                    Assert.True(steps >= 36, $"The dream arrived after {steps} spaces.");
                    return game;
                }

                if (kind == FastTrackKind.Dream)
                {
                    game = game.Resolve(SpaceChoice.Decline);
                    continue;
                }

                try
                {
                    game = game.Resolve();
                }
                catch (TurnException)
                {
                    game = game.Settle();
                }

                continue;
            }

            game = game.EndTurn();
        }

        throw new InvalidOperationException("The dream was not reached.");
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
        Game resolved;
        try
        {
            resolved = rolled.Resolve(choice);
        }
        catch (TurnException)
        {
            resolved = rolled.Settle();
        }

        if (resolved.CurrentPlayer.IsBankrupt)
        {
            return resolved.EndTurn();
        }

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
