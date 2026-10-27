namespace RatRaceEscape.Engine;

/// <summary>
/// One card in the market deck. An offer names the asset a buyer wants and
/// the price, per share or for the whole thing. An event moves every player's
/// cash by the same amount, positive when the table collects and negative
/// when the table pays.
/// </summary>
public sealed class MarketCard
{
    private MarketCard(
        string id,
        MarketKind kind,
        string title,
        string summary,
        string? assetName,
        bool perShare,
        Money price,
        Money cashDelta)
    {
        Id = id;
        Kind = kind;
        Title = title;
        Summary = summary;
        AssetName = assetName;
        PerShare = perShare;
        Price = price;
        CashDelta = cashDelta;
    }

    public string Id { get; }

    public MarketKind Kind { get; }

    public string Title { get; }

    public string Summary { get; }

    /// <summary>
    /// The stock symbol, or the property or business name, a buyer wants.
    /// Null on an event.
    /// </summary>
    public string? AssetName { get; }

    /// <summary>
    /// True when <see cref="Price"/> is a price per share. False when the
    /// offer is for the whole asset, and false on an event.
    /// </summary>
    public bool PerShare { get; }

    public Money Price { get; }

    public Money CashDelta { get; }

    public static MarketCard Offer(
        string id,
        string title,
        string summary,
        string assetName,
        bool perShare,
        Money offer)
    {
        ArgumentNullException.ThrowIfNull(assetName);
        return Create(id, MarketKind.Offer, title, summary, assetName, perShare, offer, Money.Zero);
    }

    public static MarketCard Event(string id, string title, string summary, Money cashDelta)
    {
        return Create(id, MarketKind.Event, title, summary, assetName: null, perShare: false, Money.Zero, cashDelta);
    }

    public static MarketCard Create(
        string id,
        MarketKind kind,
        string title,
        string summary,
        string? assetName,
        bool perShare,
        Money offer,
        Money cashDelta)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(summary);

        List<string> errors = new();
        string slug = CardText.RequireId(errors, id);
        string trimmedTitle = CardText.RequireTitle(errors, title);
        string trimmedSummary = CardText.RequireSummary(errors, summary);
        string? trimmedAsset = string.IsNullOrWhiteSpace(assetName) ? null : assetName.Trim();

        if (!Enum.IsDefined(kind))
        {
            errors.Add("Market kind is not an offer or an event.");
        }
        else if (kind == MarketKind.Offer)
        {
            trimmedAsset = RequireOffer(errors, trimmedAsset, perShare, offer, cashDelta);
        }
        else
        {
            RequireEvent(errors, trimmedAsset, perShare, offer, cashDelta);
        }

        if (errors.Count > 0)
        {
            throw new CardValidationException(errors);
        }

        return new MarketCard(slug, kind, trimmedTitle, trimmedSummary, trimmedAsset, perShare, offer, cashDelta);
    }

    private static string? RequireOffer(
        List<string> errors,
        string? assetName,
        bool perShare,
        Money offer,
        Money cashDelta)
    {
        if (perShare)
        {
            assetName = assetName?.ToUpperInvariant();
            if (assetName is null || !IsSymbol(assetName))
            {
                errors.Add("A share offer needs a stock symbol.");
            }
        }
        else if (assetName is null || assetName.Length is < 2 or > 60 || assetName.Any(char.IsControl))
        {
            errors.Add("An offer needs the asset it wants to buy.");
        }

        if (!offer.IsPositive)
        {
            errors.Add("An offer price must be greater than zero.");
        }

        if (!cashDelta.IsZero)
        {
            errors.Add("An offer does not change everyone's cash.");
        }

        return assetName;
    }

    private static void RequireEvent(
        List<string> errors,
        string? assetName,
        bool perShare,
        Money offer,
        Money cashDelta)
    {
        if (assetName is not null)
        {
            errors.Add("An event does not name an asset.");
        }

        if (perShare)
        {
            errors.Add("An event is not priced per share.");
        }

        if (!offer.IsZero)
        {
            errors.Add("An event does not name an offer price.");
        }

        if (cashDelta.IsZero)
        {
            errors.Add("An event must change everyone's cash.");
        }
    }

    private static bool IsSymbol(string symbol)
    {
        if (symbol.Length is < 1 or > 8)
        {
            return false;
        }

        if (!char.IsAsciiLetter(symbol[0]))
        {
            return false;
        }

        for (int index = 1; index < symbol.Length; index++)
        {
            if (!char.IsAsciiLetterOrDigit(symbol[index]))
            {
                return false;
            }
        }

        return true;
    }
}
