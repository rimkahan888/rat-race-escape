namespace RatRaceEscape.Engine;

/// <summary>
/// One person at the table. Cash starts as the profession's savings.
/// </summary>
public sealed class Player
{
    private Player(string name, Profession profession, Money cash, TokenColour tokenColour)
    {
        Name = name;
        Profession = profession;
        Cash = cash;
        TokenColour = tokenColour;
    }

    public string Name { get; }

    public Profession Profession { get; }

    /// <summary>
    /// Cash on hand, in cents, taken from the profession's starting savings.
    /// </summary>
    public Money Cash { get; }

    public TokenColour TokenColour { get; }

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

        return new Player(trimmedName, profession, profession.StartingSavings, tokenColour);
    }
}
