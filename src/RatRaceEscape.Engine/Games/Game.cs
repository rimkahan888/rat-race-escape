namespace RatRaceEscape.Engine;

/// <summary>
/// One table. Players are seated from the professions they chose, the first
/// player is on turn, and the phase starts in the rat race. The turn starts
/// by waiting for that player to roll.
/// </summary>
public sealed partial class Game
{
    public const int MinimumPlayers = 1;

    public const int MaximumPlayers = 6;

    private Game(
        Guid id,
        IReadOnlyList<Player> players,
        int currentTurnIndex,
        GamePhase phase,
        TurnPhase turnPhase,
        TurnRoll? lastRoll,
        DeckHook? pendingDeck,
        CardPiles piles,
        DrawnCard? drawn,
        int? winnerIndex = null,
        int? seed = null,
        int diceConsumed = 0)
    {
        ArgumentNullException.ThrowIfNull(piles);
        Id = id;
        Players = players;
        CurrentTurnIndex = currentTurnIndex;
        Phase = phase;
        TurnPhase = turnPhase;
        LastRoll = lastRoll;
        PendingDeck = pendingDeck;
        Piles = piles;
        Drawn = drawn;
        WinnerIndex = winnerIndex;
        Seed = seed;
        DiceConsumed = diceConsumed;
    }

    public Guid Id { get; }

    public IReadOnlyList<Player> Players { get; }

    public int CurrentTurnIndex { get; }

    public GamePhase Phase { get; }

    /// <summary>
    /// The player who bought a dream on the fast track, or null while the
    /// table is still playing.
    /// </summary>
    public int? WinnerIndex { get; }

    /// <summary>
    /// Where this turn is. A new game is waiting for the first player to roll.
    /// </summary>
    public TurnPhase TurnPhase { get; }

    /// <summary>
    /// The roll that opened the current turn, or null while the table waits for one.
    /// </summary>
    public TurnRoll? LastRoll { get; }

    /// <summary>
    /// Deck the drawn card came from, or null when the square has no card.
    /// Set when the space is resolved, and cleared when the turn ends.
    /// </summary>
    public DeckHook? PendingDeck { get; }

    /// <summary>
    /// The four decks. A new game deals each catalog from the top unless a
    /// shuffle was passed to <see cref="Create(Guid, IReadOnlyList{PlayerSetup}, IShuffle)"/>.
    /// </summary>
    public CardPiles Piles { get; }

    /// <summary>
    /// The card drawn for this space, or null before the draw and after the turn ends.
    /// </summary>
    public DrawnCard? Drawn { get; }

    /// <summary>
    /// Optional seed for the dice and the opening shuffle. Null uses the
    /// roller and shuffle passed in by the caller.
    /// </summary>
    public int? Seed { get; }

    /// <summary>
    /// How many die faces this table has already taken from <see cref="Seed"/>.
    /// </summary>
    public int DiceConsumed { get; }

    public Player CurrentPlayer => Players[CurrentTurnIndex];

    public static Game Create(IReadOnlyList<PlayerSetup> setups) => Create(Guid.NewGuid(), setups);

    public static Game Create(IReadOnlyList<PlayerSetup> setups, IShuffle shuffle) =>
        Create(Guid.NewGuid(), setups, shuffle);

    public static Game Create(Guid id, IReadOnlyList<PlayerSetup> setups) =>
        Create(id, setups, new IdentityShuffle());

    public static Game Create(IReadOnlyList<PlayerSetup> setups, int seed) =>
        Create(Guid.NewGuid(), setups, new RandomShuffle(seed), seed);

    public static Game Create(Guid id, IReadOnlyList<PlayerSetup> setups, IShuffle shuffle)
    {
        ArgumentNullException.ThrowIfNull(shuffle);
        return Create(id, setups, CardPiles.Create(shuffle));
    }

    public static Game Create(Guid id, IReadOnlyList<PlayerSetup> setups, IShuffle shuffle, int seed)
    {
        ArgumentNullException.ThrowIfNull(shuffle);
        return Create(id, setups, CardPiles.Create(shuffle), seed);
    }

    public static Game Create(Guid id, IReadOnlyList<PlayerSetup> setups, CardPiles piles) =>
        Create(id, setups, piles, seed: null);

    public static Game Create(Guid id, IReadOnlyList<PlayerSetup> setups, CardPiles piles, int? seed)
    {
        ArgumentNullException.ThrowIfNull(piles);
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

        return new Game(
            id,
            players.ToArray(),
            currentTurnIndex: 0,
            GamePhase.RatRace,
            TurnPhase.AwaitingRoll,
            lastRoll: null,
            pendingDeck: null,
            piles,
            drawn: null,
            winnerIndex: null,
            seed,
            diceConsumed: 0);
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

    private void RequireAction()
    {
        if (Phase is GamePhase.Over or GamePhase.Won)
        {
            throw new TurnException("The game is already over.");
        }

        if (CurrentPlayer.IsBankrupt)
        {
            throw new TurnException("A bankrupt player cannot take that action.");
        }
    }
}
