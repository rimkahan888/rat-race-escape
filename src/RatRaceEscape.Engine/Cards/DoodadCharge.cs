namespace RatRaceEscape.Engine;

/// <summary>
/// Pays an unplanned expense from cash on hand. The player is unchanged when
/// the cash cannot cover it.
/// </summary>
public static class DoodadCharge
{
    public static Player Pay(Player player, Doodad doodad)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(doodad);

        if (player.Cash < doodad.Cost)
        {
            throw new TurnException("Cash cannot cover the doodad.");
        }

        return player.WithCash(player.Cash - doodad.Cost);
    }
}
