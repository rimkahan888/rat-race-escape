namespace RatRaceEscape.Engine;

/// <summary>
/// Sells a property or a business for a price paid in cash. The buyer takes
/// the mortgage or the note with the asset, so that liability leaves the
/// sheet. The first holding with the name is the one that sells.
/// </summary>
public static class AssetSale
{
    public static Player SellRealEstate(Player player, string name, Money price)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(name);
        string trimmed = RequireName(name);
        RequirePrice(price);

        List<RealEstateHolding> kept = new(player.BalanceSheet.RealEstate.Count);
        bool sold = false;
        foreach (RealEstateHolding property in player.BalanceSheet.RealEstate)
        {
            if (!sold && property.Name.Equals(trimmed, StringComparison.OrdinalIgnoreCase))
            {
                sold = true;
                continue;
            }

            kept.Add(property);
        }

        if (!sold)
        {
            throw new TurnException("The player does not own that property.");
        }

        return Replace(player, player.Cash + price, kept, player.BalanceSheet.Businesses);
    }

    public static Player SellBusiness(Player player, string name, Money price)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(name);
        string trimmed = RequireName(name);
        RequirePrice(price);

        List<BusinessHolding> kept = new(player.BalanceSheet.Businesses.Count);
        bool sold = false;
        foreach (BusinessHolding business in player.BalanceSheet.Businesses)
        {
            if (!sold && business.Name.Equals(trimmed, StringComparison.OrdinalIgnoreCase))
            {
                sold = true;
                continue;
            }

            kept.Add(business);
        }

        if (!sold)
        {
            throw new TurnException("The player does not own that business.");
        }

        return Replace(player, player.Cash + price, player.BalanceSheet.RealEstate, kept);
    }

    private static Player Replace(
        Player player,
        Money savings,
        IReadOnlyList<RealEstateHolding> realEstate,
        IReadOnlyList<BusinessHolding> businesses)
    {
        BalanceSheet sheet = player.BalanceSheet;
        return player.WithBalanceSheet(BalanceSheet.Create(
            savings,
            sheet.Stocks,
            realEstate,
            businesses,
            sheet.Mortgage,
            sheet.CarLoan,
            sheet.CreditCard,
            sheet.OtherLoans));
    }

    private static string RequireName(string name)
    {
        string trimmed = name.Trim();
        if (trimmed.Length is < 2 or > 60 || trimmed.Any(char.IsControl))
        {
            throw new TurnException("An asset sale needs the asset name.");
        }

        return trimmed;
    }

    private static void RequirePrice(Money price)
    {
        if (!price.IsPositive)
        {
            throw new TurnException("Sale price must be greater than zero.");
        }
    }
}
