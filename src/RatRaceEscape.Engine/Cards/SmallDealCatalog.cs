using System.Reflection;
using System.Text.Json;

namespace RatRaceEscape.Engine;

/// <summary>
/// The small-deals deck shipped with the game: stocks and modest properties.
/// Amounts in the embedded file are integer cents. Every entry is passed
/// through <see cref="SmallDeal.Create"/>.
/// </summary>
public static class SmallDealCatalog
{
    private const string ResourceName = "RatRaceEscape.Engine.Cards.small-deals.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static IReadOnlyList<SmallDeal> All { get; } = LoadEmbedded();

    public static SmallDeal GetById(string id)
    {
        ArgumentNullException.ThrowIfNull(id);

        foreach (SmallDeal deal in All)
        {
            if (deal.Id == id)
            {
                return deal;
            }
        }

        throw new KeyNotFoundException($"No small deal with id '{id}'.");
    }

    public static Deck<SmallDeal> CreateDeck(IShuffle shuffle)
    {
        ArgumentNullException.ThrowIfNull(shuffle);
        return Deck<SmallDeal>.Create(All, shuffle);
    }

    public static IReadOnlyList<SmallDeal> Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        List<SmallDealDocument>? documents = JsonSerializer.Deserialize<List<SmallDealDocument>>(json, SerializerOptions);
        if (documents is null || documents.Count == 0)
        {
            throw new JsonException("Small deal catalog must contain at least one deal.");
        }

        List<SmallDeal> deals = new(documents.Count);
        HashSet<string> ids = new(StringComparer.Ordinal);

        foreach (SmallDealDocument? document in documents)
        {
            if (document is null)
            {
                throw new JsonException("Small deal catalog contains an empty entry.");
            }

            SmallDeal deal = document.ToDeal();
            if (!ids.Add(deal.Id))
            {
                throw new InvalidOperationException($"Small deal id '{deal.Id}' is duplicated.");
            }

            deals.Add(deal);
        }

        return deals;
    }

    private static IReadOnlyList<SmallDeal> LoadEmbedded()
    {
        Assembly assembly = typeof(SmallDealCatalog).Assembly;
        using Stream? stream = assembly.GetManifestResourceStream(ResourceName);
        if (stream is null)
        {
            throw new InvalidOperationException($"Missing embedded small deal catalog '{ResourceName}'.");
        }

        using StreamReader reader = new(stream);
        return Parse(reader.ReadToEnd());
    }

    private sealed class SmallDealDocument
    {
        public string? Id { get; set; }

        public string? Kind { get; set; }

        public string? Title { get; set; }

        public string? Summary { get; set; }

        public string? Symbol { get; set; }

        public long PricePerShareCents { get; set; }

        public long DividendPerShareCents { get; set; }

        public long DownPaymentCents { get; set; }

        public long MortgageCents { get; set; }

        public long MonthlyCashFlowCents { get; set; }

        public SmallDeal ToDeal()
        {
            if (string.IsNullOrWhiteSpace(Kind))
            {
                throw new JsonException("Small deal kind is required.");
            }

            string kind = Kind.Trim();
            if (!kind.Equals("stock", StringComparison.OrdinalIgnoreCase)
                && !kind.Equals("property", StringComparison.OrdinalIgnoreCase))
            {
                throw new JsonException($"Small deal kind '{Kind}' is not stock or property.");
            }

            return SmallDeal.Create(
                Id!,
                kind.Equals("stock", StringComparison.OrdinalIgnoreCase) ? SmallDealKind.Stock : SmallDealKind.Property,
                Title!,
                Summary!,
                Symbol,
                Money.FromCents(PricePerShareCents),
                Money.FromCents(DividendPerShareCents),
                Money.FromCents(DownPaymentCents),
                Money.FromCents(MortgageCents),
                Money.FromCents(MonthlyCashFlowCents));
        }
    }
}
