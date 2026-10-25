namespace RatRaceEscape.Engine;

/// <summary>
/// Raised when a new game breaks one or more seating rules.
/// </summary>
public sealed class GameValidationException : Exception
{
    public GameValidationException(IReadOnlyList<string> errors)
        : base(Join(errors))
    {
        Errors = errors.ToArray();
    }

    public IReadOnlyList<string> Errors { get; }

    private static string Join(IReadOnlyList<string> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        if (errors.Count == 0)
        {
            throw new ArgumentException("At least one validation error is required.", nameof(errors));
        }

        return string.Join(' ', errors);
    }
}
