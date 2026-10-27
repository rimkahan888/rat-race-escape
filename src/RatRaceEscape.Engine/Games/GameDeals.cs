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

    /// <summary>
    /// Buys the property or business in front of the current player. The down
    /// payment comes out of cash. The mortgage or note stays on that holding,
    /// and the card's cash flow is the income the holding pays. The card then
    /// goes onto its discard pile. A stock is bought by the share instead.
    /// </summary>
    public Game BuyAsset()
    {
        RequireOffer();

        if (Drawn!.SmallDeal is SmallDeal small)
        {
            if (small.Kind == SmallDealKind.Stock)
            {
                throw new TurnException("This offer is sold in shares.");
            }

            return Take(AssetPurchase.BuyRealEstate(
                CurrentPlayer,
                small.Title,
                small.DownPayment,
                small.Mortgage,
                small.MonthlyCashFlow));
        }

        if (Drawn.BigDeal is BigDeal big)
        {
            Player buyer = big.Kind == BigDealKind.Business
                ? AssetPurchase.BuyBusiness(CurrentPlayer, big.Title, big.DownPayment, big.Loan, big.MonthlyCashFlow)
                : AssetPurchase.BuyRealEstate(CurrentPlayer, big.Title, big.DownPayment, big.Loan, big.MonthlyCashFlow);
            return Take(buyer);
        }

        if (Drawn.Market is not null)
        {
            throw new TurnException("This card is not for sale.");
        }

        throw new TurnException("This bill is already paid.");
    }

    /// <summary>
    /// Buys the card in front of the current player. A stock needs a share
    /// count. A property or business does not. The card is discarded either way.
    /// </summary>
    public Game Buy(int? shares)
    {
        RequireOffer();

        if (Drawn!.SmallDeal is SmallDeal deal && deal.Kind == SmallDealKind.Stock)
        {
            if (shares is null)
            {
                throw new TurnException("Stock purchases need a share count.");
            }

            return BuyShares(shares.Value);
        }

        if (shares is not null)
        {
            throw new TurnException("This offer is not sold in shares.");
        }

        return BuyAsset();
    }

    /// <summary>
    /// Turns the drawn card down without buying it. A bill that was already
    /// paid is not refunded. The card goes onto its discard pile.
    /// </summary>
    public Game Pass()
    {
        if (TurnPhase != TurnPhase.AwaitingEnd)
        {
            throw new TurnException(
                TurnPhase == TurnPhase.AwaitingRoll
                    ? "Roll the dice before passing."
                    : "Resolve the space before passing.");
        }

        if (Drawn is null)
        {
            throw new TurnException("There is no card to pass.");
        }

        return WithPlayer(
            CurrentPlayer,
            TurnPhase.AwaitingEnd,
            LastRoll,
            pendingDeck: null,
            drawn: null,
            Discard(Piles, Drawn));
    }

    private Game Take(Player buyer)
    {
        return WithPlayer(
            buyer,
            TurnPhase.AwaitingEnd,
            LastRoll,
            pendingDeck: null,
            drawn: null,
            Discard(Piles, Drawn));
    }

    private void RequireOffer()
    {
        if (TurnPhase != TurnPhase.AwaitingEnd)
        {
            throw new TurnException(
                TurnPhase == TurnPhase.AwaitingRoll
                    ? "Roll the dice before buying."
                    : "Resolve the space before buying.");
        }

        if (Drawn is null)
        {
            throw new TurnException("There is no offer to buy.");
        }
    }
}
