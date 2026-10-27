namespace RatRaceEscape.Engine;

/// <summary>
/// Buys a property or a business for its down payment. The cash put in is the
/// asset cost. The mortgage or note is the liability on that holding. Monthly
/// cash flow is already net of the loan, so it is passive income and is not
/// also added as an expense. The player is unchanged when the cash cannot
/// cover the down payment.
/// </summary>
public static class AssetPurchase
{
    public static Player BuyRealEstate(Player player, string name, Money downPayment, Money mortgage, Money monthlyCashFlow)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(name);
        RequireDownPayment(downPayment);

        RealEstateHolding holding = RealEstateHolding.Create(name, downPayment, mortgage, monthlyCashFlow);
        Money savings = Pay(player, downPayment);
        BalanceSheet sheet = player.BalanceSheet;
        List<RealEstateHolding> properties = new(sheet.RealEstate.Count + 1);
        properties.AddRange(sheet.RealEstate);
        properties.Add(holding);

        return player.WithBalanceSheet(BalanceSheet.Create(
            savings,
            sheet.Stocks,
            properties,
            sheet.Businesses,
            sheet.Mortgage,
            sheet.CarLoan,
            sheet.CreditCard,
            sheet.OtherLoans));
    }

    public static Player BuyBusiness(Player player, string name, Money downPayment, Money liability, Money monthlyCashFlow)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(name);
        RequireDownPayment(downPayment);

        BusinessHolding holding = BusinessHolding.Create(name, downPayment, liability, monthlyCashFlow);
        Money savings = Pay(player, downPayment);
        BalanceSheet sheet = player.BalanceSheet;
        List<BusinessHolding> businesses = new(sheet.Businesses.Count + 1);
        businesses.AddRange(sheet.Businesses);
        businesses.Add(holding);

        return player.WithBalanceSheet(BalanceSheet.Create(
            savings,
            sheet.Stocks,
            sheet.RealEstate,
            businesses,
            sheet.Mortgage,
            sheet.CarLoan,
            sheet.CreditCard,
            sheet.OtherLoans));
    }

    private static void RequireDownPayment(Money downPayment)
    {
        if (!downPayment.IsPositive)
        {
            throw new TurnException("Down payment must be greater than zero.");
        }
    }

    private static Money Pay(Player player, Money downPayment)
    {
        if (player.Cash < downPayment)
        {
            throw new TurnException("Cash cannot cover the down payment.");
        }

        return player.Cash - downPayment;
    }
}
