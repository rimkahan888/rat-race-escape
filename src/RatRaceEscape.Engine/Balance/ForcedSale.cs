namespace RatRaceEscape.Engine;

/// <summary>
/// Sells assets back for the cash that was put into them until savings cover
/// a bill, or nothing is left. Stocks go first, oldest lot first, then
/// businesses, then property. A buyer of a property or business takes its
/// note. A lot or holding that cost nothing is surrendered.
/// </summary>
public static class ForcedSale
{
    public static Player Raise(Player player, Money needed)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (!needed.IsPositive)
        {
            throw new TurnException("A forced sale needs an amount due.");
        }

        player = SellStocks(player, needed);
        if (player.Cash >= needed)
        {
            return player;
        }

        player = SellBusinesses(player, needed);
        if (player.Cash >= needed)
        {
            return player;
        }

        return SellRealEstate(player, needed);
    }

    private static Player SellStocks(Player player, Money needed)
    {
        while (player.Cash < needed && player.BalanceSheet.Stocks.Count > 0)
        {
            StockHolding lot = player.BalanceSheet.Stocks[0];
            if (!lot.CostPerShare.IsPositive)
            {
                player = DropStock(player);
                continue;
            }

            int shares = SharesToCover(needed - player.Cash, lot.CostPerShare, lot.Shares);
            player = StockSale.Sell(player, lot.Symbol, shares, lot.CostPerShare);
        }

        return player;
    }

    private static Player SellBusinesses(Player player, Money needed)
    {
        while (player.Cash < needed && player.BalanceSheet.Businesses.Count > 0)
        {
            BusinessHolding business = player.BalanceSheet.Businesses[0];
            player = business.Cost.IsPositive
                ? AssetSale.SellBusiness(player, business.Name, business.Cost)
                : DropBusiness(player);
        }

        return player;
    }

    private static Player SellRealEstate(Player player, Money needed)
    {
        while (player.Cash < needed && player.BalanceSheet.RealEstate.Count > 0)
        {
            RealEstateHolding property = player.BalanceSheet.RealEstate[0];
            player = property.Cost.IsPositive
                ? AssetSale.SellRealEstate(player, property.Name, property.Cost)
                : DropRealEstate(player);
        }

        return player;
    }

    private static int SharesToCover(Money shortfall, Money price, int available)
    {
        long shares = (shortfall.Cents + price.Cents - 1) / price.Cents;
        if (shares < 1)
        {
            return 1;
        }

        if (shares > available)
        {
            return available;
        }

        return (int)shares;
    }

    private static Player DropStock(Player player)
    {
        List<StockHolding> stocks = new(player.BalanceSheet.Stocks.Count - 1);
        for (int index = 1; index < player.BalanceSheet.Stocks.Count; index++)
        {
            stocks.Add(player.BalanceSheet.Stocks[index]);
        }

        return Replace(player, player.Cash, stocks, player.BalanceSheet.RealEstate, player.BalanceSheet.Businesses);
    }

    private static Player DropBusiness(Player player)
    {
        List<BusinessHolding> businesses = new(player.BalanceSheet.Businesses.Count - 1);
        for (int index = 1; index < player.BalanceSheet.Businesses.Count; index++)
        {
            businesses.Add(player.BalanceSheet.Businesses[index]);
        }

        return Replace(player, player.Cash, player.BalanceSheet.Stocks, player.BalanceSheet.RealEstate, businesses);
    }

    private static Player DropRealEstate(Player player)
    {
        List<RealEstateHolding> realEstate = new(player.BalanceSheet.RealEstate.Count - 1);
        for (int index = 1; index < player.BalanceSheet.RealEstate.Count; index++)
        {
            realEstate.Add(player.BalanceSheet.RealEstate[index]);
        }

        return Replace(player, player.Cash, player.BalanceSheet.Stocks, realEstate, player.BalanceSheet.Businesses);
    }

    private static Player Replace(
        Player player,
        Money savings,
        IReadOnlyList<StockHolding> stocks,
        IReadOnlyList<RealEstateHolding> realEstate,
        IReadOnlyList<BusinessHolding> businesses)
    {
        BalanceSheet sheet = player.BalanceSheet;
        return player.WithBalanceSheet(BalanceSheet.Create(
            savings,
            stocks,
            realEstate,
            businesses,
            sheet.Mortgage,
            sheet.CarLoan,
            sheet.CreditCard,
            sheet.OtherLoans));
    }
}
