namespace RatRaceEscape.Engine.Tests;

public class MoneyTests
{
    [Fact]
    public void Default_value_is_zero()
    {
        Money money = default;

        Assert.Equal(0, money.Cents);
        Assert.Equal(Money.Zero, money);
        Assert.True(money.IsZero);
    }

    [Fact]
    public void FromDollars_converts_whole_dollars_to_cents()
    {
        Money money = Money.FromDollars(12);

        Assert.Equal(1_200, money.Cents);
    }

    [Fact]
    public void FromCents_keeps_the_exact_minor_units()
    {
        Money money = Money.FromCents(1_250);

        Assert.Equal(1_250, money.Cents);
    }

    [Fact]
    public void FromDollars_throws_when_the_conversion_overflows()
    {
        Assert.Throws<OverflowException>(() => Money.FromDollars(long.MaxValue));
    }

    [Theory]
    [InlineData(0, true, false, false)]
    [InlineData(1, false, true, false)]
    [InlineData(-1, false, false, true)]
    public void Sign_flags_follow_the_cent_count(long cents, bool isZero, bool isPositive, bool isNegative)
    {
        Money money = Money.FromCents(cents);

        Assert.Equal(isZero, money.IsZero);
        Assert.Equal(isPositive, money.IsPositive);
        Assert.Equal(isNegative, money.IsNegative);
    }

    [Fact]
    public void Add_sums_cent_counts()
    {
        Money sum = Money.FromCents(200).Add(Money.FromCents(50));

        Assert.Equal(Money.FromCents(250), sum);
    }

    [Fact]
    public void Addition_operator_matches_Add()
    {
        Assert.Equal(Money.FromDollars(3), Money.FromDollars(1) + Money.FromDollars(2));
    }

    [Fact]
    public void Add_throws_when_the_sum_overflows()
    {
        Money almostMax = Money.FromCents(long.MaxValue);

        Assert.Throws<OverflowException>(() => almostMax.Add(Money.FromCents(1)));
        Assert.Throws<OverflowException>(() => almostMax + Money.FromCents(1));
    }

    [Fact]
    public void Subtract_returns_the_difference()
    {
        Assert.Equal(Money.FromCents(50), Money.FromCents(200) - Money.FromCents(150));
        Assert.Equal(Money.FromCents(-50), Money.FromCents(100) - Money.FromCents(150));
    }

    [Fact]
    public void Negate_flips_the_sign()
    {
        Assert.Equal(Money.FromCents(-25), -Money.FromCents(25));
        Assert.Equal(Money.FromCents(25), -Money.FromCents(-25));
    }

    [Fact]
    public void Negate_throws_for_the_minimum_cent_count()
    {
        Money minimum = Money.FromCents(long.MinValue);

        Assert.Throws<OverflowException>(() => minimum.Negate());
        Assert.Throws<OverflowException>(() => -minimum);
    }

    [Fact]
    public void Multiply_scales_by_an_integer_factor()
    {
        Assert.Equal(Money.FromCents(40), Money.FromCents(10) * 4);
        Assert.Equal(Money.FromCents(40), 4 * Money.FromCents(10));
        Assert.Equal(Money.Zero, Money.FromDollars(5) * 0);
        Assert.Equal(Money.FromCents(-30), Money.FromCents(10) * -3);
    }

    [Fact]
    public void Multiply_throws_when_the_product_overflows()
    {
        Money huge = Money.FromCents(long.MaxValue);

        Assert.Throws<OverflowException>(() => huge * 2);
    }

    [Fact]
    public void DivideExact_splits_an_even_amount()
    {
        Assert.Equal(Money.FromCents(100), Money.FromCents(300).DivideExact(3));
        Assert.Equal(Money.FromCents(50), Money.FromCents(-100).DivideExact(-2));
    }

    [Fact]
    public void DivideExact_rejects_a_remainder()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => Money.FromCents(100).DivideExact(3));

        Assert.Contains("evenly", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DivideExact_rejects_zero()
    {
        Assert.Throws<DivideByZeroException>(() => Money.FromCents(100).DivideExact(0));
    }

    [Fact]
    public void Comparison_orders_by_cents()
    {
        Money lower = Money.FromDollars(1);
        Money higher = Money.FromDollars(2);
        Money sameAsHigher = Money.FromCents(200);

        Assert.True(higher > lower);
        Assert.True(lower < higher);
        Assert.True(higher >= sameAsHigher);
        Assert.True(sameAsHigher <= higher);
        Assert.True(higher.CompareTo(lower) > 0);
        Assert.Equal(0, higher.CompareTo(Money.FromCents(200)));
        Assert.True(lower.CompareTo(higher) < 0);
    }

    [Fact]
    public void Equal_amounts_are_interchangeable()
    {
        Money dollars = Money.FromDollars(8);
        Money cents = Money.FromCents(800);

        Assert.Equal(dollars, cents);
        Assert.True(dollars == cents);
        Assert.False(dollars != cents);
        Assert.Equal(dollars.GetHashCode(), cents.GetHashCode());
        Assert.NotEqual(dollars, Money.FromDollars(9));
    }

    [Theory]
    [InlineData(123_456, "$1,234.56")]
    [InlineData(-123_456, "($1,234.56)")]
    [InlineData(0, "$0.00")]
    [InlineData(5, "$0.05")]
    [InlineData(-5, "($0.05)")]
    [InlineData(1_000, "$10.00")]
    [InlineData(100_000_000, "$1,000,000.00")]
    public void ToString_uses_accounting_format(long cents, string expected)
    {
        Assert.Equal(expected, Money.FromCents(cents).ToString());
    }

    [Fact]
    public void ToString_formats_the_minimum_cent_count_without_overflowing()
    {
        Assert.Equal("($92,233,720,368,547,758.08)", Money.FromCents(long.MinValue).ToString());
    }
}
