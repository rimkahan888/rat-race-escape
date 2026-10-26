namespace RatRaceEscape.Engine;

/// <summary>
/// Raised when a turn action is not the one the table is waiting for.
/// </summary>
public sealed class TurnException : Exception
{
    public TurnException(string error)
        : this([error])
    {
    }

    public TurnException(IReadOnlyList<string> errors)
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
