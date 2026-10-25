namespace RatRaceEscape.Engine;

/// <summary>
/// What a player owns and owes. Savings is the cash on hand. Asset costs are
/// the cash put into stocks, property, and businesses. Liabilities are the
/// balances still owed, including each property mortgage and business loan.
/// </summary>
public sealed class BalanceSheet
{
    private BalanceSheet(
        Money savings,
        IReadOnlyList<StockHolding> stocks,
        IReadOnlyList<RealEstateHolding> realEstate,
        IReadOnlyList<BusinessHolding> businesses,
        Money mortgage,
        Money carLoan,
        Money creditCard,
        IReadOnlyList<LoanLiability> otherLoans)
    {
        Savings = savings;
        Stocks = stocks;
        RealEstate = realEstate;
        Businesses = businesses;
        Mortgage = mortgage;
        CarLoan = carLoan;
        CreditCard = creditCard;
        OtherLoans = otherLoans;

        Money stockCost = Money.Zero;
        foreach (StockHolding stock in stocks)
        {
            stockCost += stock.Cost;
        }

        Money propertyCost = Money.Zero;
        Money propertyMortgages = Money.Zero;
        foreach (RealEstateHolding property in realEstate)
        {
            propertyCost += property.Cost;
            propertyMortgages += property.Mortgage;
        }

        Money businessCost = Money.Zero;
        Money businessLiabilities = Money.Zero;
        foreach (BusinessHolding business in businesses)
        {
            businessCost += business.Cost;
            businessLiabilities += business.Liability;
        }

        Money otherBalances = Money.Zero;
        foreach (LoanLiability loan in otherLoans)
        {
            otherBalances += loan.Balance;
        }

        TotalAssets = savings + stockCost + propertyCost + businessCost;
        TotalLiabilities = mortgage + carLoan + creditCard + propertyMortgages + businessLiabilities + otherBalances;
    }

    public Money Savings { get; }

    public IReadOnlyList<StockHolding> Stocks { get; }

    public IReadOnlyList<RealEstateHolding> RealEstate { get; }

    public IReadOnlyList<BusinessHolding> Businesses { get; }

    public Money Mortgage { get; }

    public Money CarLoan { get; }

    public Money CreditCard { get; }

    public IReadOnlyList<LoanLiability> OtherLoans { get; }

    public Money TotalAssets { get; }

    public Money TotalLiabilities { get; }

    public static BalanceSheet FromProfession(Profession profession)
    {
        ArgumentNullException.ThrowIfNull(profession);

        return Create(
            profession.StartingSavings,
            [],
            [],
            [],
            profession.Liabilities.Mortgage,
            profession.Liabilities.CarLoan,
            profession.Liabilities.CreditCard,
            []);
    }

    public static BalanceSheet Create(
        Money savings,
        IReadOnlyList<StockHolding> stocks,
        IReadOnlyList<RealEstateHolding> realEstate,
        IReadOnlyList<BusinessHolding> businesses,
        Money mortgage,
        Money carLoan,
        Money creditCard,
        IReadOnlyList<LoanLiability> otherLoans)
    {
        ArgumentNullException.ThrowIfNull(stocks);
        ArgumentNullException.ThrowIfNull(realEstate);
        ArgumentNullException.ThrowIfNull(businesses);
        ArgumentNullException.ThrowIfNull(otherLoans);

        List<string> errors = new();

        if (savings.IsNegative)
        {
            errors.Add("Savings cannot be negative.");
        }

        if (mortgage.IsNegative)
        {
            errors.Add("Mortgage balance cannot be negative.");
        }

        if (carLoan.IsNegative)
        {
            errors.Add("Car loan balance cannot be negative.");
        }

        if (creditCard.IsNegative)
        {
            errors.Add("Credit card balance cannot be negative.");
        }

        RequireEntries(errors, stocks, "Stocks");
        RequireEntries(errors, realEstate, "Real estate");
        RequireEntries(errors, businesses, "Businesses");
        RequireEntries(errors, otherLoans, "Loans");
        RequireDistinctLoanNames(errors, otherLoans);

        if (errors.Count > 0)
        {
            throw new BalanceSheetValidationException(errors);
        }

        return new BalanceSheet(
            savings,
            stocks.ToArray(),
            realEstate.ToArray(),
            businesses.ToArray(),
            mortgage,
            carLoan,
            creditCard,
            otherLoans.ToArray());
    }

    private static void RequireEntries<T>(List<string> errors, IReadOnlyList<T> entries, string label)
        where T : class
    {
        foreach (T? entry in entries)
        {
            if (entry is null)
            {
                errors.Add($"{label} cannot contain an empty entry.");
                return;
            }
        }
    }

    private static void RequireDistinctLoanNames(List<string> errors, IReadOnlyList<LoanLiability> loans)
    {
        HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);
        foreach (LoanLiability? loan in loans)
        {
            if (loan is null)
            {
                continue;
            }

            if (!names.Add(loan.Name))
            {
                errors.Add($"Loan name '{loan.Name}' is used more than once.");
            }
        }
    }
}
