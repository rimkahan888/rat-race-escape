namespace RatRaceEscape.Engine;

public sealed partial class Game
{
    /// <summary>
    /// Buys a fast-track venture for cash. The monthly amount becomes passive
    /// income and does not include salary. The player can buy while waiting
    /// to roll, or after the space is resolved.
    /// </summary>
    public Game BuyFastTrackDeal(string? id)
    {
        RequireAction();

        if (CurrentPlayer.Track != BoardTrack.FastTrack)
        {
            throw new TurnException("Fast-track deals are for players who have left the rat race.");
        }

        if (TurnPhase == TurnPhase.AwaitingResolve)
        {
            throw new TurnException("Resolve the space before buying a fast-track deal.");
        }

        FastTrackDeal? deal = FastTrackBoard.FindDeal(id);
        if (deal is null)
        {
            throw new TurnException("That fast-track deal is not on the table.");
        }

        Player buyer = AssetPurchase.BuyBusiness(
            CurrentPlayer,
            deal.Title,
            deal.Cost,
            Money.Zero,
            deal.MonthlyCashFlow);
        return WithPlayer(buyer, TurnPhase, LastRoll, PendingDeck, Drawn, Piles);
    }
}
