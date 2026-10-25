namespace RatRaceEscape.Engine;

/// <summary>
/// Monthly cash flow, and the comparison between passive income and total
/// expenses that a later escape check will use. Equality does not pass that
/// check: passive income has to be strictly greater than the bills.
/// </summary>
public sealed class CashFlow
{
    private CashFlow(Money totalIncome, Money totalExpenses, Money passiveIncome)
    {
        TotalIncome = totalIncome;
        TotalExpenses = totalExpenses;
        PassiveIncome = passiveIncome;
        Monthly = totalIncome - totalExpenses;
        PassiveSurplus = passiveIncome - totalExpenses;
    }

    public Money TotalIncome { get; }

    public Money TotalExpenses { get; }

    public Money PassiveIncome { get; }

    /// <summary>
    /// Total income minus total expenses. Negative when the bills are larger.
    /// </summary>
    public Money Monthly { get; }

    /// <summary>
    /// Passive income minus total expenses.
    /// </summary>
    public Money PassiveSurplus { get; }

    public bool PassiveIncomeExceedsExpenses => PassiveIncome > TotalExpenses;

    public static CashFlow From(IncomeStatement statement)
    {
        ArgumentNullException.ThrowIfNull(statement);
        return new CashFlow(statement.TotalIncome, statement.TotalExpenses, statement.PassiveIncome);
    }

    public static CashFlow For(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        return From(IncomeStatement.For(player));
    }
}
