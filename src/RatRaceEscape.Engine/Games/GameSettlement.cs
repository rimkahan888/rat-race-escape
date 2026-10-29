namespace RatRaceEscape.Engine;

public sealed partial class Game
{
    /// <summary>
    /// Covers an expense the current player cannot pay from cash. Assets are
    /// sold for the cash that was put into them. Stocks go first, then
    /// businesses, then property. If the bill is covered, it is paid and the
    /// space resolves. If it is still short, the player goes bankrupt, the
    /// unpaid remainder is forgiven, and the space resolves without the
    /// downsized penalty.
    /// </summary>
    public Game Settle()
    {
        RequireAction();

        if (TurnPhase != TurnPhase.AwaitingResolve || LastRoll is null)
        {
            throw new TurnException(
                TurnPhase == TurnPhase.AwaitingEnd
                    ? "The space is already resolved."
                    : "Roll the dice before settling.");
        }

        if (CurrentPlayer.Track == BoardTrack.FastTrack)
        {
            return SettleFastTrack();
        }

        BoardSpace space = RatRaceBoard.At(CurrentPlayer.BoardIndex);
        if (space.Kind == SpaceKind.Downsized)
        {
            return SettleDownsized();
        }

        if (space.Kind == SpaceKind.Doodad)
        {
            return SettleDoodad();
        }

        throw new TurnException("This space has no expense to settle.");
    }

    private Game SettleFastTrack()
    {
        FastTrackSpace space = FastTrackBoard.At(CurrentPlayer.BoardIndex);
        if (space.Kind != FastTrackKind.Setback)
        {
            throw new TurnException("This space has no expense to settle.");
        }

        if (CurrentPlayer.Cash >= FastTrackBoard.Setback)
        {
            throw new TurnException("Cash covers this expense.");
        }

        Player covered = Shortfall.Cover(CurrentPlayer, FastTrackBoard.Setback);
        return WithPlayer(covered, TurnPhase.AwaitingEnd, LastRoll, pendingDeck: null, drawn: null, Piles);
    }

    private Game SettleDownsized()
    {
        Money due = IncomeStatement.For(CurrentPlayer).TotalExpenses;
        if (CurrentPlayer.Cash >= due)
        {
            throw new TurnException("Cash covers this expense.");
        }

        Player raised = ForcedSale.Raise(CurrentPlayer, due);
        if (raised.Cash >= due)
        {
            return WithPlayer(Downsized.Apply(raised), TurnPhase.AwaitingEnd, LastRoll, pendingDeck: null, drawn: null, Piles);
        }

        return WithPlayer(Bankruptcy.Apply(raised), TurnPhase.AwaitingEnd, LastRoll, pendingDeck: null, drawn: null, Piles);
    }

    private Game SettleDoodad()
    {
        DeckDraw<Doodad> drawn = Piles.Doodads.Draw();
        if (CurrentPlayer.Cash >= drawn.Card.Cost)
        {
            throw new TurnException("Cash covers this expense.");
        }

        CardPiles piles = Piles.WithDoodads(drawn.Deck);
        Player settled = Shortfall.Cover(CurrentPlayer, drawn.Card.Cost);
        return WithPlayer(
            settled,
            TurnPhase.AwaitingEnd,
            LastRoll,
            DeckHook.Doodad,
            DrawnCard.From(drawn.Card),
            piles);
    }
}
