namespace RatRaceEscape.Engine;

/// <summary>
/// One person at the table. Cash on hand is the savings line of the balance
/// sheet, and it starts as the profession's savings. The token starts on the
/// opening space of the rat race.
/// </summary>
public sealed class Player
{
    private Player(
        string name,
        Profession profession,
        TokenColour tokenColour,
        BalanceSheet balanceSheet,
        int childCount,
        int boardIndex,
        int extraDieTurnsRemaining,
        int skipTurnsRemaining,
        bool isBankrupt)
    {
        Name = name;
        Profession = profession;
        TokenColour = tokenColour;
        BalanceSheet = balanceSheet;
        ChildCount = childCount;
        BoardIndex = boardIndex;
        ExtraDieTurnsRemaining = extraDieTurnsRemaining;
        SkipTurnsRemaining = skipTurnsRemaining;
        IsBankrupt = isBankrupt;
    }

    public string Name { get; }

    public Profession Profession { get; }

    public TokenColour TokenColour { get; }

    public BalanceSheet BalanceSheet { get; }

    /// <summary>
    /// Cash on hand. This is the savings line of the balance sheet.
    /// </summary>
    public Money Cash => BalanceSheet.Savings;

    /// <summary>
    /// Children at the table. A new player has none. The income statement
    /// multiplies the profession's per-child expense by this count.
    /// </summary>
    public int ChildCount { get; }

    /// <summary>
    /// The most children a new arrival will add.
    /// </summary>
    public const int MaximumChildren = 3;

    /// <summary>
    /// Square the token is on. A new player stands on the opening space.
    /// </summary>
    public int BoardIndex { get; }

    /// <summary>
    /// Turns left on which this player may roll one or two dice. Charity adds
    /// three, starting on the next turn. A new player has none.
    /// </summary>
    public int ExtraDieTurnsRemaining { get; }

    /// <summary>
    /// Turns this player will miss after the current one. Hours cut adds two.
    /// A new player has none.
    /// </summary>
    public int SkipTurnsRemaining { get; }

    /// <summary>
    /// True after a shortfall survives a forced sale. A bankrupt player is
    /// out of the game. A new player is not bankrupt.
    /// </summary>
    public bool IsBankrupt { get; }

    public static Player Create(string name, Profession profession, TokenColour tokenColour)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(profession);

        string trimmedName = name.Trim();
        List<string> errors = new();

        if (trimmedName.Length is < 2 or > 40)
        {
            errors.Add("Name must be between 2 and 40 characters.");
        }

        if (trimmedName.Any(char.IsControl))
        {
            errors.Add("Name cannot contain control characters.");
        }

        if (!Enum.IsDefined(tokenColour))
        {
            errors.Add("Token colour is not a playing colour.");
        }

        if (errors.Count > 0)
        {
            throw new PlayerValidationException(errors);
        }

        return new Player(
            trimmedName,
            profession,
            tokenColour,
            BalanceSheet.FromProfession(profession),
            childCount: 0,
            boardIndex: 0,
            extraDieTurnsRemaining: 0,
            skipTurnsRemaining: 0,
            isBankrupt: false);
    }

    public Player WithBalanceSheet(BalanceSheet balanceSheet)
    {
        ArgumentNullException.ThrowIfNull(balanceSheet);
        return new Player(
            Name,
            Profession,
            TokenColour,
            balanceSheet,
            ChildCount,
            BoardIndex,
            ExtraDieTurnsRemaining,
            SkipTurnsRemaining,
            IsBankrupt);
    }

    public Player WithChildCount(int childCount)
    {
        if (childCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(childCount), "Child count cannot be negative.");
        }

        return new Player(
            Name,
            Profession,
            TokenColour,
            BalanceSheet,
            childCount,
            BoardIndex,
            ExtraDieTurnsRemaining,
            SkipTurnsRemaining,
            IsBankrupt);
    }

    public Player WithBoardIndex(int boardIndex)
    {
        if (boardIndex < 0 || boardIndex >= RatRaceBoard.SpaceCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(boardIndex),
                $"Board index must be from 0 to {RatRaceBoard.SpaceCount - 1}.");
        }

        return new Player(
            Name,
            Profession,
            TokenColour,
            BalanceSheet,
            ChildCount,
            boardIndex,
            ExtraDieTurnsRemaining,
            SkipTurnsRemaining,
            IsBankrupt);
    }

    public Player WithExtraDieTurns(int extraDieTurnsRemaining)
    {
        if (extraDieTurnsRemaining < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(extraDieTurnsRemaining),
                "Extra die turns cannot be negative.");
        }

        return new Player(
            Name,
            Profession,
            TokenColour,
            BalanceSheet,
            ChildCount,
            BoardIndex,
            extraDieTurnsRemaining,
            SkipTurnsRemaining,
            IsBankrupt);
    }

    public Player WithSkipTurnsRemaining(int skipTurnsRemaining)
    {
        if (skipTurnsRemaining < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(skipTurnsRemaining), "Skipped turns cannot be negative.");
        }

        return new Player(
            Name,
            Profession,
            TokenColour,
            BalanceSheet,
            ChildCount,
            BoardIndex,
            ExtraDieTurnsRemaining,
            skipTurnsRemaining,
            IsBankrupt);
    }

    public Player WithBankrupt(bool isBankrupt)
    {
        return new Player(
            Name,
            Profession,
            TokenColour,
            BalanceSheet,
            ChildCount,
            BoardIndex,
            ExtraDieTurnsRemaining,
            SkipTurnsRemaining,
            isBankrupt);
    }

    public Player WithCash(Money cash)
    {
        return WithBalanceSheet(BalanceSheet.WithSavings(cash));
    }
}
