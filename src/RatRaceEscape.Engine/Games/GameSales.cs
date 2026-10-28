namespace RatRaceEscape.Engine;

public sealed partial class Game
{
    /// <summary>
    /// Sells shares into the market bid in front of the current player. The
    /// symbol and the price come from the card. Lots of that symbol are sold
    /// oldest first. The card then goes onto the market discard pile.
    /// </summary>
    public Game SellShares(int shares)
    {
        MarketCard offer = RequireShareBid();
        Player seller = StockSale.Sell(CurrentPlayer, offer.AssetName!, shares, offer.Price);
        return Take(seller);
    }

    private MarketCard RequireShareBid()
    {
        if (TurnPhase != TurnPhase.AwaitingEnd)
        {
            throw new TurnException(
                TurnPhase == TurnPhase.AwaitingRoll
                    ? "Roll the dice before selling shares."
                    : "Resolve the space before selling shares.");
        }

        if (Drawn?.Market is not MarketCard card || card.Kind != MarketKind.Offer || !card.PerShare)
        {
            throw new TurnException("There is no share bid to sell against.");
        }

        return card;
    }
}
