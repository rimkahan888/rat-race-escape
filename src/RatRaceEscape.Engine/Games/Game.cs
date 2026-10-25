namespace RatRaceEscape.Engine;

/// <summary>
/// One table. Players are seated from the professions they chose, the first
/// player is on turn, and the phase starts in the rat race.
/// </summary>
public sealed class Game
{
    public const int MinimumPlayers = 1;

    public const int MaximumPlayers = 6;

    private Game(Guid id, IReadOnlyList<Player> players, int currentTurnIndex, GamePhase phase)
    {
        Id = id;
        Players = players;
        CurrentTurnIndex = currentTurnIndex;
        Phase = phase;
    }

    public Guid Id { get; }

    public IReadOnlyList<Player> Players { get; }

    public int CurrentTurnIndex { get; }

    public GamePhase Phase { get; }

    public Player CurrentPlayer => Players[CurrentTurnIndex];

    public static Game Create(IReadOnlyList<PlayerSetup> setups) => Create(Guid.NewGuid(), setups);

    public static Game Create(Guid id, IReadOnlyList<PlayerSetup> setups)
    {
        ArgumentNullException.ThrowIfNull(setups);

        List<string> errors = new();

        if (id == Guid.Empty)
        {
            errors.Add("Game id cannot be empty.");
        }

        if (setups.Count < MinimumPlayers)
        {
            errors.Add("A game needs at least one player.");
        }
        else if (setups.Count > MaximumPlayers)
        {
            errors.Add("A game cannot have more than 6 players.");
        }

        List<Player> players = new(setups.Count);
        HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);
        HashSet<TokenColour> colours = new();
        Profession standIn = ProfessionCatalog.All[0];

        foreach (PlayerSetup setup in setups)
        {
            Profession? profession = ResolveProfession(setup.ProfessionId, errors);

            if (setup.Name is null)
            {
                errors.Add("Name is required.");
                continue;
            }

            Player player;
            try
            {
                player = Player.Create(setup.Name, profession ?? standIn, setup.TokenColour);
            }
            catch (PlayerValidationException exception)
            {
                foreach (string error in exception.Errors)
                {
                    errors.Add(error);
                }

                continue;
            }

            bool duplicate = false;
            if (!names.Add(player.Name))
            {
                errors.Add($"Player name '{player.Name}' is used more than once.");
                duplicate = true;
            }

            if (!colours.Add(player.TokenColour))
            {
                errors.Add($"Token colour {player.TokenColour} is used more than once.");
                duplicate = true;
            }

            if (profession is not null && !duplicate)
            {
                players.Add(player);
            }
        }

        if (errors.Count > 0)
        {
            throw new GameValidationException(errors);
        }

        return new Game(id, players.ToArray(), currentTurnIndex: 0, GamePhase.RatRace);
    }

    private static Profession? ResolveProfession(string? professionId, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(professionId))
        {
            errors.Add("Profession id is required.");
            return null;
        }

        foreach (Profession profession in ProfessionCatalog.All)
        {
            if (profession.Id == professionId)
            {
                return profession;
            }
        }

        errors.Add($"No profession with id '{professionId}'.");
        return null;
    }
}
