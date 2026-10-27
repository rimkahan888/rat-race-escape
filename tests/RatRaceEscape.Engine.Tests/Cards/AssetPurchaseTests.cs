namespace RatRaceEscape.Engine.Tests;

public class AssetPurchaseTests
{
    [Fact]
    public void Buying_a_property_moves_the_down_payment_and_keeps_the_mortgage_on_the_holding()
    {
        Player baker = Baker();
        Money expenses = IncomeStatement.For(baker).TotalExpenses;

        Player buyer = AssetPurchase.BuyRealEstate(
            baker,
            "Cedar Lane Studio",
            Money.FromDollars(500),
            Money.FromDollars(4_500),
            Money.FromDollars(20));

        RealEstateHolding property = Assert.Single(buyer.BalanceSheet.RealEstate);
        Assert.Equal("Cedar Lane Studio", property.Name);
        Assert.Equal(Money.FromDollars(500), property.Cost);
        Assert.Equal(Money.FromDollars(4_500), property.Mortgage);
        Assert.Equal(Money.FromDollars(20), property.MonthlyCashFlow);
        Assert.Equal(baker.Cash - Money.FromDollars(500), buyer.Cash);
        Assert.Equal(baker.BalanceSheet.Mortgage, buyer.BalanceSheet.Mortgage);
        Assert.Empty(buyer.BalanceSheet.OtherLoans);
        Assert.Equal(baker.BalanceSheet.TotalAssets, buyer.BalanceSheet.TotalAssets);
        Assert.Equal(baker.BalanceSheet.TotalLiabilities + Money.FromDollars(4_500), buyer.BalanceSheet.TotalLiabilities);
        Assert.Equal(Money.FromDollars(20), IncomeStatement.For(buyer).PassiveIncome);
        Assert.Equal(expenses, IncomeStatement.For(buyer).TotalExpenses);
        Assert.Equal(CashFlow.For(baker).Monthly + Money.FromDollars(20), CashFlow.For(buyer).Monthly);
        Assert.Equal(baker.Cash, baker.Cash);
    }

    [Fact]
    public void A_property_whose_note_outruns_the_rent_reduces_passive_income()
    {
        Player baker = Baker().WithCash(Money.FromDollars(1_200));

        Player buyer = AssetPurchase.BuyRealEstate(
            baker,
            "Maple Court Cottage",
            Money.FromDollars(1_200),
            Money.FromDollars(8_000),
            Money.FromDollars(-10));

        Assert.Equal(Money.Zero, buyer.Cash);
        Assert.Equal(Money.FromDollars(-10), IncomeStatement.For(buyer).PassiveIncome);
        Assert.Equal(CashFlow.For(baker).Monthly - Money.FromDollars(10), CashFlow.For(buyer).Monthly);
        Assert.Equal(IncomeStatement.For(baker).TotalExpenses, IncomeStatement.For(buyer).TotalExpenses);
    }

    [Fact]
    public void Buying_a_business_keeps_the_note_on_the_holding()
    {
        Player baker = Baker().WithCash(Money.FromDollars(5_000));
        Money expenses = IncomeStatement.For(baker).TotalExpenses;

        Player buyer = AssetPurchase.BuyBusiness(
            baker,
            "Lantern Ferry",
            Money.FromDollars(5_000),
            Money.FromDollars(15_000),
            Money.FromDollars(320));

        BusinessHolding business = Assert.Single(buyer.BalanceSheet.Businesses);
        Assert.Equal("Lantern Ferry", business.Name);
        Assert.Equal(Money.FromDollars(5_000), business.Cost);
        Assert.Equal(Money.FromDollars(15_000), business.Liability);
        Assert.Equal(Money.Zero, buyer.Cash);
        Assert.Equal(baker.BalanceSheet.Mortgage, buyer.BalanceSheet.Mortgage);
        Assert.Empty(buyer.BalanceSheet.OtherLoans);
        Assert.Empty(buyer.BalanceSheet.RealEstate);
        Assert.Equal(baker.BalanceSheet.TotalAssets, buyer.BalanceSheet.TotalAssets);
        Assert.Equal(baker.BalanceSheet.TotalLiabilities + Money.FromDollars(15_000), buyer.BalanceSheet.TotalLiabilities);
        Assert.Equal(Money.FromDollars(320), IncomeStatement.For(buyer).PassiveIncome);
        Assert.Equal(expenses, IncomeStatement.For(buyer).TotalExpenses);
    }

    [Fact]
    public void A_business_bought_with_no_note_does_not_add_a_liability()
    {
        Player baker = Baker().WithCash(Money.FromDollars(3_000));

        Player buyer = AssetPurchase.BuyBusiness(
            baker,
            "Kiln and Clay",
            Money.FromDollars(3_000),
            Money.Zero,
            Money.FromDollars(180));

        Assert.Equal(Money.Zero, buyer.Cash);
        Assert.Equal(baker.BalanceSheet.TotalLiabilities, buyer.BalanceSheet.TotalLiabilities);
        Assert.Equal(Money.FromDollars(180), Assert.Single(buyer.BalanceSheet.Businesses).MonthlyCashFlow);
    }

    [Fact]
    public void A_second_property_is_kept_beside_the_first()
    {
        Player baker = Baker().WithCash(Money.FromDollars(1_000));

        Player twice = AssetPurchase.BuyRealEstate(
            AssetPurchase.BuyRealEstate(baker, "Cedar Lane Studio", Money.FromDollars(500), Money.FromDollars(4_500), Money.FromDollars(20)),
            "Orchard Gate Shed",
            Money.FromDollars(400),
            Money.Zero,
            Money.FromDollars(25));

        Assert.Equal(2, twice.BalanceSheet.RealEstate.Count);
        Assert.Equal(Money.FromDollars(45), IncomeStatement.For(twice).PassiveIncome);
        Assert.Equal(baker.BalanceSheet.TotalLiabilities + Money.FromDollars(4_500), twice.BalanceSheet.TotalLiabilities);
    }

    [Fact]
    public void Buy_fails_when_cash_is_short_and_leaves_the_player()
    {
        Player baker = Baker().WithCash(Money.FromDollars(499));

        TurnException property = Assert.Throws<TurnException>(() =>
            AssetPurchase.BuyRealEstate(baker, "Cedar Lane Studio", Money.FromDollars(500), Money.FromDollars(4_500), Money.FromDollars(20)));
        TurnException business = Assert.Throws<TurnException>(() =>
            AssetPurchase.BuyBusiness(baker, "Kiln and Clay", Money.FromDollars(500), Money.Zero, Money.FromDollars(180)));

        Assert.Contains("Cash cannot cover the down payment.", property.Errors);
        Assert.Contains("Cash cannot cover the down payment.", business.Errors);
        Assert.Equal(Money.FromDollars(499), baker.Cash);
        Assert.Empty(baker.BalanceSheet.RealEstate);
        Assert.Empty(baker.BalanceSheet.Businesses);
    }

    [Fact]
    public void Buy_rejects_a_missing_down_payment_and_null_arguments()
    {
        Player baker = Baker();

        TurnException zero = Assert.Throws<TurnException>(() =>
            AssetPurchase.BuyRealEstate(baker, "Shed", Money.Zero, Money.Zero, Money.FromDollars(1)));
        TurnException negative = Assert.Throws<TurnException>(() =>
            AssetPurchase.BuyBusiness(baker, "Stall", Money.FromDollars(-1), Money.Zero, Money.FromDollars(1)));

        Assert.Contains("Down payment must be greater than zero.", zero.Errors);
        Assert.Contains("Down payment must be greater than zero.", negative.Errors);
        Assert.Throws<ArgumentNullException>(() =>
            AssetPurchase.BuyRealEstate(null!, "Shed", Money.FromDollars(1), Money.Zero, Money.Zero));
        Assert.Throws<ArgumentNullException>(() =>
            AssetPurchase.BuyRealEstate(baker, null!, Money.FromDollars(1), Money.Zero, Money.Zero));
        Assert.Throws<ArgumentNullException>(() =>
            AssetPurchase.BuyBusiness(null!, "Stall", Money.FromDollars(1), Money.Zero, Money.Zero));
        Assert.Throws<ArgumentNullException>(() =>
            AssetPurchase.BuyBusiness(baker, null!, Money.FromDollars(1), Money.Zero, Money.Zero));
        Assert.Empty(baker.BalanceSheet.RealEstate);
    }

    private static Player Baker()
    {
        return Player.Create("Ava", ProfessionCatalog.GetById("night-baker"), TokenColour.Amber);
    }
}
