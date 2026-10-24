namespace RatRaceEscape.Engine;

/// <summary>
/// Monthly bills that come with a profession. Per-child cost is applied
/// once the player has children.
/// </summary>
public sealed record ProfessionExpenses(
    Money Mortgage,
    Money CarLoan,
    Money CreditCard,
    Money Other,
    Money PerChild)
{
    public Money Total(int childCount)
    {
        if (childCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(childCount), "Child count cannot be negative.");
        }

        return Mortgage + CarLoan + CreditCard + Other + (PerChild * childCount);
    }
}
