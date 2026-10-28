namespace RatRaceEscape.Engine.Tests;

public class ShortfallTests
{
    [Fact]
    public void Cash_that_covers_the_bill_is_spent_and_the_assets_stay()
    {
        Player baker = StockPurchase.Buy(Baker().WithCash(Money.FromDollars(200)), "PIER", 2, Money.FromDollars(25), Money.FromDollars(1));

        Player paid = Shortfall.Cover(baker, Money.FromDollars(40));

        Assert.Equal(baker.Cash - Money.FromDollars(40), paid.Cash);
        Assert.Equal(2, Assert.Single(paid.BalanceSheet.Stocks).Shares);
        Assert.False(paid.IsBankrupt);
    }

    [Fact]
    public void A_stock_sale_stops_once_the_bill_is_covered()
    {
        Player baker = StockPurchase.Buy(Baker().WithCash(Money.FromDollars(200)), "PIER", 4, Money.FromDollars(25), Money.FromDollars(1))
            .WithCash(Money.FromDollars(10));
        Player shop = AssetPurchase.BuyBusiness(
            baker.WithCash(baker.Cash + Money.FromDollars(300)),
            "Kiln and Clay",
            Money.FromDollars(300),
            Money.Zero,
            Money.FromDollars(180));
        shop = shop.WithCash(Money.FromDollars(10));

        Player raised = ForcedSale.Raise(shop, Money.FromDollars(60));

        Assert.Equal(Money.FromDollars(60), raised.Cash);
        Assert.Equal(2, Assert.Single(raised.BalanceSheet.Stocks).Shares);
        Assert.Single(raised.BalanceSheet.Businesses);
        Assert.False(raised.IsBankrupt);
    }

    [Fact]
    public void A_business_sells_before_property_when_the_stocks_run_out()
    {
        Player baker = AssetPurchase.BuyRealEstate(
            AssetPurchase.BuyBusiness(
                Baker().WithCash(Money.FromDollars(800)),
                "Kiln and Clay",
                Money.FromDollars(300),
                Money.Zero,
                Money.FromDollars(180)),
            "Cedar Lane Studio",
            Money.FromDollars(500),
            Money.FromDollars(4_500),
            Money.FromDollars(20));
        baker = baker.WithCash(Money.Zero);

        Player raised = ForcedSale.Raise(baker, Money.FromDollars(100));

        Assert.Equal(Money.FromDollars(300), raised.Cash);
        Assert.Empty(raised.BalanceSheet.Businesses);
        Assert.Single(raised.BalanceSheet.RealEstate);
    }

    [Fact]
    public void A_bill_that_survives_the_sale_wipes_the_sheet_and_the_bank_loan()
    {
        Player baker = BankLoan.Borrow(Baker(), 1);
        baker = StockPurchase.Buy(baker, "PIER", 4, Money.FromDollars(25), Money.FromDollars(1));
        int children = 1;
        baker = baker.WithChildCount(children).WithBoardIndex(4);

        Player broke = Shortfall.Cover(baker, Money.FromDollars(5_000));

        Assert.True(broke.IsBankrupt);
        Assert.Equal(Money.Zero, broke.Cash);
        Assert.Empty(broke.BalanceSheet.Stocks);
        Assert.Empty(broke.BalanceSheet.RealEstate);
        Assert.Empty(broke.BalanceSheet.Businesses);
        Assert.Empty(broke.BalanceSheet.OtherLoans);
        Assert.Equal(baker.BalanceSheet.Mortgage, broke.BalanceSheet.Mortgage);
        Assert.Equal(baker.BalanceSheet.CarLoan, broke.BalanceSheet.CarLoan);
        Assert.Equal(baker.BalanceSheet.CreditCard, broke.BalanceSheet.CreditCard);
        Assert.Equal(children, broke.ChildCount);
        Assert.Equal(4, broke.BoardIndex);
        Assert.False(baker.IsBankrupt);
        Assert.Equal(4, Assert.Single(baker.BalanceSheet.Stocks).Shares);
    }

    [Fact]
    public void A_worthless_lot_is_surrendered_and_an_already_bankrupt_player_stays()
    {
        Player baker = Baker();
        BalanceSheet sheet = baker.BalanceSheet;
        Player holder = baker.WithBalanceSheet(BalanceSheet.Create(
            Money.FromDollars(10),
            [StockHolding.Create("KITE", 2, Money.Zero, Money.Zero)],
            sheet.RealEstate,
            sheet.Businesses,
            sheet.Mortgage,
            sheet.CarLoan,
            sheet.CreditCard,
            sheet.OtherLoans));

        Player raised = ForcedSale.Raise(holder, Money.FromDollars(40));
        Player broke = Bankruptcy.Apply(holder);

        Assert.Equal(Money.FromDollars(10), raised.Cash);
        Assert.Empty(raised.BalanceSheet.Stocks);
        Assert.False(raised.IsBankrupt);
        Assert.True(broke.IsBankrupt);
        Assert.Same(broke, Shortfall.Cover(broke, Money.FromDollars(1)));
        Assert.Throws<ArgumentNullException>(() => Shortfall.Cover(null!, Money.FromDollars(1)));
        Assert.Throws<ArgumentNullException>(() => Bankruptcy.Apply(null!));
        TurnException due = Assert.Throws<TurnException>(() => ForcedSale.Raise(baker, Money.Zero));
        Assert.Contains("A forced sale needs an amount due.", due.Errors);
    }

    private static Player Baker()
    {
        return Player.Create("Ava", ProfessionCatalog.GetById("night-baker"), TokenColour.Amber);
    }
}
