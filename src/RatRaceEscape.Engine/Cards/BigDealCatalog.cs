using System.Reflection;
using System.Text.Json;

namespace RatRaceEscape.Engine;

/// <summary>
/// The big-deals deck shipped with the game: larger properties and businesses.
/// Amounts in the embedded file are integer cents. Every entry is passed
/// through <see cref="BigDeal.Create"/>.
/// </summary>
public static class BigDealCatalog
{
    private const string ResourceName = "RatRaceEscape.Engine.Cards.big-deals.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static IReadOnlyList<BigDeal> All { get; } = LoadEmbedded();

    public static BigDeal GetById(string id)
    {
        ArgumentNullException.ThrowIfNull(id);

        foreach (BigDeal deal in All)
        {
            if (deal.Id == id)
            {
                return deal;
            }
        }

        throw new KeyNotFoundException($"No big deal with id '{id}'.");
    }

    public static Deck<BigDeal> CreateDeck(IShuffle shuffle)
    {
        ArgumentNullException.ThrowIfNull(shuffle);
        return Deck<BigDeal>.Create(All, shuffle);
    }

    public static IReadOnlyList<BigDeal> Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        List<BigDealDocument>? documents = JsonSerializer.Deserialize<List<BigDealDocument>>(json, SerializerOptions);
        if (documents is null || documents.Count == 0)
        {
            throw new JsonException("Big deal catalog must contain at least one deal.");
        }

        List<BigDeal> deals = new(documents.Count);
        HashSet<string> ids = new(StringComparer.Ordinal);

        foreach (BigDealDocument? document in documents)
        {
            if (document is null)
            {
                throw new JsonException("Big deal catalog contains an empty entry.");
            }

            BigDeal deal = document.ToDeal();
            if (!ids.Add(deal.Id))
            {
                throw new InvalidOperationException($"Big deal id '{deal.Id}' is duplicated.");
            }

            deals.Add(deal);
        }

        return deals;
    }

    private static IReadOnlyList<BigDeal> LoadEmbedded()
    {
        Assembly assembly = typeof(BigDealCatalog).Assembly;
        using Stream? stream = assembly.GetManifestResourceStream(ResourceName);
        if (stream is null)
        {
            throw new InvalidOperationException($"Missing embedded big deal catalog '{ResourceName}'.");
        }

        using StreamReader reader = new(stream);
        return Parse(reader.ReadToEnd());
    }

    private sealed class BigDealDocument
    {
        public string? Id { get; set; }

        public string? Kind { get; set; }

        public string? Title { get; set; }

        public string? Summary { get; set; }

        public long DownPaymentCents { get; set; }

        public long LoanCents { get; set; }

        public long MonthlyCashFlowCents { get; set; }

        public BigDeal ToDeal()
        {
            if (string.IsNullOrWhiteSpace(Kind))
            {
                throw new JsonException("Big deal kind is required.");
            }

            string kind = Kind.Trim();
            if (!kind.Equals("property", StringComparison.OrdinalIgnoreCase)
                && !kind.Equals("business", StringComparison.OrdinalIgnoreCase))
            {
                throw new JsonException($"Big deal kind '{Kind}' is not property or business.");
            }

            return BigDeal.Create(
                Id!,
                kind.Equals("property", StringComparison.OrdinalIgnoreCase) ? BigDealKind.Property : BigDealKind.Business,
                Title!,
                Summary!,
                Money.FromCents(DownPaymentCents),
                Money.FromCents(LoanCents),
                Money.FromCents(MonthlyCashFlowCents));
        }
    }
}
