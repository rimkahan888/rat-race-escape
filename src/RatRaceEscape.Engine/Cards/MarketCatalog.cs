using System.Reflection;
using System.Text.Json;

namespace RatRaceEscape.Engine;

/// <summary>
/// The market deck shipped with the game: offers for assets a player may own,
/// and events that change every player's cash. Amounts in the embedded file
/// are integer cents. Every entry is passed through <see cref="MarketCard.Create"/>.
/// </summary>
public static class MarketCatalog
{
    private const string ResourceName = "RatRaceEscape.Engine.Cards.market.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static IReadOnlyList<MarketCard> All { get; } = LoadEmbedded();

    public static MarketCard GetById(string id)
    {
        ArgumentNullException.ThrowIfNull(id);

        foreach (MarketCard card in All)
        {
            if (card.Id == id)
            {
                return card;
            }
        }

        throw new KeyNotFoundException($"No market card with id '{id}'.");
    }

    public static Deck<MarketCard> CreateDeck(IShuffle shuffle)
    {
        ArgumentNullException.ThrowIfNull(shuffle);
        return Deck<MarketCard>.Create(All, shuffle);
    }

    public static IReadOnlyList<MarketCard> Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        List<MarketDocument>? documents = JsonSerializer.Deserialize<List<MarketDocument>>(json, SerializerOptions);
        if (documents is null || documents.Count == 0)
        {
            throw new JsonException("Market catalog must contain at least one card.");
        }

        List<MarketCard> cards = new(documents.Count);
        HashSet<string> ids = new(StringComparer.Ordinal);

        foreach (MarketDocument? document in documents)
        {
            if (document is null)
            {
                throw new JsonException("Market catalog contains an empty entry.");
            }

            MarketCard card = document.ToCard();
            if (!ids.Add(card.Id))
            {
                throw new InvalidOperationException($"Market card id '{card.Id}' is duplicated.");
            }

            cards.Add(card);
        }

        return cards;
    }

    private static IReadOnlyList<MarketCard> LoadEmbedded()
    {
        Assembly assembly = typeof(MarketCatalog).Assembly;
        using Stream? stream = assembly.GetManifestResourceStream(ResourceName);
        if (stream is null)
        {
            throw new InvalidOperationException($"Missing embedded market catalog '{ResourceName}'.");
        }

        using StreamReader reader = new(stream);
        return Parse(reader.ReadToEnd());
    }

    private sealed class MarketDocument
    {
        public string? Id { get; set; }

        public string? Kind { get; set; }

        public string? Title { get; set; }

        public string? Summary { get; set; }

        public string? AssetName { get; set; }

        public bool PerShare { get; set; }

        public long OfferCents { get; set; }

        public long CashDeltaCents { get; set; }

        public MarketCard ToCard()
        {
            if (string.IsNullOrWhiteSpace(Kind))
            {
                throw new JsonException("Market kind is required.");
            }

            string kind = Kind.Trim();
            if (!kind.Equals("offer", StringComparison.OrdinalIgnoreCase)
                && !kind.Equals("event", StringComparison.OrdinalIgnoreCase))
            {
                throw new JsonException($"Market kind '{Kind}' is not an offer or an event.");
            }

            return MarketCard.Create(
                Id!,
                kind.Equals("offer", StringComparison.OrdinalIgnoreCase) ? MarketKind.Offer : MarketKind.Event,
                Title!,
                Summary!,
                AssetName,
                PerShare,
                Money.FromCents(OfferCents),
                Money.FromCents(CashDeltaCents));
        }
    }
}
