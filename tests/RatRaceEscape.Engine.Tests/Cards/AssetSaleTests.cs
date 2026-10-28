namespace RatRaceEscape.Engine.Tests;

public class AssetSaleTests
{
    [Fact]
    public void Selling_a_property_pays_the_price_and_the_buyer_takes_the_mortgage()
    {
        Player baker = Owner();
        Money assets = baker.BalanceSheet.TotalAssets;
        Money liabilities = baker.BalanceSheet.TotalLiabilities;
        Money home = baker.BalanceSheet.Mortgage;

        Player seller = AssetSale.SellRealEstate(baker, " cedar lane studio ", Money.FromDollars(2_500));

        Assert.Empty(seller.BalanceSheet.RealEstate);
        Assert.Equal(baker.Cash + Money.FromDollars(2_500), seller.Cash);
        Assert.Equal(home, seller.BalanceSheet.Mortgage);
        Assert.Equal(assets - Money.FromDollars(500) + Money.FromDollars(2_500), seller.BalanceSheet.TotalAssets);
        Assert.Equal(liabilities - Money.FromDollars(4_500), seller.BalanceSheet.TotalLiabilities);
        Assert.Equal(Money.Zero, IncomeStatement.For(seller).PassiveIncome);
        Assert.Equal(baker.Cash, baker.Cash);
        Assert.Single(baker.BalanceSheet.RealEstate);
    }

    [Fact]
    public void Selling_a_business_pays_the_price_and_the_buyer_takes_the_note()
    {
        Player baker = Baker().WithCash(Money.FromDollars(6_000));
        Player owner = AssetPurchase.BuyBusiness(
            baker,
            "Lantern Ferry",
            Money.FromDollars(5_000),
            Money.FromDollars(15_000),
            Money.FromDollars(320));
        Money liabilities = owner.BalanceSheet.TotalLiabilities;

        Player seller = AssetSale.SellBusiness(owner, "LANTERN FERRY", Money.FromDollars(6_000));

        Assert.Empty(seller.BalanceSheet.Businesses);
        Assert.Equal(owner.Cash + Money.FromDollars(6_000), seller.Cash);
        Assert.Equal(liabilities - Money.FromDollars(15_000), seller.BalanceSheet.TotalLiabilities);
        Assert.Equal(baker.BalanceSheet.Mortgage, seller.BalanceSheet.Mortgage);
        Assert.Equal(Money.Zero, IncomeStatement.For(seller).PassiveIncome);
        Assert.Single(owner.BalanceSheet.Businesses);
    }

    [Fact]
    public void The_first_matching_holding_sells_and_the_other_stays()
    {
        Player baker = Baker().WithCash(Money.FromDollars(2_000));
        Player owner = AssetPurchase.BuyRealEstate(
            AssetPurchase.BuyRealEstate(
                baker,
                "Cedar Lane Studio",
                Money.FromDollars(500),
                Money.FromDollars(4_500),
                Money.FromDollars(20)),
            "Cedar Lane Studio",
            Money.FromDollars(500),
            Money.FromDollars(4_500),
            Money.FromDollars(20));

        Player seller = AssetSale.SellRealEstate(owner, "Cedar Lane Studio", Money.FromDollars(2_500));

        RealEstateHolding left = Assert.Single(seller.BalanceSheet.RealEstate);
        Assert.Equal("Cedar Lane Studio", left.Name);
        Assert.Equal(Money.FromDollars(20), IncomeStatement.For(seller).PassiveIncome);
        Assert.Equal(owner.Cash + Money.FromDollars(2_500), seller.Cash);
    }

    [Fact]
    public void Selling_a_losing_property_removes_its_drag_on_income()
    {
        Player baker = Baker().WithCash(Money.FromDollars(1_200));
        Player owner = AssetPurchase.BuyRealEstate(
            baker,
            "Maple Court Cottage",
            Money.FromDollars(1_200),
            Money.FromDollars(8_000),
            Money.FromDollars(-10));

        Player seller = AssetSale.SellRealEstate(owner, "Maple Court Cottage", Money.FromDollars(1_000));

        Assert.Equal(Money.Zero, IncomeStatement.For(seller).PassiveIncome);
        Assert.Equal(Money.FromDollars(1_000), seller.Cash);
    }

    [Fact]
    public void A_missing_asset_a_bad_price_and_a_bad_name_leave_the_player()
    {
        Player baker = Owner();

        TurnException missing = Assert.Throws<TurnException>(() =>
            AssetSale.SellRealEstate(baker, "Kiln and Clay", Money.FromDollars(1)));
        TurnException business = Assert.Throws<TurnException>(() =>
            AssetSale.SellBusiness(baker, "Cedar Lane Studio", Money.FromDollars(1)));
        TurnException free = Assert.Throws<TurnException>(() =>
            AssetSale.SellRealEstate(baker, "Cedar Lane Studio", Money.Zero));
        TurnException name = Assert.Throws<TurnException>(() =>
            AssetSale.SellBusiness(baker, " ", Money.FromDollars(1)));

        Assert.Contains("The player does not own that property.", missing.Errors);
        Assert.Contains("The player does not own that business.", business.Errors);
        Assert.Contains("Sale price must be greater than zero.", free.Errors);
        Assert.Contains("An asset sale needs the asset name.", name.Errors);
        Assert.Throws<ArgumentNullException>(() => AssetSale.SellRealEstate(null!, "Cedar Lane Studio", Money.FromDollars(1)));
        Assert.Throws<ArgumentNullException>(() => AssetSale.SellBusiness(baker, null!, Money.FromDollars(1)));
        Assert.Single(baker.BalanceSheet.RealEstate);
        Assert.Equal(baker.Cash, baker.Cash);
    }

    private static Player Owner()
    {
        return AssetPurchase.BuyRealEstate(
            Baker(),
            "Cedar Lane Studio",
            Money.FromDollars(500),
            Money.FromDollars(4_500),
            Money.FromDollars(20));
    }

    private static Player Baker()
    {
        return Player.Create("Ava", ProfessionCatalog.GetById("night-baker"), TokenColour.Amber);
    }
}
