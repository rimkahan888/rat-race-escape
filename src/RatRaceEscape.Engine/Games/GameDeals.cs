namespace RatRaceEscape.Engine;

public sealed partial class Game
{
    /// <summary>
    /// Buys shares of the stock offer in front of the current player. The
    /// price and dividend come from the card. Cash has to cover the whole
    /// lot. The card then goes onto the small-deal discard pile.
    /// </summary>
    public Game BuyShares(int shares)
    {
        if (TurnPhase != TurnPhase.AwaitingEnd)
        {
            throw new TurnException(
                TurnPhase == TurnPhase.AwaitingRoll
                    ? "Roll the dice before buying shares."
                    : "Resolve the space before buying shares.");
        }

        if (Drawn?.SmallDeal is not SmallDeal deal || deal.Kind != SmallDealKind.Stock)
        {
            throw new TurnException("There is no stock offer to buy.");
        }

        Player buyer = StockPurchase.Buy(
            CurrentPlayer,
            deal.Symbol!,
            shares,
            deal.PricePerShare,
            deal.DividendPerShare);

        return WithPlayer(
            buyer,
            TurnPhase.AwaitingEnd,
            LastRoll,
            pendingDeck: null,
            drawn: null,
            Discard(Piles, Drawn));
    }
}
