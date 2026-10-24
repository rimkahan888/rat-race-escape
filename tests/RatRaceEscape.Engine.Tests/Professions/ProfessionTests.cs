namespace RatRaceEscape.Engine.Tests;

public class ProfessionTests
{
    [Fact]
    public void Create_stores_the_profession_and_trims_text()
    {
        Profession profession = ValidProfession(title: "  Night-Shift Baker  ", summary: "  Starts before dawn so the neighborhood has bread.  ");

        Assert.Equal("night-baker", profession.Id);
        Assert.Equal("Night-Shift Baker", profession.Title);
        Assert.Equal("Starts before dawn so the neighborhood has bread.", profession.Summary);
        Assert.Equal(Money.FromDollars(2_800), profession.MonthlySalary);
        Assert.Equal(Money.FromDollars(560), profession.MonthlyTaxes);
        Assert.Equal(Money.FromDollars(900), profession.StartingSavings);
        Assert.Equal(Money.FromDollars(650), profession.Expenses.Mortgage);
        Assert.Equal(Money.FromDollars(180), profession.Expenses.CarLoan);
        Assert.Equal(Money.FromDollars(60), profession.Expenses.CreditCard);
        Assert.Equal(Money.FromDollars(720), profession.Expenses.Other);
        Assert.Equal(Money.FromDollars(280), profession.Expenses.PerChild);
        Assert.Equal(Money.FromDollars(78_000), profession.Liabilities.Mortgage);
        Assert.Equal(Money.FromDollars(5_400), profession.Liabilities.CarLoan);
        Assert.Equal(Money.FromDollars(1_800), profession.Liabilities.CreditCard);
    }

    [Fact]
    public void Monthly_cash_flow_subtracts_taxes_and_expenses_from_salary()
    {
        Profession profession = ValidProfession();

        Assert.Equal(Money.FromDollars(1_610), profession.Expenses.Total(0));
        Assert.Equal(Money.FromDollars(2_170), profession.Expenses.Total(2));
        Assert.Equal(Money.FromDollars(85_200), profession.Liabilities.Total);
        Assert.Equal(Money.FromDollars(630), profession.MonthlyCashFlow(0));
        Assert.Equal(Money.FromDollars(70), profession.MonthlyCashFlow(2));
        Assert.Equal(Money.FromCents(-21_000), profession.MonthlyCashFlow(3));
    }

    [Fact]
    public void Expense_total_rejects_a_negative_child_count()
    {
        Profession profession = ValidProfession();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => profession.Expenses.Total(-1));

        Assert.Equal("childCount", exception.ParamName);
    }

    [Fact]
    public void Create_rejects_a_null_id()
    {
        Assert.Throws<ArgumentNullException>(() => ValidProfession(id: null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("Night-Baker")]
    [InlineData("night baker")]
    [InlineData("-night")]
    [InlineData("night-")]
    [InlineData("night--baker")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public void Create_rejects_an_id_that_is_not_a_slug(string? id)
    {
        ProfessionValidationException exception = Assert.Throws<ProfessionValidationException>(
            () => ValidProfession(id: id));

        Assert.Contains("Id must be a lowercase kebab-case slug.", exception.Errors);
    }

    [Fact]
    public void Create_rejects_a_title_outside_the_allowed_length()
    {
        Assert.Contains(
            "Title must be between 2 and 60 characters.",
            Assert.Throws<ProfessionValidationException>(() => ValidProfession(title: "A")).Errors);
        Assert.Contains(
            "Title must be between 2 and 60 characters.",
            Assert.Throws<ProfessionValidationException>(() => ValidProfession(title: new string('A', 61))).Errors);
    }

    [Fact]
    public void Create_rejects_a_summary_outside_the_allowed_length()
    {
        Assert.Contains(
            "Summary must be between 12 and 240 characters.",
            Assert.Throws<ProfessionValidationException>(() => ValidProfession(summary: "Too short.")).Errors);
    }

    [Fact]
    public void Create_rejects_a_salary_that_is_not_positive()
    {
        ProfessionValidationException exception = Assert.Throws<ProfessionValidationException>(
            () => ValidProfession(monthlySalary: Money.Zero));

        Assert.Contains("Monthly salary must be greater than zero.", exception.Errors);
    }

    [Theory]
    [InlineData(2_800)]
    [InlineData(2_801)]
    public void Create_rejects_taxes_that_are_not_below_salary(int taxDollars)
    {
        ProfessionValidationException exception = Assert.Throws<ProfessionValidationException>(
            () => ValidProfession(monthlyTaxes: Money.FromDollars(taxDollars)));

        Assert.Contains("Monthly taxes must be zero or greater and less than salary.", exception.Errors);
    }

    [Fact]
    public void Create_rejects_negative_taxes()
    {
        ProfessionValidationException exception = Assert.Throws<ProfessionValidationException>(
            () => ValidProfession(monthlyTaxes: Money.FromDollars(-1)));

        Assert.Contains("Monthly taxes must be zero or greater and less than salary.", exception.Errors);
    }

    [Fact]
    public void Create_rejects_negative_savings_and_expense_parts()
    {
        AssertHasError(
            () => ValidProfession(startingSavings: Money.FromDollars(-1)),
            "Starting savings cannot be negative.");
        AssertHasError(
            () => ValidProfession(otherExpenses: Money.FromDollars(-1)),
            "Other expenses cannot be negative.");
        AssertHasError(
            () => ValidProfession(perChildExpense: Money.FromDollars(-1)),
            "Per-child expense cannot be negative.");
    }

    [Theory]
    [InlineData("mortgage")]
    [InlineData("carLoan")]
    [InlineData("creditCard")]
    public void Create_requires_a_payment_that_fits_the_balance(string loan)
    {
        AssertHasError(
            () => ValidProfession(SetBalance(loan, Money.FromDollars(1_000), Money.Zero)),
            PaymentRequired(loan));
        AssertHasError(
            () => ValidProfession(SetBalance(loan, Money.Zero, Money.FromDollars(10))),
            PaymentWithoutBalance(loan));
        AssertHasError(
            () => ValidProfession(SetBalance(loan, Money.FromDollars(100), Money.FromDollars(101))),
            PaymentExceedsBalance(loan));
        AssertHasError(
            () => ValidProfession(SetBalance(loan, Money.FromDollars(-1), Money.FromDollars(10))),
            NegativeBalance(loan));
        AssertHasError(
            () => ValidProfession(SetBalance(loan, Money.FromDollars(100), Money.FromDollars(-1))),
            NegativePayment(loan));
    }

    [Fact]
    public void Create_rejects_a_profession_with_no_positive_cash_flow()
    {
        ProfessionValidationException exception = Assert.Throws<ProfessionValidationException>(
            () => ValidProfession(otherExpenses: Money.FromDollars(1_350)));

        Assert.Contains("Monthly cash flow with no children must be greater than zero.", exception.Errors);
    }

    [Fact]
    public void Create_allows_zero_savings_and_zero_taxes_when_cash_flow_stays_positive()
    {
        Profession profession = ValidProfession(monthlyTaxes: Money.Zero, startingSavings: Money.Zero);

        Assert.Equal(Money.FromDollars(1_190), profession.MonthlyCashFlow(0));
        Assert.Equal(Money.Zero, profession.StartingSavings);
    }

    [Fact]
    public void Create_reports_every_broken_rule_together()
    {
        ProfessionValidationException exception = Assert.Throws<ProfessionValidationException>(
            () => ValidProfession(
                id: "NOPE",
                monthlySalary: Money.Zero,
                startingSavings: Money.FromDollars(-5)));

        Assert.Contains("Id must be a lowercase kebab-case slug.", exception.Errors);
        Assert.Contains("Monthly salary must be greater than zero.", exception.Errors);
        Assert.Contains("Starting savings cannot be negative.", exception.Errors);
        Assert.Contains("Monthly cash flow with no children must be greater than zero.", exception.Errors);
    }

    private static void AssertHasError(Func<Profession> create, string expected)
    {
        ProfessionValidationException exception = Assert.Throws<ProfessionValidationException>(create);
        Assert.Contains(expected, exception.Errors);
    }

    private static Profession ValidProfession(
        string? id = "night-baker",
        string? title = "Night-Shift Baker",
        string? summary = "Starts before dawn so the neighborhood has bread.",
        Money? monthlySalary = null,
        Money? monthlyTaxes = null,
        Money? mortgagePayment = null,
        Money? carLoanPayment = null,
        Money? creditCardPayment = null,
        Money? otherExpenses = null,
        Money? perChildExpense = null,
        Money? startingSavings = null,
        Money? mortgageBalance = null,
        Money? carLoanBalance = null,
        Money? creditCardBalance = null)
    {
        return Profession.Create(
            id!,
            title!,
            summary!,
            monthlySalary ?? Money.FromDollars(2_800),
            monthlyTaxes ?? Money.FromDollars(560),
            new ProfessionExpenses(
                mortgagePayment ?? Money.FromDollars(650),
                carLoanPayment ?? Money.FromDollars(180),
                creditCardPayment ?? Money.FromDollars(60),
                otherExpenses ?? Money.FromDollars(720),
                perChildExpense ?? Money.FromDollars(280)),
            startingSavings ?? Money.FromDollars(900),
            new ProfessionLiabilities(
                mortgageBalance ?? Money.FromDollars(78_000),
                carLoanBalance ?? Money.FromDollars(5_400),
                creditCardBalance ?? Money.FromDollars(1_800)));
    }

    private static Action<ProfessionDraft> SetBalance(string loan, Money balance, Money payment)
    {
        return draft =>
        {
            switch (loan)
            {
                case "mortgage":
                    draft.MortgageBalance = balance;
                    draft.MortgagePayment = payment;
                    break;
                case "carLoan":
                    draft.CarLoanBalance = balance;
                    draft.CarLoanPayment = payment;
                    break;
                case "creditCard":
                    draft.CreditCardBalance = balance;
                    draft.CreditCardPayment = payment;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(loan), loan, "Unknown loan.");
            }
        };
    }

    private static Profession ValidProfession(Action<ProfessionDraft> mutate)
    {
        ProfessionDraft draft = ProfessionDraft.Baseline();
        mutate(draft);
        return draft.Create();
    }

    private static string PaymentRequired(string loan) => loan switch
    {
        "mortgage" => "Mortgage payment is required when the mortgage balance is greater than zero.",
        "carLoan" => "Car loan payment is required when the car loan balance is greater than zero.",
        "creditCard" => "Credit card payment is required when the credit card balance is greater than zero.",
        _ => throw new ArgumentOutOfRangeException(nameof(loan), loan, null),
    };

    private static string PaymentWithoutBalance(string loan) => loan switch
    {
        "mortgage" => "Mortgage payment must be zero when there is no mortgage balance.",
        "carLoan" => "Car loan payment must be zero when there is no car loan balance.",
        "creditCard" => "Credit card payment must be zero when there is no credit card balance.",
        _ => throw new ArgumentOutOfRangeException(nameof(loan), loan, null),
    };

    private static string PaymentExceedsBalance(string loan) => loan switch
    {
        "mortgage" => "Mortgage payment cannot exceed the mortgage balance.",
        "carLoan" => "Car loan payment cannot exceed the car loan balance.",
        "creditCard" => "Credit card payment cannot exceed the credit card balance.",
        _ => throw new ArgumentOutOfRangeException(nameof(loan), loan, null),
    };

    private static string NegativeBalance(string loan) => loan switch
    {
        "mortgage" => "Mortgage balance cannot be negative.",
        "carLoan" => "Car loan balance cannot be negative.",
        "creditCard" => "Credit card balance cannot be negative.",
        _ => throw new ArgumentOutOfRangeException(nameof(loan), loan, null),
    };

    private static string NegativePayment(string loan) => loan switch
    {
        "mortgage" => "Mortgage payment cannot be negative.",
        "carLoan" => "Car loan payment cannot be negative.",
        "creditCard" => "Credit card payment cannot be negative.",
        _ => throw new ArgumentOutOfRangeException(nameof(loan), loan, null),
    };

    private sealed class ProfessionDraft
    {
        public string Id { get; set; } = "night-baker";
        public string Title { get; set; } = "Night-Shift Baker";
        public string Summary { get; set; } = "Starts before dawn so the neighborhood has bread.";
        public Money MonthlySalary { get; set; } = Money.FromDollars(2_800);
        public Money MonthlyTaxes { get; set; } = Money.FromDollars(560);
        public Money MortgagePayment { get; set; } = Money.FromDollars(650);
        public Money CarLoanPayment { get; set; } = Money.FromDollars(180);
        public Money CreditCardPayment { get; set; } = Money.FromDollars(60);
        public Money OtherExpenses { get; set; } = Money.FromDollars(720);
        public Money PerChildExpense { get; set; } = Money.FromDollars(280);
        public Money StartingSavings { get; set; } = Money.FromDollars(900);
        public Money MortgageBalance { get; set; } = Money.FromDollars(78_000);
        public Money CarLoanBalance { get; set; } = Money.FromDollars(5_400);
        public Money CreditCardBalance { get; set; } = Money.FromDollars(1_800);

        public static ProfessionDraft Baseline() => new();

        public Profession Create()
        {
            return Profession.Create(
                Id,
                Title,
                Summary,
                MonthlySalary,
                MonthlyTaxes,
                new ProfessionExpenses(MortgagePayment, CarLoanPayment, CreditCardPayment, OtherExpenses, PerChildExpense),
                StartingSavings,
                new ProfessionLiabilities(MortgageBalance, CarLoanBalance, CreditCardBalance));
        }
    }
}
