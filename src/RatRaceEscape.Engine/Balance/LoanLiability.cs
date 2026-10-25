namespace RatRaceEscape.Engine;

/// <summary>
/// A named loan other than the home mortgage, car loan, and credit card
/// that come with a profession. The payment is the monthly amount due.
/// </summary>
public sealed class LoanLiability
{
    private LoanLiability(string name, Money balance, Money monthlyPayment)
    {
        Name = name;
        Balance = balance;
        MonthlyPayment = monthlyPayment;
    }

    public string Name { get; }

    public Money Balance { get; }

    public Money MonthlyPayment { get; }

    public static LoanLiability Create(string name, Money balance, Money monthlyPayment)
    {
        ArgumentNullException.ThrowIfNull(name);

        string trimmed = name.Trim();
        List<string> errors = new();

        if (trimmed.Length is < 2 or > 40)
        {
            errors.Add("Loan name must be between 2 and 40 characters.");
        }

        if (trimmed.Any(char.IsControl))
        {
            errors.Add("Loan name cannot contain control characters.");
        }

        if (balance.IsNegative)
        {
            errors.Add("Loan balance cannot be negative.");
        }

        if (monthlyPayment.IsNegative)
        {
            errors.Add("Loan payment cannot be negative.");
        }

        if (balance.IsPositive && monthlyPayment.IsZero)
        {
            errors.Add("Loan payment is required when the loan balance is greater than zero.");
        }

        if (balance.IsPositive && monthlyPayment.IsPositive && monthlyPayment > balance)
        {
            errors.Add("Loan payment cannot exceed the loan balance.");
        }

        if (balance.IsZero && monthlyPayment.IsPositive)
        {
            errors.Add("Loan payment must be zero when there is no loan balance.");
        }

        if (errors.Count > 0)
        {
            throw new BalanceSheetValidationException(errors);
        }

        return new LoanLiability(trimmed, balance, monthlyPayment);
    }
}
