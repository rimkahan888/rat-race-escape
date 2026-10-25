namespace RatRaceEscape.Engine;

/// <summary>
/// Monthly salary, passive income, and the bills a player pays. Passive income
/// is the net amount from stocks, property, and businesses, so an asset that
/// costs more than it pays in reduces it and can make it negative.
/// </summary>
public sealed class IncomeStatement
{
    private IncomeStatement(
        Money salary,
        Money passiveIncome,
        Money taxes,
        Money mortgagePayment,
        Money carLoanPayment,
        Money creditCardPayment,
        Money otherExpenses,
        Money perChildExpense,
        int childCount,
        Money otherLiabilityPayments)
    {
        Salary = salary;
        PassiveIncome = passiveIncome;
        Taxes = taxes;
        MortgagePayment = mortgagePayment;
        CarLoanPayment = carLoanPayment;
        CreditCardPayment = creditCardPayment;
        OtherExpenses = otherExpenses;
        PerChildExpense = perChildExpense;
        ChildCount = childCount;
        OtherLiabilityPayments = otherLiabilityPayments;
        ChildExpenses = perChildExpense * childCount;
        LiabilityPayments = mortgagePayment + carLoanPayment + creditCardPayment + otherLiabilityPayments;
        TotalIncome = salary + passiveIncome;
        TotalExpenses = taxes + LiabilityPayments + otherExpenses + ChildExpenses;
    }

    public Money Salary { get; }

    public Money PassiveIncome { get; }

    public Money Taxes { get; }

    public Money MortgagePayment { get; }

    public Money CarLoanPayment { get; }

    public Money CreditCardPayment { get; }

    public Money OtherLiabilityPayments { get; }

    /// <summary>
    /// Home mortgage, car loan, credit card, and any other note, added together.
    /// </summary>
    public Money LiabilityPayments { get; }

    /// <summary>
    /// Living costs from the profession that are not taxes or debt payments.
    /// </summary>
    public Money OtherExpenses { get; }

    public Money PerChildExpense { get; }

    public int ChildCount { get; }

    public Money ChildExpenses { get; }

    public Money TotalIncome { get; }

    public Money TotalExpenses { get; }

    public static IncomeStatement For(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        Profession profession = player.Profession;
        return Create(
            profession.MonthlySalary,
            PassiveIncomeOf(player.BalanceSheet),
            profession.MonthlyTaxes,
            profession.Expenses.Mortgage,
            profession.Expenses.CarLoan,
            profession.Expenses.CreditCard,
            profession.Expenses.Other,
            profession.Expenses.PerChild,
            player.ChildCount,
            OtherLoanPayments(player.BalanceSheet));
    }

    public static IncomeStatement Create(
        Money salary,
        Money passiveIncome,
        Money taxes,
        Money mortgagePayment,
        Money carLoanPayment,
        Money creditCardPayment,
        Money otherExpenses,
        Money perChildExpense,
        int childCount,
        Money otherLiabilityPayments)
    {
        List<string> errors = new();

        if (salary.IsNegative)
        {
            errors.Add("Salary cannot be negative.");
        }

        if (taxes.IsNegative)
        {
            errors.Add("Taxes cannot be negative.");
        }

        if (mortgagePayment.IsNegative)
        {
            errors.Add("Mortgage payment cannot be negative.");
        }

        if (carLoanPayment.IsNegative)
        {
            errors.Add("Car loan payment cannot be negative.");
        }

        if (creditCardPayment.IsNegative)
        {
            errors.Add("Credit card payment cannot be negative.");
        }

        if (otherExpenses.IsNegative)
        {
            errors.Add("Other expenses cannot be negative.");
        }

        if (perChildExpense.IsNegative)
        {
            errors.Add("Per-child expense cannot be negative.");
        }

        if (childCount < 0)
        {
            errors.Add("Child count cannot be negative.");
        }

        if (otherLiabilityPayments.IsNegative)
        {
            errors.Add("Other liability payments cannot be negative.");
        }

        if (errors.Count > 0)
        {
            throw new IncomeStatementValidationException(errors);
        }

        return new IncomeStatement(
            salary,
            passiveIncome,
            taxes,
            mortgagePayment,
            carLoanPayment,
            creditCardPayment,
            otherExpenses,
            perChildExpense,
            childCount,
            otherLiabilityPayments);
    }

    private static Money PassiveIncomeOf(BalanceSheet sheet)
    {
        Money total = Money.Zero;

        foreach (StockHolding stock in sheet.Stocks)
        {
            total += stock.MonthlyIncome;
        }

        foreach (RealEstateHolding property in sheet.RealEstate)
        {
            total += property.MonthlyCashFlow;
        }

        foreach (BusinessHolding business in sheet.Businesses)
        {
            total += business.MonthlyCashFlow;
        }

        return total;
    }

    private static Money OtherLoanPayments(BalanceSheet sheet)
    {
        Money total = Money.Zero;

        foreach (LoanLiability loan in sheet.OtherLoans)
        {
            total += loan.MonthlyPayment;
        }

        return total;
    }
}
