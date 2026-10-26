namespace RatRaceEscape.Engine;

/// <summary>
/// Paying one tenth of total income, with any leftover cent kept by the player,
/// buys three turns of choosing one or two dice. The turn already in progress
/// is not one of them.
/// </summary>
public static class Charity
{
    public const int IncomeShareDenominator = 10;

    public const int ExtraDieTurns = 3;

    public static Money Donation(Money totalIncome)
    {
        if (!totalIncome.IsPositive)
        {
            return Money.Zero;
        }

        return Money.FromCents(totalIncome.Cents / IncomeShareDenominator);
    }

    public static Player Donate(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        Money donation = Donation(IncomeStatement.For(player).TotalIncome);
        if (player.Cash < donation)
        {
            throw new TurnException("Cash cannot cover the charity donation.");
        }

        return player
            .WithCash(player.Cash - donation)
            .WithExtraDieTurns(player.ExtraDieTurnsRemaining + ExtraDieTurns);
    }
}
