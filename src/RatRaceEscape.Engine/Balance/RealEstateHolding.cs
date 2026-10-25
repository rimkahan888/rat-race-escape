namespace RatRaceEscape.Engine;

/// <summary>
/// A property on the asset side. Monthly cash flow is the net amount the
/// property pays the player after its own mortgage, and it may be negative.
/// Cost is the cash put into the property. Mortgage is the balance still owed.
/// </summary>
public sealed class RealEstateHolding
{
    private RealEstateHolding(string name, Money cost, Money mortgage, Money monthlyCashFlow)
    {
        Name = name;
        Cost = cost;
        Mortgage = mortgage;
        MonthlyCashFlow = monthlyCashFlow;
    }

    public string Name { get; }

    public Money Cost { get; }

    public Money Mortgage { get; }

    public Money MonthlyCashFlow { get; }

    public static RealEstateHolding Create(string name, Money cost, Money mortgage, Money monthlyCashFlow)
    {
        ArgumentNullException.ThrowIfNull(name);

        string trimmed = name.Trim();
        List<string> errors = new();
        RequireName(errors, trimmed, "Real estate name");

        if (cost.IsNegative)
        {
            errors.Add("Real estate cost cannot be negative.");
        }

        if (mortgage.IsNegative)
        {
            errors.Add("Real estate mortgage cannot be negative.");
        }

        if (errors.Count > 0)
        {
            throw new BalanceSheetValidationException(errors);
        }

        return new RealEstateHolding(trimmed, cost, mortgage, monthlyCashFlow);
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
