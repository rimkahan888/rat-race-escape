namespace RatRaceEscape.Engine;

/// <summary>
/// One square on the rat-race loop. The title and summary are the square's
/// own wording. Cards drawn on a deal, market, or doodad square come later.
/// </summary>
public sealed class BoardSpace
{
    private BoardSpace(int index, SpaceKind kind, string title, string summary)
    {
        Index = index;
        Kind = kind;
        Title = title;
        Summary = summary;
    }

    public int Index { get; }

    public SpaceKind Kind { get; }

    public string Title { get; }

    public string Summary { get; }

    public static BoardSpace Create(int index, SpaceKind kind, string title, string summary)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(summary);

        string trimmedTitle = title.Trim();
        string trimmedSummary = summary.Trim();
        List<string> errors = new();

        if (index < 0 || index >= RatRaceBoard.SpaceCount)
        {
            errors.Add($"Index must be from 0 to {RatRaceBoard.SpaceCount - 1}.");
        }

        if (!Enum.IsDefined(kind))
        {
            errors.Add("Space kind is not a board space.");
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

        return new BoardSpace(index, kind, trimmedTitle, trimmedSummary);
    }
}
