namespace RatRaceEscape.Engine;

/// <summary>
/// A lot of shares. The dividend is the monthly amount each share pays.
/// </summary>
public sealed class StockHolding
{
    private StockHolding(string symbol, int shares, Money costPerShare, Money monthlyDividendPerShare)
    {
        Symbol = symbol;
        Shares = shares;
        CostPerShare = costPerShare;
        MonthlyDividendPerShare = monthlyDividendPerShare;
    }

    public string Symbol { get; }

    public int Shares { get; }

    public Money CostPerShare { get; }

    public Money MonthlyDividendPerShare { get; }

    public Money Cost => CostPerShare * Shares;

    public Money MonthlyIncome => MonthlyDividendPerShare * Shares;

    public static StockHolding Create(string symbol, int shares, Money costPerShare, Money monthlyDividendPerShare)
    {
        ArgumentNullException.ThrowIfNull(symbol);

        string trimmed = symbol.Trim().ToUpperInvariant();
        List<string> errors = new();

        if (!IsSymbol(trimmed))
        {
            errors.Add("Stock symbol must be 1 to 8 letters or digits and start with a letter.");
        }

        if (shares <= 0)
        {
            errors.Add("Stock shares must be greater than zero.");
        }

        if (costPerShare.IsNegative)
        {
            errors.Add("Stock cost per share cannot be negative.");
        }

        if (monthlyDividendPerShare.IsNegative)
        {
            errors.Add("Stock dividend per share cannot be negative.");
        }

        if (errors.Count > 0)
        {
            throw new BalanceSheetValidationException(errors);
        }

        return new StockHolding(trimmed, shares, costPerShare, monthlyDividendPerShare);
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
