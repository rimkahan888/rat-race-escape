namespace RatRaceEscape.Engine;

/// <summary>
/// One card in the small-deals deck. A stock names a symbol, a price per
/// share, and the monthly dividend each share pays. A property names the
/// down payment, the mortgage, and the monthly cash flow left after that note.
/// Cash flow may be negative when the note is larger than the rent.
/// </summary>
public sealed class SmallDeal
{
    private SmallDeal(
        string id,
        SmallDealKind kind,
        string title,
        string summary,
        string? symbol,
        Money pricePerShare,
        Money dividendPerShare,
        Money downPayment,
        Money mortgage,
        Money monthlyCashFlow)
    {
        Id = id;
        Kind = kind;
        Title = title;
        Summary = summary;
        Symbol = symbol;
        PricePerShare = pricePerShare;
        DividendPerShare = dividendPerShare;
        DownPayment = downPayment;
        Mortgage = mortgage;
        MonthlyCashFlow = monthlyCashFlow;
    }

    public string Id { get; }

    public SmallDealKind Kind { get; }

    public string Title { get; }

    public string Summary { get; }

    public string? Symbol { get; }

    public Money PricePerShare { get; }

    public Money DividendPerShare { get; }

    public Money DownPayment { get; }

    public Money Mortgage { get; }

    public Money MonthlyCashFlow { get; }

    public static SmallDeal Stock(
        string id,
        string title,
        string summary,
        string symbol,
        Money pricePerShare,
        Money dividendPerShare)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        return Create(
            id,
            SmallDealKind.Stock,
            title,
            summary,
            symbol,
            pricePerShare,
            dividendPerShare,
            Money.Zero,
            Money.Zero,
            Money.Zero);
    }

    public static SmallDeal Property(
        string id,
        string title,
        string summary,
        Money downPayment,
        Money mortgage,
        Money monthlyCashFlow)
    {
        return Create(
            id,
            SmallDealKind.Property,
            title,
            summary,
            symbol: null,
            Money.Zero,
            Money.Zero,
            downPayment,
            mortgage,
            monthlyCashFlow);
    }

    public static SmallDeal Create(
        string id,
        SmallDealKind kind,
        string title,
        string summary,
        string? symbol,
        Money pricePerShare,
        Money dividendPerShare,
        Money downPayment,
        Money mortgage,
        Money monthlyCashFlow)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(summary);

        List<string> errors = new();
        string slug = CardText.RequireId(errors, id);
        string trimmedTitle = CardText.RequireTitle(errors, title);
        string trimmedSummary = CardText.RequireSummary(errors, summary);
        string? trimmedSymbol = string.IsNullOrWhiteSpace(symbol) ? null : symbol.Trim().ToUpperInvariant();

        if (!Enum.IsDefined(kind))
        {
            errors.Add("Deal kind is not a small deal.");
        }
        else if (kind == SmallDealKind.Stock)
        {
            RequireStock(errors, trimmedSymbol, pricePerShare, dividendPerShare, downPayment, mortgage, monthlyCashFlow);
        }
        else
        {
            RequireProperty(errors, trimmedSymbol, pricePerShare, dividendPerShare, downPayment, mortgage);
        }

        if (errors.Count > 0)
        {
            throw new CardValidationException(errors);
        }

        return new SmallDeal(
            slug,
            kind,
            trimmedTitle,
            trimmedSummary,
            trimmedSymbol,
            pricePerShare,
            dividendPerShare,
            downPayment,
            mortgage,
            monthlyCashFlow);
    }

    private static void RequireStock(
        List<string> errors,
        string? symbol,
        Money pricePerShare,
        Money dividendPerShare,
        Money downPayment,
        Money mortgage,
        Money monthlyCashFlow)
    {
        if (symbol is null || !IsSymbol(symbol))
        {
            errors.Add("Stock symbol must be 1 to 8 letters or digits and start with a letter.");
        }

        if (!pricePerShare.IsPositive)
        {
            errors.Add("Share price must be greater than zero.");
        }

        if (dividendPerShare.IsNegative)
        {
            errors.Add("Dividend per share cannot be negative.");
        }

        if (!downPayment.IsZero)
        {
            errors.Add("A stock deal does not have a down payment.");
        }

        if (!mortgage.IsZero)
        {
            errors.Add("A stock deal does not have a mortgage.");
        }

        if (!monthlyCashFlow.IsZero)
        {
            errors.Add("A stock deal does not have property cash flow.");
        }
    }

    private static void RequireProperty(
        List<string> errors,
        string? symbol,
        Money pricePerShare,
        Money dividendPerShare,
        Money downPayment,
        Money mortgage)
    {
        if (symbol is not null)
        {
            errors.Add("A property deal does not have a stock symbol.");
        }

        if (!pricePerShare.IsZero)
        {
            errors.Add("A property deal does not have a share price.");
        }

        if (!dividendPerShare.IsZero)
        {
            errors.Add("A property deal does not have a dividend.");
        }

        if (!downPayment.IsPositive)
        {
            errors.Add("Down payment must be greater than zero.");
        }

        if (mortgage.IsNegative)
        {
            errors.Add("Mortgage cannot be negative.");
        }
    }

    private static bool IsSymbol(string symbol)
    {
        if (symbol.Length is < 1 or > 8)
        {
            return false;
        }

        if (!char.IsAsciiLetter(symbol[0]))
        {
            return false;
        }

        for (int index = 1; index < symbol.Length; index++)
        {
            if (!char.IsAsciiLetterOrDigit(symbol[index]))
            {
                return false;
            }
        }

        return true;
    }
}
