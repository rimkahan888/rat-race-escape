namespace RatRaceEscape.Engine;

/// <summary>
/// One unplanned expense. The cost is what the player pays when the card is drawn.
/// </summary>
public sealed class Doodad
{
    private Doodad(string id, string title, string summary, Money cost)
    {
        Id = id;
        Title = title;
        Summary = summary;
        Cost = cost;
    }

    public string Id { get; }

    public string Title { get; }

    public string Summary { get; }

    public Money Cost { get; }

    public static Doodad Create(string id, string title, string summary, Money cost)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(summary);

        List<string> errors = new();
        string slug = CardText.RequireId(errors, id);
        string trimmedTitle = CardText.RequireTitle(errors, title);
        string trimmedSummary = CardText.RequireSummary(errors, summary);

        if (!cost.IsPositive)
        {
            errors.Add("Doodad cost must be greater than zero.");
        }

        if (errors.Count > 0)
        {
            throw new CardValidationException(errors);
        }

        return new Doodad(slug, trimmedTitle, trimmedSummary, cost);
    }
}
