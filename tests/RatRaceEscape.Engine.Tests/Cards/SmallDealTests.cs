namespace RatRaceEscape.Engine.Tests;

public class SmallDealTests
{
    [Fact]
    public void Stock_trims_the_wording_and_uppercases_the_symbol()
    {
        SmallDeal deal = SmallDeal.Stock(
            "lumen-glass",
            "  Lumen Glass  ",
            "  A harbor glassworks is selling shares.  ",
            " lumen ",
            Money.FromDollars(10),
            Money.Zero);

        Assert.Equal(SmallDealKind.Stock, deal.Kind);
        Assert.Equal("Lumen Glass", deal.Title);
        Assert.Equal("A harbor glassworks is selling shares.", deal.Summary);
        Assert.Equal("LUMEN", deal.Symbol);
        Assert.Equal(Money.FromDollars(10), deal.PricePerShare);
        Assert.Equal(Money.Zero, deal.DividendPerShare);
        Assert.Equal(Money.Zero, deal.DownPayment);
        Assert.Equal(Money.Zero, deal.Mortgage);
        Assert.Equal(Money.Zero, deal.MonthlyCashFlow);
    }

    [Fact]
    public void Property_keeps_a_negative_cash_flow_and_allows_no_mortgage()
    {
        SmallDeal owed = SmallDeal.Property(
            "maple-court-cottage",
            "Maple Court Cottage",
            "A tired cottage that still needs a new roof.",
            Money.FromDollars(1_200),
            Money.FromDollars(8_000),
            Money.FromDollars(-10));
        SmallDeal owned = SmallDeal.Property(
            "orchard-gate-shed",
            "Orchard Gate Shed",
            "A shed at the orchard gate, bought with cash.",
            Money.FromDollars(400),
            Money.Zero,
            Money.FromDollars(25));

        Assert.Equal(SmallDealKind.Property, owed.Kind);
        Assert.Null(owed.Symbol);
        Assert.Equal(Money.FromDollars(-10), owed.MonthlyCashFlow);
        Assert.Equal(Money.FromDollars(8_000), owed.Mortgage);
        Assert.Equal(Money.Zero, owned.Mortgage);
        Assert.Equal(Money.Zero, owned.PricePerShare);
        Assert.Equal(Money.Zero, owned.DividendPerShare);
    }

    [Fact]
    public void Stock_reports_symbol_price_and_property_fields_together()
    {
        CardValidationException exception = Assert.Throws<CardValidationException>(() =>
            SmallDeal.Create(
                "nope",
                SmallDealKind.Stock,
                "Lumen Glass",
                "A harbor glassworks is selling shares.",
                "1BAD",
                Money.Zero,
                Money.FromDollars(-1),
                Money.FromDollars(1),
                Money.FromDollars(2),
                Money.FromDollars(3)));

        Assert.Contains("Stock symbol must be 1 to 8 letters or digits and start with a letter.", exception.Errors);
        Assert.Contains("Share price must be greater than zero.", exception.Errors);
        Assert.Contains("Dividend per share cannot be negative.", exception.Errors);
        Assert.Contains("A stock deal does not have a down payment.", exception.Errors);
        Assert.Contains("A stock deal does not have a mortgage.", exception.Errors);
        Assert.Contains("A stock deal does not have property cash flow.", exception.Errors);
    }

    [Fact]
    public void Property_reports_share_fields_and_a_bad_down_payment_together()
    {
        CardValidationException exception = Assert.Throws<CardValidationException>(() =>
            SmallDeal.Create(
                "cedar-lane-studio",
                SmallDealKind.Property,
                "Cedar Lane Studio",
                "A one-room rental over a cabinet shop.",
                "PIER",
                Money.FromDollars(10),
                Money.FromDollars(1),
                Money.Zero,
                Money.FromDollars(-5),
                Money.FromDollars(20)));

        Assert.Contains("A property deal does not have a stock symbol.", exception.Errors);
        Assert.Contains("A property deal does not have a share price.", exception.Errors);
        Assert.Contains("A property deal does not have a dividend.", exception.Errors);
        Assert.Contains("Down payment must be greater than zero.", exception.Errors);
        Assert.Contains("Mortgage cannot be negative.", exception.Errors);
    }

    [Fact]
    public void Create_reports_wording_problems_and_an_unknown_kind_together()
    {
        CardValidationException exception = Assert.Throws<CardValidationException>(() =>
            SmallDeal.Create(
                "LUMEN",
                (SmallDealKind)9,
                " ",
                "too short",
                null,
                Money.Zero,
                Money.Zero,
                Money.Zero,
                Money.Zero,
                Money.Zero));

        Assert.Contains("Id must be a lowercase kebab-case slug.", exception.Errors);
        Assert.Contains("Title must be between 2 and 60 characters.", exception.Errors);
        Assert.Contains("Summary must be between 12 and 240 characters.", exception.Errors);
        Assert.Contains("Deal kind is not a small deal.", exception.Errors);
    }

    [Fact]
    public void Create_rejects_null_wording()
    {
        Assert.Throws<ArgumentNullException>(() => SmallDeal.Stock(null!, "Title", "A long enough summary.", "LUMEN", Money.FromDollars(1), Money.Zero));
        Assert.Throws<ArgumentNullException>(() => SmallDeal.Stock("lumen-glass", null!, "A long enough summary.", "LUMEN", Money.FromDollars(1), Money.Zero));
        Assert.Throws<ArgumentNullException>(() => SmallDeal.Stock("lumen-glass", "Title", null!, "LUMEN", Money.FromDollars(1), Money.Zero));
        Assert.Throws<ArgumentNullException>(() => SmallDeal.Stock("lumen-glass", "Title", "A long enough summary.", null!, Money.FromDollars(1), Money.Zero));
    }

    [Fact]
    public void Validation_exception_requires_an_error()
    {
        Assert.Throws<ArgumentNullException>(() => new CardValidationException(null!));
        Assert.Throws<ArgumentException>(() => new CardValidationException([]));
    }
}
