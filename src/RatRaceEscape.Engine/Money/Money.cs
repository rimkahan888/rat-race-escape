using System.Globalization;

namespace RatRaceEscape.Engine;

/// <summary>
/// An amount of money stored as an integer number of cents.
/// Arithmetic is checked, so overflow fails instead of wrapping.
/// </summary>
public readonly record struct Money : IComparable<Money>
{
    public const int CentsPerDollar = 100;

    public long Cents { get; }

    public Money(long cents)
    {
        Cents = cents;
    }

    public static Money Zero => new(0);

    public bool IsZero => Cents == 0;

    public bool IsPositive => Cents > 0;

    public bool IsNegative => Cents < 0;

    public static Money FromCents(long cents) => new(cents);

    public static Money FromDollars(long dollars) => new(checked(dollars * CentsPerDollar));

    public Money Add(Money other) => new(checked(Cents + other.Cents));

    public Money Subtract(Money other) => new(checked(Cents - other.Cents));

    public Money Negate() => new(checked(-Cents));

    public Money Multiply(int factor) => new(checked(Cents * factor));

    /// <summary>
    /// Divides the amount by <paramref name="divisor"/> when the cents split evenly.
    /// A remainder would drop money, so that case throws.
    /// </summary>
    public Money DivideExact(int divisor)
    {
        if (divisor == 0)
        {
            throw new DivideByZeroException("Cannot divide money by zero.");
        }

        if (Cents % divisor != 0)
        {
            throw new InvalidOperationException("Amount does not divide evenly.");
        }

        return new Money(Cents / divisor);
    }

    public static Money operator +(Money left, Money right) => left.Add(right);

    public static Money operator -(Money left, Money right) => left.Subtract(right);

    public static Money operator -(Money value) => value.Negate();

    public static Money operator *(Money money, int factor) => money.Multiply(factor);

    public static Money operator *(int factor, Money money) => money.Multiply(factor);

    public static bool operator >(Money left, Money right) => left.Cents > right.Cents;

    public static bool operator <(Money left, Money right) => left.Cents < right.Cents;

    public static bool operator >=(Money left, Money right) => left.Cents >= right.Cents;

    public static bool operator <=(Money left, Money right) => left.Cents <= right.Cents;

    public int CompareTo(Money other) => Cents.CompareTo(other.Cents);

    public override string ToString() => FormatAccounting(Cents);

    private static string FormatAccounting(long cents)
    {
        bool negative = cents < 0;
        ulong absolute = cents == long.MinValue
            ? 1UL << 63
            : (ulong)(negative ? -cents : cents);
        ulong dollars = absolute / CentsPerDollar;
        ulong remainder = absolute % CentsPerDollar;
        string body = string.Create(
            CultureInfo.InvariantCulture,
            $"${dollars:N0}.{remainder:00}");

        return negative ? $"({body})" : body;
    }
}
