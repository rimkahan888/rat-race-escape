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
        RequireAction();
        MarketCard offer = RequireShareBid();
        Player seller = StockSale.Sell(CurrentPlayer, offer.AssetName!, shares, offer.Price);
        return Take(seller);
    }

    /// <summary>
    /// Sells the property or business named on the market offer in front of
    /// the current player. The price comes from the card. A property sells
    /// ahead of a business when both use that name. The buyer takes the
    /// mortgage or the note. The card then goes onto the market discard pile.
    /// </summary>
    public Game SellAsset()
    {
        RequireAction();
        MarketCard offer = RequireAssetOffer();
        string name = offer.AssetName!;
        bool property = OwnsRealEstate(CurrentPlayer, name);
        if (!property && !OwnsBusiness(CurrentPlayer, name))
        {
            throw new TurnException("The player does not own that asset.");
        }

        Player seller = property
            ? AssetSale.SellRealEstate(CurrentPlayer, name, offer.Price)
            : AssetSale.SellBusiness(CurrentPlayer, name, offer.Price);
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

    private MarketCard RequireAssetOffer()
    {
        if (TurnPhase != TurnPhase.AwaitingEnd)
        {
            throw new TurnException(
                TurnPhase == TurnPhase.AwaitingRoll
                    ? "Roll the dice before selling."
                    : "Resolve the space before selling.");
        }

        if (Drawn?.Market is not MarketCard card || card.Kind != MarketKind.Offer || card.PerShare)
        {
            throw new TurnException("There is no asset offer to sell.");
        }

        return card;
    }

    private static bool OwnsRealEstate(Player player, string name)
    {
        foreach (RealEstateHolding property in player.BalanceSheet.RealEstate)
        {
            if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool OwnsBusiness(Player player, string name)
    {
        foreach (BusinessHolding business in player.BalanceSheet.Businesses)
        {
            if (business.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
