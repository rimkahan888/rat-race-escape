namespace RatRaceEscape.Engine;

/// <summary>
/// One card in the big-deals deck. The down payment is the cash the buyer
/// puts in. The loan is the mortgage on a property, or the note on a business.
/// Monthly cash flow is what is left after that loan, and it may be negative.
/// </summary>
public sealed class BigDeal
{
    private BigDeal(
        string id,
        BigDealKind kind,
        string title,
        string summary,
        Money downPayment,
        Money loan,
        Money monthlyCashFlow)
    {
        Id = id;
        Kind = kind;
        Title = title;
        Summary = summary;
        DownPayment = downPayment;
        Loan = loan;
        MonthlyCashFlow = monthlyCashFlow;
    }

    public string Id { get; }

    public BigDealKind Kind { get; }

    public string Title { get; }

    public string Summary { get; }

    public Money DownPayment { get; }

    /// <summary>
    /// The mortgage on a property, or the note still owed on a business.
    /// </summary>
    public Money Loan { get; }

    public Money MonthlyCashFlow { get; }

    public static BigDeal Property(
        string id,
        string title,
        string summary,
        Money downPayment,
        Money mortgage,
        Money monthlyCashFlow)
    {
        return Create(id, BigDealKind.Property, title, summary, downPayment, mortgage, monthlyCashFlow);
    }

    public static BigDeal Business(
        string id,
        string title,
        string summary,
        Money downPayment,
        Money liability,
        Money monthlyCashFlow)
    {
        return Create(id, BigDealKind.Business, title, summary, downPayment, liability, monthlyCashFlow);
    }

    public static BigDeal Create(
        string id,
        BigDealKind kind,
        string title,
        string summary,
        Money downPayment,
        Money loan,
        Money monthlyCashFlow)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(summary);

        List<string> errors = new();
        string slug = CardText.RequireId(errors, id);
        string trimmedTitle = CardText.RequireTitle(errors, title);
        string trimmedSummary = CardText.RequireSummary(errors, summary);

        if (!Enum.IsDefined(kind))
        {
            errors.Add("Deal kind is not a big deal.");
        }

        if (!downPayment.IsPositive)
        {
            errors.Add("Down payment must be greater than zero.");
        }

        if (loan.IsNegative)
        {
            errors.Add("Loan cannot be negative.");
        }

        if (errors.Count > 0)
        {
            throw new CardValidationException(errors);
        }

        return new BigDeal(slug, kind, trimmedTitle, trimmedSummary, downPayment, loan, monthlyCashFlow);
    }
}
