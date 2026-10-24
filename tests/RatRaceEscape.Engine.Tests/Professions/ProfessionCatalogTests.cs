using System.Text.Json;

namespace RatRaceEscape.Engine.Tests;

public class ProfessionCatalogTests
{
    private static readonly string[] ExpectedIds =
    [
        "night-baker",
        "municipal-archivist",
        "harbor-ferry-mate",
        "clinic-lab-tech",
        "grid-electrician",
        "trail-designer",
        "tunnel-inspector",
        "reef-surveyor",
    ];

    [Fact]
    public void All_loads_the_original_roster_in_order()
    {
        IReadOnlyList<Profession> professions = ProfessionCatalog.All;

        Assert.Equal(ExpectedIds, professions.Select(profession => profession.Id));
    }

    [Fact]
    public void Each_profession_passes_the_starting_position_rules()
    {
        Assert.NotEmpty(ProfessionCatalog.All);

        foreach (Profession profession in ProfessionCatalog.All)
        {
            Profession revalidated = Profession.Create(
                profession.Id,
                profession.Title,
                profession.Summary,
                profession.MonthlySalary,
                profession.MonthlyTaxes,
                profession.Expenses,
                profession.StartingSavings,
                profession.Liabilities);

            Assert.Equal(profession.Title, revalidated.Title);
            Assert.True(revalidated.MonthlyCashFlow(0).IsPositive);
            Assert.False(revalidated.StartingSavings.IsNegative);
            Assert.True(revalidated.MonthlyTaxes < revalidated.MonthlySalary);
        }
    }

    [Fact]
    public void Night_baker_uses_the_authored_pay_and_bills()
    {
        Profession baker = ProfessionCatalog.GetById("night-baker");

        Assert.Equal("Night-Shift Baker", baker.Title);
        Assert.Equal(Money.FromDollars(2_800), baker.MonthlySalary);
        Assert.Equal(Money.FromDollars(560), baker.MonthlyTaxes);
        Assert.Equal(Money.FromDollars(650), baker.Expenses.Mortgage);
        Assert.Equal(Money.FromDollars(78_000), baker.Liabilities.Mortgage);
        Assert.Equal(Money.FromDollars(180), baker.Expenses.CarLoan);
        Assert.Equal(Money.FromDollars(5_400), baker.Liabilities.CarLoan);
        Assert.Equal(Money.FromDollars(60), baker.Expenses.CreditCard);
        Assert.Equal(Money.FromDollars(1_800), baker.Liabilities.CreditCard);
        Assert.Equal(Money.FromDollars(720), baker.Expenses.Other);
        Assert.Equal(Money.FromDollars(280), baker.Expenses.PerChild);
        Assert.Equal(Money.FromDollars(900), baker.StartingSavings);
        Assert.Equal(Money.FromDollars(630), baker.MonthlyCashFlow(0));
    }

    [Fact]
    public void GetById_is_case_sensitive()
    {
        Assert.Throws<KeyNotFoundException>(() => ProfessionCatalog.GetById("Night-Baker"));
    }

    [Fact]
    public void GetById_rejects_a_null_id()
    {
        Assert.Throws<ArgumentNullException>(() => ProfessionCatalog.GetById(null!));
    }

    [Fact]
    public void Parse_rejects_a_duplicate_id()
    {
        string json = "[" + SampleJob("sample-job") + "," + SampleJob("sample-job") + "]";

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => ProfessionCatalog.Parse(json));

        Assert.Contains("duplicated", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_rejects_a_profession_that_fails_validation()
    {
        string json = """
            [
              {
                "id": "test-job",
                "title": "Test Job",
                "summary": "A placeholder that fails validation.",
                "monthlySalaryCents": 0,
                "monthlyTaxesCents": 0,
                "mortgagePaymentCents": 0,
                "carLoanPaymentCents": 0,
                "creditCardPaymentCents": 0,
                "otherExpensesCents": 0,
                "perChildExpenseCents": 0,
                "startingSavingsCents": 0,
                "mortgageBalanceCents": 0,
                "carLoanBalanceCents": 0,
                "creditCardBalanceCents": 0
              }
            ]
            """;

        Assert.Throws<ProfessionValidationException>(() => ProfessionCatalog.Parse(json));
    }

    [Fact]
    public void Parse_rejects_an_empty_catalog()
    {
        Assert.Throws<JsonException>(() => ProfessionCatalog.Parse("[]"));
    }

    [Fact]
    public void Parse_rejects_null_json()
    {
        Assert.Throws<ArgumentNullException>(() => ProfessionCatalog.Parse(null!));
    }

    private static string SampleJob(string id)
    {
        return $$"""
            {
              "id": "{{id}}",
              "title": "Sample Job",
              "summary": "Used only to exercise the catalog parser.",
              "monthlySalaryCents": 100000,
              "monthlyTaxesCents": 10000,
              "mortgagePaymentCents": 0,
              "carLoanPaymentCents": 0,
              "creditCardPaymentCents": 0,
              "otherExpensesCents": 10000,
              "perChildExpenseCents": 0,
              "startingSavingsCents": 0,
              "mortgageBalanceCents": 0,
              "carLoanBalanceCents": 0,
              "creditCardBalanceCents": 0
            }
            """;
    }
}
