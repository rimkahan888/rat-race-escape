using System.Reflection;
using System.Text.Json;

namespace RatRaceEscape.Engine;

/// <summary>
/// The doodad deck shipped with the game: unplanned expenses. Amounts in the
/// embedded file are integer cents. Every entry is passed through <see cref="Doodad.Create"/>.
/// </summary>
public static class DoodadCatalog
{
    private const string ResourceName = "RatRaceEscape.Engine.Cards.doodads.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static IReadOnlyList<Doodad> All { get; } = LoadEmbedded();

    public static Doodad GetById(string id)
    {
        ArgumentNullException.ThrowIfNull(id);

        foreach (Doodad doodad in All)
        {
            if (doodad.Id == id)
            {
                return doodad;
            }
        }

        throw new KeyNotFoundException($"No doodad with id '{id}'.");
    }

    public static Deck<Doodad> CreateDeck(IShuffle shuffle)
    {
        ArgumentNullException.ThrowIfNull(shuffle);
        return Deck<Doodad>.Create(All, shuffle);
    }

    public static IReadOnlyList<Doodad> Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        List<DoodadDocument>? documents = JsonSerializer.Deserialize<List<DoodadDocument>>(json, SerializerOptions);
        if (documents is null || documents.Count == 0)
        {
            throw new JsonException("Doodad catalog must contain at least one card.");
        }

        List<Doodad> doodads = new(documents.Count);
        HashSet<string> ids = new(StringComparer.Ordinal);

        foreach (DoodadDocument? document in documents)
        {
            if (document is null)
            {
                throw new JsonException("Doodad catalog contains an empty entry.");
            }

            Doodad doodad = document.ToDoodad();
            if (!ids.Add(doodad.Id))
            {
                throw new InvalidOperationException($"Doodad id '{doodad.Id}' is duplicated.");
            }

            doodads.Add(doodad);
        }

        return doodads;
    }

    private static IReadOnlyList<Doodad> LoadEmbedded()
    {
        Assembly assembly = typeof(DoodadCatalog).Assembly;
        using Stream? stream = assembly.GetManifestResourceStream(ResourceName);
        if (stream is null)
        {
            throw new InvalidOperationException($"Missing embedded doodad catalog '{ResourceName}'.");
        }

        using StreamReader reader = new(stream);
        return Parse(reader.ReadToEnd());
    }

    private sealed class DoodadDocument
    {
        public string? Id { get; set; }

        public string? Title { get; set; }

        public string? Summary { get; set; }

        public long CostCents { get; set; }

        public Doodad ToDoodad()
        {
            return Doodad.Create(Id!, Title!, Summary!, Money.FromCents(CostCents));
        }
    }
}
