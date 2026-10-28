namespace RatRaceEscape.Engine.Tests;

public class LoanTests
{
    private static readonly Guid GameId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public void The_current_player_can_borrow_before_rolling_and_repay_before_the_turn_ends()
    {
        Game table = Table();
        Money boCash = table.Players[1].Cash;
        Money expenses = IncomeStatement.For(table.CurrentPlayer).TotalExpenses;

        Game borrowed = table.Borrow(2);
        Game rolled = borrowed.Roll(new ScriptedDieRoller([1]));
        Game repaid = rolled.Repay(1);

        LoanLiability open = Assert.Single(borrowed.CurrentPlayer.BalanceSheet.OtherLoans);
        Assert.Equal(Money.FromDollars(2_000), open.Balance);
        Assert.Equal(Money.FromDollars(200), open.MonthlyPayment);
        Assert.Equal(Money.FromDollars(2_900), borrowed.CurrentPlayer.Cash);
        Assert.Equal(expenses + Money.FromDollars(200), IncomeStatement.For(borrowed.CurrentPlayer).TotalExpenses);
        Assert.Equal(TurnPhase.AwaitingRoll, borrowed.TurnPhase);
        Assert.Empty(table.CurrentPlayer.BalanceSheet.OtherLoans);
        Assert.Equal(boCash, borrowed.Players[1].Cash);

        LoanLiability left = Assert.Single(repaid.CurrentPlayer.BalanceSheet.OtherLoans);
        Assert.Equal(Money.FromDollars(1_000), left.Balance);
        Assert.Equal(Money.FromDollars(100), left.MonthlyPayment);
        Assert.Equal(TurnPhase.AwaitingResolve, repaid.TurnPhase);
        Assert.Equal(1, repaid.CurrentPlayer.BoardIndex);
        Assert.Empty(repaid.Players[1].BalanceSheet.OtherLoans);
    }

    [Fact]
    public void Borrowing_during_a_resolved_turn_still_stacks_and_the_last_payment_clears_it()
    {
        Game ready = Table().Roll(new ScriptedDieRoller([3])).Resolve(SpaceChoice.Decline);

        Game borrowed = ready.Borrow(1).Borrow(1);
        Game cleared = borrowed.Repay(2);

        Assert.Equal(TurnPhase.AwaitingEnd, borrowed.TurnPhase);
        Assert.Equal(Money.FromDollars(2_000), Assert.Single(borrowed.CurrentPlayer.BalanceSheet.OtherLoans).Balance);
        Assert.Equal(Money.FromDollars(2_900), borrowed.CurrentPlayer.Cash);
        Assert.Empty(cleared.CurrentPlayer.BalanceSheet.OtherLoans);
        Assert.Equal(Money.FromDollars(900), cleared.CurrentPlayer.Cash);
        Assert.Equal(IncomeStatement.For(ready.CurrentPlayer).TotalExpenses, IncomeStatement.For(cleared.CurrentPlayer).TotalExpenses);
    }

    [Fact]
    public void A_repayment_the_player_cannot_cover_or_does_not_owe_is_rejected()
    {
        Game table = Table();
        Game borrowed = table.Borrow(1);

        TurnException missing = Assert.Throws<TurnException>(() => table.Repay(1));
        TurnException over = Assert.Throws<TurnException>(() => borrowed.Repay(2));
        TurnException none = Assert.Throws<TurnException>(() => table.Borrow(0));

        Assert.Contains("There is no bank loan to repay.", missing.Errors);
        Assert.Contains("The repayment is larger than the bank loan.", over.Errors);
        Assert.Contains("Loan increments must be greater than zero.", none.Errors);
        Assert.Equal(Money.FromDollars(1_000), Assert.Single(borrowed.CurrentPlayer.BalanceSheet.OtherLoans).Balance);
        Assert.Equal(TurnPhase.AwaitingRoll, table.TurnPhase);
    }

    private static Game Table()
    {
        return Game.Create(
            GameId,
            [
                new PlayerSetup("Ava", "night-baker", TokenColour.Amber),
                new PlayerSetup("Bo", "municipal-archivist", TokenColour.Cobalt),
            ]);
    }
}
