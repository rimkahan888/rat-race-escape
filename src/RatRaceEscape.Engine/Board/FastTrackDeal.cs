namespace RatRaceEscape.Engine;

/// <summary>
/// A venture offered on the fast track. The cost comes out of cash and the
/// monthly amount is passive income. Salary is not part of that income.
/// </summary>
public sealed class FastTrackDeal
{
    private FastTrackDeal(string id, string title, string summary, Money cost, Money monthlyCashFlow)
    {
        Id = id;
        Title = title;
        Summary = summary;
        Cost = cost;
        MonthlyCashFlow = monthlyCashFlow;
    }

    public string Id { get; }

    public string Title { get; }

    public string Summary { get; }

    public Money Cost { get; }

    public Money MonthlyCashFlow { get; }

    public static FastTrackDeal Create(string id, string title, string summary, Money cost, Money monthlyCashFlow)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(summary);

        string trimmedId = id.Trim();
        string trimmedTitle = title.Trim();
        string trimmedSummary = summary.Trim();
        List<string> errors = new();

        if (trimmedId.Length is < 2 or > 40 || trimmedId.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-'))
        {
            errors.Add("Fast-track deal id must be 2 to 40 letters, digits, or hyphens.");
        }

        if (trimmedTitle.Length is < 2 or > 60)
        {
            errors.Add("Fast-track deal title must be between 2 and 60 characters.");
        }

        if (trimmedSummary.Length is < 2 or > 200)
        {
            errors.Add("Fast-track deal summary must be between 2 and 200 characters.");
        }

        if (!cost.IsPositive)
        {
            errors.Add("Fast-track deal cost must be greater than zero.");
        }

        if (!monthlyCashFlow.IsPositive)
        {
            errors.Add("Fast-track deal cash flow must be greater than zero.");
        }

        if (errors.Count > 0)
        {
            throw new CardValidationException(errors);
        }

        return new FastTrackDeal(trimmedId, trimmedTitle, trimmedSummary, cost, monthlyCashFlow);
    }
}
