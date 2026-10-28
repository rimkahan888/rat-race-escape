namespace RatRaceEscape.Engine;

/// <summary>
/// One square on the fast track. The title and summary are the square's own
/// wording.
/// </summary>
public sealed class FastTrackSpace
{
    private FastTrackSpace(int index, FastTrackKind kind, string title, string summary)
    {
        Index = index;
        Kind = kind;
        Title = title;
        Summary = summary;
    }

    public int Index { get; }

    public FastTrackKind Kind { get; }

    public string Title { get; }

    public string Summary { get; }

    public static FastTrackSpace Create(int index, FastTrackKind kind, string title, string summary)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(summary);

        string trimmedTitle = title.Trim();
        string trimmedSummary = summary.Trim();
        List<string> errors = new();

        if (index < 0 || index >= FastTrackBoard.SpaceCount)
        {
            errors.Add($"Index must be from 0 to {FastTrackBoard.SpaceCount - 1}.");
        }

        if (!Enum.IsDefined(kind))
        {
            errors.Add("Space kind is not a fast-track space.");
        }

        if (trimmedTitle.Length is < 2 or > 40)
        {
            errors.Add("Title must be between 2 and 40 characters.");
        }

        if (trimmedTitle.Any(char.IsControl))
        {
            errors.Add("Title cannot contain control characters.");
        }

        if (trimmedSummary.Length is < 12 or > 160)
        {
            errors.Add("Summary must be between 12 and 160 characters.");
        }

        if (trimmedSummary.Any(char.IsControl))
        {
            errors.Add("Summary cannot contain control characters.");
        }

        if (errors.Count > 0)
        {
            throw new BoardSpaceValidationException(errors);
        }

        return new FastTrackSpace(index, kind, trimmedTitle, trimmedSummary);
    }
}
