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
        Money raw = totalIncome - totalExpenses;
        Monthly = raw.IsNegative ? Money.Zero : raw;
        PassiveSurplus = passiveIncome - totalExpenses;
    }

    public Money TotalIncome { get; }

    public Money TotalExpenses { get; }

    public Money PassiveIncome { get; }

    /// <summary>
    /// Total income minus total expenses, never below zero. A month that
    /// does not cover the bills pays nothing instead of taking cash.
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

    /// <summary>
    /// Income minus expenses before the zero floor. Choices use this so a
    /// loan or purchase cannot hide a shortfall behind the floor.
    /// </summary>
    public static Money RawMonthly(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        IncomeStatement statement = IncomeStatement.For(player);
        return statement.TotalIncome - statement.TotalExpenses;
    }

    /// <summary>
    /// Refuses a choice whose resulting sheet would take monthly cash flow
    /// below zero. The player is unchanged because the caller has not stored it.
    /// </summary>
    public static void RejectIfNegative(Player player, string reason)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (RawMonthly(player).IsNegative)
        {
            throw new TurnException(reason);
        }
    }

    /// <summary>
    /// How many bank-loan increments this player can take without monthly
    /// cash flow going below zero. Zero when there is no room.
    /// </summary>
    public static int BankIncrementsAllowed(Player player)
    {
        Money raw = RawMonthly(player);
        if (!raw.IsPositive)
        {
            return 0;
        }

        long increments = raw.Cents / BankLoan.MonthlyPaymentPerIncrement.Cents;
        if (increments > int.MaxValue)
        {
            return int.MaxValue;
        }

        return (int)increments;
    }
}
