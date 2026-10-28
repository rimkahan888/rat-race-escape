namespace RatRaceEscape.Engine;

/// <summary>
/// Pays a bill from cash. When cash is short, assets are sold for what was
/// put into them. When the bill is still short, the player goes bankrupt and
/// the unpaid remainder is forgiven.
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

        if (player.Cash >= due)
        {
            return player.WithCash(player.Cash - due);
        }

        return Bankruptcy.Apply(player);
    }
}
