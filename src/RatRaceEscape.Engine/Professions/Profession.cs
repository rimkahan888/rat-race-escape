namespace RatRaceEscape.Engine;

/// <summary>
/// A starting job. Salary, taxes, and bills determine the monthly cash flow
/// that later feeds the player's income statement.
/// </summary>
public sealed class Profession
{
    private Profession(
        string id,
        string title,
        string summary,
        Money monthlySalary,
        Money monthlyTaxes,
        ProfessionExpenses expenses,
        Money startingSavings,
        ProfessionLiabilities liabilities)
    {
        Id = id;
        Title = title;
        Summary = summary;
        MonthlySalary = monthlySalary;
        MonthlyTaxes = monthlyTaxes;
        Expenses = expenses;
        StartingSavings = startingSavings;
        Liabilities = liabilities;
    }

    public string Id { get; }

    public string Title { get; }

    public string Summary { get; }

    public Money MonthlySalary { get; }

    public Money MonthlyTaxes { get; }

    public ProfessionExpenses Expenses { get; }

    public Money StartingSavings { get; }

    public ProfessionLiabilities Liabilities { get; }

    public Money MonthlyCashFlow(int childCount)
    {
        return MonthlySalary - MonthlyTaxes - Expenses.Total(childCount);
    }

    public static Profession Create(
        string id,
        string title,
        string summary,
        Money monthlySalary,
        Money monthlyTaxes,
        ProfessionExpenses expenses,
        Money startingSavings,
        ProfessionLiabilities liabilities)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(summary);
        ArgumentNullException.ThrowIfNull(expenses);
        ArgumentNullException.ThrowIfNull(liabilities);

        string trimmedTitle = title.Trim();
        string trimmedSummary = summary.Trim();
        List<string> errors = new();

        if (!IsSlug(id))
        {
            errors.Add("Id must be a lowercase kebab-case slug.");
        }

        if (trimmedTitle.Length is < 2 or > 60)
        {
            errors.Add("Title must be between 2 and 60 characters.");
        }

        if (trimmedSummary.Length is < 12 or > 240)
        {
            errors.Add("Summary must be between 12 and 240 characters.");
        }

        if (!monthlySalary.IsPositive)
        {
            errors.Add("Monthly salary must be greater than zero.");
        }

        if (monthlyTaxes.IsNegative || monthlyTaxes >= monthlySalary)
        {
            errors.Add("Monthly taxes must be zero or greater and less than salary.");
        }

        RequireLoan(
            errors,
            "Mortgage",
            "mortgage",
            liabilities.Mortgage,
            expenses.Mortgage);
        RequireLoan(
            errors,
            "Car loan",
            "car loan",
            liabilities.CarLoan,
            expenses.CarLoan);
        RequireLoan(
            errors,
            "Credit card",
            "credit card",
            liabilities.CreditCard,
            expenses.CreditCard);

        if (expenses.Other.IsNegative)
        {
            errors.Add("Other expenses cannot be negative.");
        }

        if (expenses.PerChild.IsNegative)
        {
            errors.Add("Per-child expense cannot be negative.");
        }

        if (startingSavings.IsNegative)
        {
            errors.Add("Starting savings cannot be negative.");
        }

        if (!HasNegativePart(expenses) && (monthlySalary - monthlyTaxes - expenses.Total(0)) <= Money.Zero)
        {
            errors.Add("Monthly cash flow with no children must be greater than zero.");
        }

        if (errors.Count > 0)
        {
            throw new ProfessionValidationException(errors);
        }

        return new Profession(
            id,
            trimmedTitle,
            trimmedSummary,
            monthlySalary,
            monthlyTaxes,
            expenses,
            startingSavings,
            liabilities);
    }

    private static void RequireLoan(
        List<string> errors,
        string label,
        string lowerLabel,
        Money balance,
        Money payment)
    {
        if (balance.IsNegative)
        {
            errors.Add($"{label} balance cannot be negative.");
        }

        if (payment.IsNegative)
        {
            errors.Add($"{label} payment cannot be negative.");
        }

        if (balance.IsPositive && payment.IsZero)
        {
            errors.Add($"{label} payment is required when the {lowerLabel} balance is greater than zero.");
        }

        if (balance.IsPositive && payment.IsPositive && payment > balance)
        {
            errors.Add($"{label} payment cannot exceed the {lowerLabel} balance.");
        }

        if (balance.IsZero && !payment.IsZero && !payment.IsNegative)
        {
            errors.Add($"{label} payment must be zero when there is no {lowerLabel} balance.");
        }
    }

    private static bool HasNegativePart(ProfessionExpenses expenses)
    {
        return expenses.Mortgage.IsNegative
            || expenses.CarLoan.IsNegative
            || expenses.CreditCard.IsNegative
            || expenses.Other.IsNegative
            || expenses.PerChild.IsNegative;
    }

    private static bool IsSlug(string id)
    {
        if (id.Length is < 2 or > 40)
        {
            return false;
        }

        for (int index = 0; index < id.Length; index++)
        {
            char character = id[index];
            if (character is (>= 'a' and <= 'z') or (>= '0' and <= '9'))
            {
                continue;
            }

            if (character != '-' || index == 0 || index == id.Length - 1 || id[index - 1] == '-')
            {
                return false;
            }
        }

        return true;
    }
}
