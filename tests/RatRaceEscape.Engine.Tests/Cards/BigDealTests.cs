namespace RatRaceEscape.Engine.Tests;

public class BigDealTests
{
    [Fact]
    public void Property_trims_the_wording_and_keeps_a_negative_cash_flow()
    {
        BigDeal deal = BigDeal.Property(
            "lantern-yard",
            "  Lantern Yard  ",
            "  A courtyard of rooms that needs new wiring.  ",
            Money.FromDollars(5_000),
            Money.FromDollars(36_000),
            Money.FromDollars(-80));

        Assert.Equal(BigDealKind.Property, deal.Kind);
        Assert.Equal("Lantern Yard", deal.Title);
        Assert.Equal("A courtyard of rooms that needs new wiring.", deal.Summary);
        Assert.Equal(Money.FromDollars(5_000), deal.DownPayment);
        Assert.Equal(Money.FromDollars(36_000), deal.Loan);
        Assert.Equal(Money.FromDollars(-80), deal.MonthlyCashFlow);
    }

    [Fact]
    public void Business_allows_a_note_of_zero()
    {
        BigDeal deal = BigDeal.Business(
            "kiln-and-clay",
            "Kiln and Clay",
            "A pottery workshop bought with cash.",
            Money.FromDollars(3_000),
            Money.Zero,
            Money.FromDollars(180));

        Assert.Equal(BigDealKind.Business, deal.Kind);
        Assert.Equal(Money.Zero, deal.Loan);
        Assert.Equal(Money.FromDollars(180), deal.MonthlyCashFlow);
    }

    [Fact]
    public void Create_reports_wording_kind_and_money_problems_together()
    {
        CardValidationException exception = Assert.Throws<CardValidationException>(() =>
            BigDeal.Create(
                "NOPE",
                (BigDealKind)9,
                " ",
                "too short",
                Money.Zero,
                Money.FromDollars(-1),
                Money.FromDollars(10)));

        Assert.Contains("Id must be a lowercase kebab-case slug.", exception.Errors);
        Assert.Contains("Title must be between 2 and 60 characters.", exception.Errors);
        Assert.Contains("Summary must be between 12 and 240 characters.", exception.Errors);
        Assert.Contains("Deal kind is not a big deal.", exception.Errors);
        Assert.Contains("Down payment must be greater than zero.", exception.Errors);
        Assert.Contains("Loan cannot be negative.", exception.Errors);
    }

    [Fact]
    public void Create_rejects_null_wording()
    {
        Assert.Throws<ArgumentNullException>(() =>
            BigDeal.Property(null!, "Title", "A long enough summary.", Money.FromDollars(1), Money.Zero, Money.Zero));
        Assert.Throws<ArgumentNullException>(() =>
            BigDeal.Business("kiln-and-clay", null!, "A long enough summary.", Money.FromDollars(1), Money.Zero, Money.Zero));
        Assert.Throws<ArgumentNullException>(() =>
            BigDeal.Business("kiln-and-clay", "Title", null!, Money.FromDollars(1), Money.Zero, Money.Zero));
    }
}
