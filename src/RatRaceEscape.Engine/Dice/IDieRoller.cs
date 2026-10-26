namespace RatRaceEscape.Engine;

/// <summary>
/// Rolls a single six-sided die. Callers inject this so a game can use a
/// seeded roller in tests and a random roller at the table.
/// </summary>
public interface IDieRoller
{
    /// <summary>
    /// Returns one face. Implementations must return a value from
    /// <see cref="Dice.MinimumFace"/> through <see cref="Dice.MaximumFace"/>.
    /// </summary>
    int Roll();
}
