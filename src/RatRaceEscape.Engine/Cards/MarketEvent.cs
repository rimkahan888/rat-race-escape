namespace RatRaceEscape.Engine;

/// <summary>
/// Pays or collects the same amount from every player who is still in the
/// game. A player who cannot cover a charge sells assets and, if still short,
/// goes bankrupt. The other players still pay.
/// </summary>
public static class MarketEvent
{
    public static Player[] Apply(IReadOnlyList<Player> players, Money cashDelta)
    {
        ArgumentNullException.ThrowIfNull(players);

        if (cashDelta.IsZero)
        {
            throw new TurnException("An event must change everyone's cash.");
        }

        Player[] paid = new Player[players.Count];
        for (int index = 0; index < players.Count; index++)
        {
            Player player = players[index];
            if (player.IsBankrupt)
            {
                paid[index] = player;
                continue;
            }

            paid[index] = cashDelta.IsNegative
                ? Shortfall.Cover(player, cashDelta.Negate())
                : player.WithCash(player.Cash + cashDelta);
        }

        return paid;
    }
}
