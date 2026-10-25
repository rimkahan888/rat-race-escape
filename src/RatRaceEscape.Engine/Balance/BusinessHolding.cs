namespace RatRaceEscape.Engine;

/// <summary>
/// A business on the asset side. Monthly cash flow is the net amount the
/// business pays the player, and it may be negative. Cost is the cash put in.
/// Liability is any loan taken to buy it.
/// </summary>
public sealed class BusinessHolding
{
    private BusinessHolding(string name, Money cost, Money liability, Money monthlyCashFlow)
    {
        Name = name;
        Cost = cost;
        Liability = liability;
        MonthlyCashFlow = monthlyCashFlow;
    }

    public string Name { get; }

    public Money Cost { get; }

    public Money Liability { get; }

    public Money MonthlyCashFlow { get; }

    public static BusinessHolding Create(string name, Money cost, Money liability, Money monthlyCashFlow)
    {
        ArgumentNullException.ThrowIfNull(name);

        string trimmed = name.Trim();
        List<string> errors = new();
        RequireName(errors, trimmed, "Business name");

        if (cost.IsNegative)
        {
            errors.Add("Business cost cannot be negative.");
        }

        if (liability.IsNegative)
        {
            errors.Add("Business liability cannot be negative.");
        }

        if (errors.Count > 0)
        {
            throw new BalanceSheetValidationException(errors);
        }

        return new BusinessHolding(trimmed, cost, liability, monthlyCashFlow);
    }

    private static void RequireName(List<string> errors, string trimmed, string label)
    {
        if (trimmed.Length is < 2 or > 60)
        {
            errors.Add($"{label} must be between 2 and 60 characters.");
        }

        if (trimmed.Any(char.IsControl))
        {
            errors.Add($"{label} cannot contain control characters.");
        }
    }
}
