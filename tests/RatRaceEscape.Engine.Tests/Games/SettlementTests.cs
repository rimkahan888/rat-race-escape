namespace RatRaceEscape.Engine.Tests;

public class SettlementTests
{
    private static readonly Guid GameId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    [Fact]
    public void A_bill_that_cash_cannot_cover_bankrupts_a_player_with_no_assets()
    {
        Game landed = DoodadTable(Money.FromDollars(8_000)).Roll(new ScriptedDieRoller([1]));

        TurnException unpaid = Assert.Throws<TurnException>(() => landed.Resolve());
        Game settled = landed.Settle();
        Game over = settled.EndTurn();

        Assert.Contains("Cash cannot cover the doodad.", unpaid.Errors);
        Assert.Equal(TurnPhase.AwaitingResolve, landed.TurnPhase);
        Assert.Equal("gold-crown", settled.Drawn!.Id);
        Assert.True(settled.CurrentPlayer.IsBankrupt);
        Assert.Equal(Money.Zero, settled.CurrentPlayer.Cash);
        Assert.Equal(landed.CurrentPlayer.BalanceSheet.Mortgage, settled.CurrentPlayer.BalanceSheet.Mortgage);
        Assert.Equal(TurnPhase.AwaitingEnd, settled.TurnPhase);
        Assert.Equal(0, settled.CurrentPlayer.SkipTurnsRemaining);
        Assert.Equal(GamePhase.Over, over.Phase);
        Assert.Equal(TurnPhase.AwaitingRoll, over.TurnPhase);
        Assert.Null(over.Drawn);
        Assert.Equal(1, over.Piles.Doodads.DiscardCount);
        TurnException done = Assert.Throws<TurnException>(() => over.Roll(new ScriptedDieRoller([1])));
        Assert.Contains("The game is already over.", done.Errors);
        TurnException again = Assert.Throws<TurnException>(() => over.EndTurn());
        Assert.Contains("The game is already over.", again.Errors);
    }

    [Fact]
    public void Selling_shares_at_cost_pays_the_bill_and_leaves_the_player_in()
    {
        Game landed = DoodadTable(Money.FromDollars(1_400))
            .Roll(new ScriptedDieRoller([2]))
            .Resolve(SpaceChoice.SmallDeal)
            .Buy(20)
            .EndTurn()
            .Roll(new ScriptedDieRoller([6]))
            .Resolve()
            .EndTurn()
            .Roll(new ScriptedDieRoller([2]));

        Assert.Equal(SpaceKind.Doodad, RatRaceBoard.At(landed.CurrentPlayer.BoardIndex).Kind);
        Assert.Equal(10, landed.CurrentPlayer.BoardIndex);
        Money cash = landed.CurrentPlayer.Cash;

        Game settled = landed.Settle();

        Assert.False(settled.CurrentPlayer.IsBankrupt);
        Assert.Equal(cash + Money.FromDollars(70) - Money.FromDollars(1_400), settled.CurrentPlayer.Cash);
        Assert.Equal(13, Assert.Single(settled.CurrentPlayer.BalanceSheet.Stocks).Shares);
        Assert.Equal("LUMEN", settled.CurrentPlayer.BalanceSheet.Stocks[0].Symbol);
        Assert.Equal("gold-crown", settled.Drawn!.Id);
        Assert.Equal(TurnPhase.AwaitingEnd, settled.TurnPhase);
        Assert.Equal(0, settled.CurrentPlayer.SkipTurnsRemaining);
        Assert.Equal(1, settled.CurrentPlayer.ChildCount);
    }

    [Fact]
    public void A_bankrupt_player_is_skipped_and_cannot_borrow()
    {
        Game settled = Two().Roll(new ScriptedDieRoller([1])).Settle();

        TurnException borrow = Assert.Throws<TurnException>(() => settled.Borrow(1));
        Game bo = settled.EndTurn();
        Game back = bo.Roll(new ScriptedDieRoller([6])).Resolve().EndTurn();

        Assert.Contains("A bankrupt player cannot take that action.", borrow.Errors);
        Assert.Equal("Bo", bo.CurrentPlayer.Name);
        Assert.True(bo.Players[0].IsBankrupt);
        Assert.False(bo.Players[1].IsBankrupt);
        Assert.Equal("Bo", back.CurrentPlayer.Name);
        Assert.True(back.Players[0].IsBankrupt);
        Assert.Equal(GamePhase.RatRace, back.Phase);
        Assert.Equal(Money.FromDollars(1_950), back.Players[1].Cash);
    }

    [Fact]
    public void Hours_cut_that_a_sale_cannot_cover_bankrupts_without_skipping_turns()
    {
        Game landed = Game.Create(GameId, [new PlayerSetup("Ava", "night-baker", TokenColour.Amber)])
            .Roll(new ScriptedDieRoller([6]))
            .Resolve()
            .EndTurn()
            .Roll(new ScriptedDieRoller([6]))
            .Resolve()
            .EndTurn()
            .Roll(new ScriptedDieRoller([1]));

        TurnException unpaid = Assert.Throws<TurnException>(() => landed.Resolve());
        Game settled = landed.Settle();

        Assert.Contains("Cash cannot cover the downsized expenses.", unpaid.Errors);
        Assert.True(settled.CurrentPlayer.IsBankrupt);
        Assert.Equal(0, settled.CurrentPlayer.SkipTurnsRemaining);
        Assert.Equal(Money.Zero, settled.CurrentPlayer.Cash);
        Assert.Equal(TurnPhase.AwaitingEnd, settled.TurnPhase);
    }

    [Fact]
    public void Settling_is_rejected_when_cash_covers_the_bill_or_the_space_has_none()
    {
        Game charity = Game.Create(GameId, [new PlayerSetup("Ava", "night-baker", TokenColour.Amber)])
            .Roll(new ScriptedDieRoller([3]));
        Game waiting = Game.Create(GameId, [new PlayerSetup("Ava", "night-baker", TokenColour.Amber)]);
        Game covered = DoodadTable(Money.FromDollars(50)).Roll(new ScriptedDieRoller([1]));

        TurnException wrong = Assert.Throws<TurnException>(() => charity.Settle());
        TurnException early = Assert.Throws<TurnException>(() => waiting.Settle());
        TurnException enough = Assert.Throws<TurnException>(() => covered.Settle());

        Assert.Contains("This space has no expense to settle.", wrong.Errors);
        Assert.Contains("Roll the dice before settling.", early.Errors);
        Assert.Contains("Cash covers this expense.", enough.Errors);
        Assert.Equal(TurnPhase.AwaitingResolve, charity.TurnPhase);
        Assert.Null(covered.Drawn);
        Assert.Equal(1, covered.Piles.Doodads.DrawCount);
        Assert.Equal(Money.FromDollars(900), covered.CurrentPlayer.Cash);
    }

    [Fact]
    public void A_bill_bigger_than_cash_is_paid_with_a_loan_that_keeps_cash_flow()
    {
        Game landed = DoodadTable(Money.FromDollars(1_000)).Roll(new ScriptedDieRoller([1]));
        Money before = CashFlow.For(landed.CurrentPlayer).Monthly;

        Game settled = landed.Settle();

        Assert.False(settled.CurrentPlayer.IsBankrupt);
        Assert.Equal(Money.FromDollars(900), settled.CurrentPlayer.Cash);
        LoanLiability loan = Assert.Single(settled.CurrentPlayer.BalanceSheet.OtherLoans);
        Assert.Equal(Money.FromDollars(1_000), loan.Balance);
        Assert.Equal(Money.FromDollars(100), loan.MonthlyPayment);
        Assert.Equal(before - Money.FromDollars(100), CashFlow.For(settled.CurrentPlayer).Monthly);
        Assert.False(CashFlow.For(settled.CurrentPlayer).Monthly.IsNegative);
        Assert.Equal(TurnPhase.AwaitingEnd, settled.TurnPhase);
        Assert.Equal(GamePhase.RatRace, settled.EndTurn().Phase);
    }

    private static Game DoodadTable(Money cost)
    {
        Doodad crown = Doodad.Create(
            "gold-crown",
            "Gold Crown",
            "A crown the dentist did not plan for.",
            cost);
        CardPiles piles = CardPiles.Create(new IdentityShuffle())
            .WithDoodads(Deck<Doodad>.Create([crown], new IdentityShuffle()));
        return Game.Create(GameId, [new PlayerSetup("Ava", "night-baker", TokenColour.Amber)], piles);
    }

    private static Game Two()
    {
        Doodad crown = Doodad.Create(
            "gold-crown",
            "Gold Crown",
            "A crown the dentist did not plan for.",
            Money.FromDollars(8_000));
        CardPiles piles = CardPiles.Create(new IdentityShuffle())
            .WithDoodads(Deck<Doodad>.Create([crown], new IdentityShuffle()));
        return Game.Create(
            GameId,
            [
                new PlayerSetup("Ava", "night-baker", TokenColour.Amber),
                new PlayerSetup("Bo", "municipal-archivist", TokenColour.Cobalt),
            ],
            piles);
    }

}
