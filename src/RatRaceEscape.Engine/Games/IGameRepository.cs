namespace RatRaceEscape.Engine;

/// <summary>
/// Stores games for the host. <see cref="Find"/> returns null when the id is unknown.
/// </summary>
public interface IGameRepository
{
    void Add(Game game);

    Game? Find(Guid id);
}
