namespace RatRaceEscape.Engine;

/// <summary>
/// Shared wording rules for every card: a kebab-case id, a short title, and a summary.
/// </summary>
internal static class CardText
{
    public static string RequireId(List<string> errors, string id)
    {
        if (!IsSlug(id))
        {
            errors.Add("Id must be a lowercase kebab-case slug.");
        }

        return id;
    }

    public static string RequireTitle(List<string> errors, string title)
    {
        string trimmed = title.Trim();
        if (trimmed.Length is < 2 or > 60)
        {
            errors.Add("Title must be between 2 and 60 characters.");
        }

        if (trimmed.Any(char.IsControl))
        {
            errors.Add("Title cannot contain control characters.");
        }

        return trimmed;
    }

    public static string RequireSummary(List<string> errors, string summary)
    {
        string trimmed = summary.Trim();
        if (trimmed.Length is < 12 or > 240)
        {
            errors.Add("Summary must be between 12 and 240 characters.");
        }

        if (trimmed.Any(char.IsControl))
        {
            errors.Add("Summary cannot contain control characters.");
        }

        return trimmed;
    }

    private static bool IsSlug(string id)
    {
        if (id.Length is < 2 or > 40)
        {
            return false;
        }

        for (int index = 0; index < id.Length; index++)
        {
            char character = id[index];
            if (character is (>= 'a' and <= 'z') or (>= '0' and <= '9'))
            {
                continue;
            }

            if (character != '-' || index == 0 || index == id.Length - 1 || id[index - 1] == '-')
            {
                return false;
            }
        }

        return true;
    }
}
