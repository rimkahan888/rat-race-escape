namespace RatRaceEscape.Engine;

/// <summary>
/// Raised when an income statement breaks a bookkeeping rule.
/// </summary>
public sealed class IncomeStatementValidationException : Exception
{
    public IncomeStatementValidationException(IReadOnlyList<string> errors)
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
