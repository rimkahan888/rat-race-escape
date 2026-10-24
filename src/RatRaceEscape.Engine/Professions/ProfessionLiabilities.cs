namespace RatRaceEscape.Engine;

/// <summary>
/// Outstanding balances that come with a profession. These are stocks on
/// the balance sheet, not the monthly payments.
/// </summary>
public sealed record ProfessionLiabilities(Money Mortgage, Money CarLoan, Money CreditCard)
{
    public Money Total => Mortgage + CarLoan + CreditCard;
}
