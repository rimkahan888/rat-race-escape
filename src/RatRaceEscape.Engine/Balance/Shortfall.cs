namespace RatRaceEscape.Engine;

/// <summary>
/// Pays a bill from cash. When cash is short, assets are sold for what was
/// put into them. When the bill is still short, the bank lends in increments
/// that leave monthly cash flow at zero or above, and only when that loan
/// covers the rest. When nothing covers the bill, the player goes bankrupt
/// and the unpaid remainder is forgiven.
/// </summary>
public static class Shortfall
{
    public static Player Cover(Player player, Money due)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (!due.IsPositive)
        {
            throw new TurnException("A shortfall needs an amount due.");
        }

        if (player.IsBankrupt)
        {
            return player;
        }

        if (player.Cash < due)
        {
            player = ForcedSale.Raise(player, due);
        }

        if (player.Cash < due)
        {
            player = BorrowIfItCovers(player, due);
        }

        if (player.Cash >= due)
        {
            return player.WithCash(player.Cash - due);
        }

        return Bankruptcy.Apply(player);
    }

    private static Player BorrowIfItCovers(Player player, Money due)
    {
        int allowed = CashFlow.BankIncrementsAllowed(player);
        if (allowed <= 0)
        {
            return player;
        }

        Money gap = due - player.Cash;
        long needed = (gap.Cents + BankLoan.Increment.Cents - 1) / BankLoan.Increment.Cents;
        if (needed < 1 || needed > allowed)
        {
            return player;
        }

        return BankLoan.Borrow(player, (int)needed);
    }
}
