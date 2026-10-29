namespace RatRaceEscape.Engine.Tests;

public class SeededGameTests
{
    private const int StepLimit = 8_000;

    private static readonly string[] Professions =
    [
        "night-baker",
        "municipal-archivist",
        "harbor-ferry-mate",
        "clinic-lab-tech",
        "grid-electrician",
        "trail-designer",
        "tunnel-inspector",
        "reef-surveyor",
    ];

    private static readonly string[] Names = ["Ava", "Bo", "Cam", "Dee"];

    private static readonly TokenColour[] Colours =
    [
        TokenColour.Amber,
        TokenColour.Cobalt,
        TokenColour.Fern,
        TokenColour.Coral,
    ];

    [Fact]
    public void Seeded_games_finish_with_no_negative_cash_or_cash_flow()
    {
        for (int seed = 1; seed <= 20; seed++)
        {
            int players = (seed % 3) + 1;
            Game finished = Play(seed, players);

            Assert.True(
                finished.Phase is GamePhase.Won or GamePhase.Over,
                $"Seed {seed} with {players} players ended in {finished.Phase}.");
            AssertSolvent(finished, seed);
        }
    }

    private static Game Play(int seed, int playerCount)
    {
        RandomDieRoller dice = new(seed);
        List<PlayerSetup> seats = new(playerCount);
        for (int seat = 0; seat < playerCount; seat++)
        {
            seats.Add(new PlayerSetup(
                Names[seat],
                Professions[(seed + seat) % Professions.Length],
                Colours[seat]));
        }

        Game game = Game.Create(Guid.NewGuid(), seats, new RandomShuffle(seed));
        for (int step = 0; step < StepLimit && game.Phase == GamePhase.RatRace; step++)
        {
            AssertSolvent(game, seed);
            try
            {
                game = Step(game, dice);
            }
            catch (TurnException exception)
            {
                Assert.Fail(
                    $"Seed {seed} stopped on {game.CurrentPlayer.Name} at {game.TurnPhase}: {exception.Message}");
            }
        }

        Assert.True(
            game.Phase is GamePhase.Won or GamePhase.Over,
            $"Seed {seed} was still {game.Phase} after {StepLimit} steps.");
        return game;
    }

    private static Game Step(Game game, RandomDieRoller dice)
    {
        if (game.CurrentPlayer.IsBankrupt)
        {
            return game.EndTurn();
        }

        if (game.TurnPhase == TurnPhase.AwaitingRoll)
        {
            if (RatRaceExit.CanLeave(game.CurrentPlayer))
            {
                return game.LeaveRatRace();
            }

            int diceCount = game.CurrentPlayer.ExtraDieTurnsRemaining > 0 ? 2 : 1;
            return game.Roll(dice, diceCount);
        }

        if (game.TurnPhase == TurnPhase.AwaitingResolve)
        {
            return Resolve(game);
        }

        Game acted = BuyOffer(game);
        if (acted.Phase != GamePhase.RatRace)
        {
            return acted;
        }

        if (acted.Drawn is not null)
        {
            acted = acted.Pass();
        }

        if (RatRaceExit.CanLeave(acted.CurrentPlayer))
        {
            acted = acted.LeaveRatRace();
        }

        return acted.TurnPhase == TurnPhase.AwaitingEnd ? acted.EndTurn() : acted;
    }

    private static Game Resolve(Game game)
    {
        if (game.CurrentPlayer.Track == BoardTrack.FastTrack)
        {
            return ResolveFastTrack(game);
        }

        SpaceKind kind = RatRaceBoard.At(game.CurrentPlayer.BoardIndex).Kind;
        if (kind == SpaceKind.Charity)
        {
            Money donation = Charity.Donation(IncomeStatement.For(game.CurrentPlayer).TotalIncome);
            SpaceChoice choice = game.CurrentPlayer.Cash >= donation + Money.FromDollars(200)
                ? SpaceChoice.Donate
                : SpaceChoice.Decline;
            return game.Resolve(choice);
        }

        if (kind == SpaceKind.Deal)
        {
            SpaceChoice choice = game.CurrentPlayer.Cash >= Money.FromDollars(3_000)
                ? SpaceChoice.BigDeal
                : SpaceChoice.SmallDeal;
            return game.Resolve(choice);
        }

        try
        {
            return game.Resolve();
        }
        catch (TurnException exception) when (IsForcedBill(exception))
        {
            return game.Settle();
        }
    }

    private static Game ResolveFastTrack(Game game)
    {
        FastTrackKind kind = FastTrackBoard.At(game.CurrentPlayer.BoardIndex).Kind;
        if (kind == FastTrackKind.Dream)
        {
            SpaceChoice choice = game.CurrentPlayer.Cash >= FastTrackBoard.DreamPrice
                ? SpaceChoice.ClaimDream
                : SpaceChoice.Decline;
            return game.Resolve(choice);
        }

        try
        {
            return game.Resolve();
        }
        catch (TurnException exception) when (IsForcedBill(exception))
        {
            return game.Settle();
        }
    }

    private static Game BuyOffer(Game game)
    {
        if (game.Drawn?.SmallDeal is SmallDeal stock && stock.Kind == SmallDealKind.Stock)
        {
            if (!stock.PricePerShare.IsPositive)
            {
                return game;
            }

            int shares = (int)(game.CurrentPlayer.Cash.Cents / stock.PricePerShare.Cents);
            if (shares < 1)
            {
                return game;
            }

            try
            {
                return game.Buy(shares);
            }
            catch (TurnException)
            {
                return game;
            }
        }

        if (game.Drawn?.SmallDeal is not null || game.Drawn?.BigDeal is not null)
        {
            try
            {
                return game.Buy(shares: null);
            }
            catch (TurnException)
            {
                return game;
            }
        }

        return game;
    }

    private static bool IsForcedBill(TurnException exception)
    {
        foreach (string error in exception.Errors)
        {
            if (error.Contains("doodad", StringComparison.Ordinal)
                || error.Contains("downsized", StringComparison.Ordinal)
                || error.Contains("dry dock", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static void AssertSolvent(Game game, int seed)
    {
        for (int index = 0; index < game.Players.Count; index++)
        {
            Player player = game.Players[index];
            Assert.False(
                player.Cash.IsNegative,
                $"Seed {seed} player {player.Name} cash is {player.Cash}.");
            Assert.False(
                CashFlow.For(player).Monthly.IsNegative,
                $"Seed {seed} player {player.Name} cash flow is {CashFlow.For(player).Monthly}.");
        }

        if (game.LastRoll is TurnRoll roll)
        {
            Assert.False(roll.PaydayCash.IsNegative, $"Seed {seed} payday is {roll.PaydayCash}.");
        }
    }
}
