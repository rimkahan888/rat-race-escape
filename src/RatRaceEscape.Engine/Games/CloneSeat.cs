namespace RatRaceEscape.Engine;

/// <summary>
/// Seats a second player with the same profession and starting sheet as the
/// human, and a different token.
/// </summary>
public static class CloneSeat
{
    public static PlayerSetup Beside(PlayerSetup human)
    {
        string name = human.Name.Trim();
        string cloneName = name.Length <= 32 ? $"{name}'s clone" : "Clone";
        return new PlayerSetup(cloneName, human.ProfessionId, NextColour(human.TokenColour));
    }

    public static TokenColour NextColour(TokenColour taken)
    {
        foreach (TokenColour colour in Enum.GetValues<TokenColour>())
        {
            if (colour != taken)
            {
                return colour;
            }
        }

        throw new InvalidOperationException("Every token colour is taken.");
    }
}
