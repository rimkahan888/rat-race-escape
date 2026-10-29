namespace RatRaceEscape.Engine.Tests;

public class CloneTurnTests
{
    private static readonly Guid GameId = Guid.Parse("13131313-1313-1313-1313-131313131313");

    [Fact]
    public void The_clone_copies_the_profession_and_the_starting_sheet()
    {
        PlayerSetup human = new("Ava", "night-baker", TokenColour.Amber);
        PlayerSetup clone = CloneSeat.Beside(human);
        Game game = Game.Create(GameId, [human, clone]).SeatClone(1);

        Assert.Equal(1, game.CloneIndex);
        Assert.Equal(human.ProfessionId, clone.ProfessionId);
        Assert.Equal("Ava's clone", clone.Name);
        Assert.NotEqual(human.TokenColour, clone.TokenColour);
        Assert.Equal(game.Players[0].Profession.Id, game.Players[1].Profession.Id);
        Assert.Equal(game.Players[0].Cash, game.Players[1].Cash);
        Assert.Equal(game.Players[0].BalanceSheet.TotalLiabilities, game.Players[1].BalanceSheet.TotalLiabilities);
        Assert.Equal(game.Players[0].BalanceSheet.Mortgage, game.Players[1].BalanceSheet.Mortgage);
        Assert.False(CloneTurn.IsTheirTurn(game));
        TurnException waiting = Assert.Throws<TurnException>(() => CloneTurn.Step(game, new ScriptedDieRoller([1])));
        Assert.Contains("It is not the clone's turn.", waiting.Errors);
    }

    [Fact]
    public void The_clone_buys_income_it_can_afford_and_then_leaves()
    {
        SmallDeal cottage = SmallDeal.Property(
            "orchard-cottage",
            "Orchard Cottage",
            "A cottage that pays far more than the bills.",
            Money.FromDollars(100),
            Money.Zero,
            Money.FromDollars(10_000));
        CardPiles piles = CardPiles.Create(new IdentityShuffle())
            .WithSmallDeals(Deck<SmallDeal>.Create([cottage], new IdentityShuffle()));
        Game game = Game.Create(
            GameId,
            [new PlayerSetup("Ava", "night-baker", TokenColour.Amber)],
            piles).SeatClone(0);

        Game rolled = CloneTurn.Step(game, new ScriptedDieRoller([2]));
        Game drawn = CloneTurn.Step(rolled, new ScriptedDieRoller([1]));
        Game bought = CloneTurn.Step(drawn, new ScriptedDieRoller([1]));
        Game left = CloneTurn.Step(bought, new ScriptedDieRoller([1]));

        Assert.Equal(SpaceKind.Deal, RatRaceBoard.At(rolled.CurrentPlayer.BoardIndex).Kind);
        Assert.Equal("orchard-cottage", drawn.Drawn!.Id);
        Assert.Equal("Orchard Cottage", Assert.Single(bought.CurrentPlayer.BalanceSheet.RealEstate).Name);
        Assert.False(CashFlow.For(bought.CurrentPlayer).Monthly.IsNegative);
        Assert.Equal(BoardTrack.FastTrack, left.CurrentPlayer.Track);
        Assert.True(RatRaceExit.CanLeave(bought.CurrentPlayer) || left.CurrentPlayer.Track == BoardTrack.FastTrack);
    }

    [Fact]
    public void The_clone_passes_a_deal_that_would_make_cash_flow_negative()
    {
        BigDeal yard = BigDeal.Property(
            "lantern-yard",
            "Lantern Yard",
            "A courtyard whose mortgage outruns the rent.",
            Money.FromDollars(1_000),
            Money.FromDollars(1_000),
            Money.FromDollars(-80));
        CardPiles piles = CardPiles.Create(new IdentityShuffle())
            .WithBigDeals(Deck<BigDeal>.Create([yard], new IdentityShuffle()));
        Game tight = Game.Create(
            GameId,
            [new PlayerSetup("Ava", "night-baker", TokenColour.Amber)],
            piles).SeatClone(0).Borrow(6);
        Game drawn = tight.Roll(new ScriptedDieRoller([4])).Resolve(SpaceChoice.BigDeal);

        Assert.False(CloneTurn.KeepsCashFlow(tight.CurrentPlayer, Money.FromDollars(-80)));
        Game passed = CloneTurn.Step(drawn, new ScriptedDieRoller([1]));

        Assert.Equal("lantern-yard", drawn.Drawn!.Id);
        Assert.Null(passed.Drawn);
        Assert.Empty(passed.CurrentPlayer.BalanceSheet.RealEstate);
        Assert.False(CashFlow.For(passed.CurrentPlayer).Monthly.IsNegative);
        Assert.False(tight.CurrentPlayer.Cash.IsNegative);
    }

    [Fact]
    public void The_clone_repays_a_loan_when_cash_is_spare()
    {
        Game borrowed = Game.Create(
            GameId,
            [new PlayerSetup("Ava", "night-baker", TokenColour.Amber)]).SeatClone(0).Borrow(2);

        Game repaid = CloneTurn.Step(borrowed, new ScriptedDieRoller([1]));

        Assert.Equal(Money.FromDollars(1_000), Assert.Single(repaid.CurrentPlayer.BalanceSheet.OtherLoans).Balance);
        Assert.Equal(borrowed.CurrentPlayer.Cash - BankLoan.Increment, repaid.CurrentPlayer.Cash);
        Assert.False(CashFlow.For(repaid.CurrentPlayer).Monthly.IsNegative);
        Assert.Equal(TurnPhase.AwaitingRoll, repaid.TurnPhase);
    }

    [Fact]
    public void Seeded_clone_games_finish_without_a_negative_number()
    {
        for (int seed = 1; seed <= 6; seed++)
        {
            Game game = Game.Create(
                [new PlayerSetup("Ava", "night-baker", TokenColour.Amber)],
                seed).SeatClone(0);
            for (int step = 0; step < 8_000 && game.Phase == GamePhase.RatRace; step++)
            {
                AssertSolvent(game, seed);
                game = CloneTurn.Step(game, new ScriptedDieRoller([3]));
            }

            Assert.True(game.Phase is GamePhase.Won or GamePhase.Over, $"Seed {seed} ended in {game.Phase}.");
            AssertSolvent(game, seed);
        }
    }

    private static void AssertSolvent(Game game, int seed)
    {
        foreach (Player player in game.Players)
        {
            Assert.False(player.Cash.IsNegative, $"Seed {seed} cash {player.Cash}.");
            Assert.False(CashFlow.For(player).Monthly.IsNegative, $"Seed {seed} cash flow {CashFlow.For(player).Monthly}.");
        }
    }
}
