namespace RatRaceEscape.Engine;

/// <summary>
/// Closes a player out when a bill survives every way to pay it. Cash, assets,
/// and every liability are cleared, and the income statement goes to zero so
/// the old salary and bills are not still on the sheet. The name and
/// profession stay. The player is out of the game.
/// </summary>
public static class Bankruptcy
{
    public static Player Apply(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        return player
            .WithBalanceSheet(BalanceSheet.Create(
                Money.Zero,
                [],
                [],
                [],
                Money.Zero,
                Money.Zero,
                Money.Zero,
                []))
            .WithBankrupt(true);
    }
}
