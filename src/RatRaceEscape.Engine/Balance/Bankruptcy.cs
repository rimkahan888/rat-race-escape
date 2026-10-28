namespace RatRaceEscape.Engine;

/// <summary>
/// Closes a player out when a bill survives a forced sale. Cash goes to zero
/// and the unpaid remainder is forgiven. Assets and bank loans are wiped.
/// The profession's mortgage, car loan, and credit card stay. The player is
/// out of the game.
/// </summary>
public static class Bankruptcy
{
    public static Player Apply(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        BalanceSheet sheet = player.BalanceSheet;
        return player
            .WithBalanceSheet(BalanceSheet.Create(
                Money.Zero,
                [],
                [],
                [],
                sheet.Mortgage,
                sheet.CarLoan,
                sheet.CreditCard,
                []))
            .WithBankrupt(true);
    }
}
