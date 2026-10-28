namespace RatRaceEscape.Engine;

/// <summary>
/// Pays or collects the same amount from every player. A charge that one
/// player cannot cover is not taken from anyone.
/// </summary>
public static class MarketEvent
{
    public static Player[] Apply(IReadOnlyList<Player> players, Money cashDelta)
    {
        ArgumentNullException.ThrowIfNull(players);

        if (cashDelta.IsNegative)
        {
            Money due = cashDelta.Negate();
            foreach (Player player in players)
            {
                if (player.Cash < due)
                {
                    throw new TurnException("Cash cannot cover the market event.");
                }
            }
        }
        else if (!cashDelta.IsPositive)
        {
            throw new TurnException("An event must change everyone's cash.");
        }

        Player[] paid = new Player[players.Count];
        for (int index = 0; index < players.Count; index++)
        {
            Player player = players[index];
            paid[index] = player.WithCash(player.Cash + cashDelta);
        }

        return paid;
    }
}
