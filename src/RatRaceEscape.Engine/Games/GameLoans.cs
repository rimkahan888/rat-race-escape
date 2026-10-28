namespace RatRaceEscape.Engine;

public sealed partial class Game
{
    /// <summary>
    /// Borrows from the bank in increments of one thousand dollars. Each
    /// increment adds that cash, a matching principal on the bank loan, and
    /// one hundred dollars to the monthly payment. The current player can
    /// borrow on any step of their turn.
    /// </summary>
    public Game Borrow(int increments)
    {
        RequireAction();
        Player borrower = BankLoan.Borrow(CurrentPlayer, increments);
        return WithPlayer(borrower, TurnPhase, LastRoll, PendingDeck, Drawn, Piles);
    }

    /// <summary>
    /// Repays the bank loan in the same increments. The last increment removes
    /// the loan. Cash has to cover the principal.
    /// </summary>
    public Game Repay(int increments)
    {
        RequireAction();
        Player payer = BankLoan.Repay(CurrentPlayer, increments);
        return WithPlayer(payer, TurnPhase, LastRoll, PendingDeck, Drawn, Piles);
    }
}
