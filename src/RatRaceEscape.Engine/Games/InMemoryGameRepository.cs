namespace RatRaceEscape.Engine;

/// <summary>
/// Keeps games in the process. Adds are safe when requests arrive together,
/// and a second add of the same id leaves the stored game in place.
/// Save replaces the stored game.
/// </summary>
public sealed class InMemoryGameRepository : IGameRepository
{
    private readonly Lock _gate = new();
    private readonly Dictionary<Guid, Game> _games = [];

    public void Add(Game game)
    {
        ArgumentNullException.ThrowIfNull(game);

        lock (_gate)
        {
            if (!_games.TryAdd(game.Id, game))
            {
                throw new InvalidOperationException($"A game with id '{game.Id}' is already stored.");
            }
        }
    }

    public Game? Find(Guid id)
    {
        lock (_gate)
        {
            return _games.TryGetValue(id, out Game? game) ? game : null;
        }
    }

    public void Save(Game game)
    {
        ArgumentNullException.ThrowIfNull(game);

        lock (_gate)
        {
            if (!_games.ContainsKey(game.Id))
            {
                throw new InvalidOperationException($"No game with id '{game.Id}' is stored.");
            }

            _games[game.Id] = game;
        }
    }
}
