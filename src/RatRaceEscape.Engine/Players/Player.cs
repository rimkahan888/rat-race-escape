namespace RatRaceEscape.Engine;

/// <summary>
/// One person at the table. Cash on hand is the savings line of the balance
/// sheet, and it starts as the profession's savings.
/// </summary>
public sealed class Player
{
    private Player(
        string name,
        Profession profession,
        TokenColour tokenColour,
        BalanceSheet balanceSheet,
        int childCount)
    {
        Name = name;
        Profession = profession;
        TokenColour = tokenColour;
        BalanceSheet = balanceSheet;
        ChildCount = childCount;
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

        return new Player(trimmedName, profession, tokenColour, BalanceSheet.FromProfession(profession), 0);
    }

    public Player WithBalanceSheet(BalanceSheet balanceSheet)
    {
        ArgumentNullException.ThrowIfNull(balanceSheet);
        return new Player(Name, Profession, TokenColour, balanceSheet, ChildCount);
    }

    public Player WithChildCount(int childCount)
    {
        if (childCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(childCount), "Child count cannot be negative.");
        }

        return new Player(Name, Profession, TokenColour, BalanceSheet, childCount);
    }
}
