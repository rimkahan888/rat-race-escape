namespace RatRaceEscape.Engine;

/// <summary>
/// A new arrival adds one child. Expenses grow by the profession's per-child
/// amount. The household stops at <see cref="Player.MaximumChildren"/>.
/// </summary>
public static class Baby
{
    public static Player Arrive(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (player.ChildCount >= Player.MaximumChildren)
        {
            return player;
        }

        return player.WithChildCount(player.ChildCount + 1);
    }
}
