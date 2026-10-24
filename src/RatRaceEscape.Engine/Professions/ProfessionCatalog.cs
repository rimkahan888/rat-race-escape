using System.Reflection;
using System.Text.Json;

namespace RatRaceEscape.Engine;

/// <summary>
/// The starting professions shipped with the game. Amounts in the embedded
/// file are integer cents. Every entry is passed through <see cref="Profession.Create"/>.
/// </summary>
public static class ProfessionCatalog
{
    private const string ResourceName = "RatRaceEscape.Engine.Professions.professions.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static IReadOnlyList<Profession> All { get; } = LoadEmbedded();

    public static Profession GetById(string id)
    {
        ArgumentNullException.ThrowIfNull(id);

        foreach (Profession profession in All)
        {
            if (profession.Id == id)
            {
                return profession;
            }
        }

        throw new KeyNotFoundException($"No profession with id '{id}'.");
    }

    public static IReadOnlyList<Profession> Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        List<ProfessionDocument>? documents = JsonSerializer.Deserialize<List<ProfessionDocument>>(json, SerializerOptions);
        if (documents is null || documents.Count == 0)
        {
            throw new JsonException("Profession catalog must contain at least one profession.");
        }

        List<Profession> professions = new(documents.Count);
        HashSet<string> ids = new(StringComparer.Ordinal);

        foreach (ProfessionDocument? document in documents)
        {
            if (document is null)
            {
                throw new JsonException("Profession catalog contains an empty entry.");
            }

            Profession profession = document.ToProfession();
            if (!ids.Add(profession.Id))
            {
                throw new InvalidOperationException($"Profession id '{profession.Id}' is duplicated.");
            }

            professions.Add(profession);
        }

        return professions;
    }

    private static IReadOnlyList<Profession> LoadEmbedded()
    {
        Assembly assembly = typeof(ProfessionCatalog).Assembly;
        using Stream? stream = assembly.GetManifestResourceStream(ResourceName);
        if (stream is null)
        {
            throw new InvalidOperationException($"Missing embedded profession catalog '{ResourceName}'.");
        }

        using StreamReader reader = new(stream);
        return Parse(reader.ReadToEnd());
    }

    private sealed class ProfessionDocument
    {
        public string? Id { get; set; }

        public string? Title { get; set; }

        public string? Summary { get; set; }

        public long MonthlySalaryCents { get; set; }

        public long MonthlyTaxesCents { get; set; }

        public long MortgagePaymentCents { get; set; }

        public long MortgageBalanceCents { get; set; }

        public long CarLoanPaymentCents { get; set; }

        public long CarLoanBalanceCents { get; set; }

        public long CreditCardPaymentCents { get; set; }

        public long CreditCardBalanceCents { get; set; }

        public long OtherExpensesCents { get; set; }

        public long PerChildExpenseCents { get; set; }

        public long StartingSavingsCents { get; set; }

        public Profession ToProfession()
        {
            return Profession.Create(
                Id!,
                Title!,
                Summary!,
                Money.FromCents(MonthlySalaryCents),
                Money.FromCents(MonthlyTaxesCents),
                new ProfessionExpenses(
                    Money.FromCents(MortgagePaymentCents),
                    Money.FromCents(CarLoanPaymentCents),
                    Money.FromCents(CreditCardPaymentCents),
                    Money.FromCents(OtherExpensesCents),
                    Money.FromCents(PerChildExpenseCents)),
                Money.FromCents(StartingSavingsCents),
                new ProfessionLiabilities(
                    Money.FromCents(MortgageBalanceCents),
                    Money.FromCents(CarLoanBalanceCents),
                    Money.FromCents(CreditCardBalanceCents)));
        }
    }
}
