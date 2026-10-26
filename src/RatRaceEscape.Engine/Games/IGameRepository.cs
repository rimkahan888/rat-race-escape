namespace RatRaceEscape.Engine;

/// <summary>
/// Stores games for the host. <see cref="Find"/> returns null when the id is unknown.
/// <see cref="Save"/> replaces a game that is already stored.
/// </summary>
public interface IGameRepository
{
    void Add(Game game);

    Game? Find(Guid id);

    void Save(Game game);
}
