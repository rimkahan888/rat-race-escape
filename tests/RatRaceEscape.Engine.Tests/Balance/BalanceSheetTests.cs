namespace RatRaceEscape.Engine.Tests;

public class BalanceSheetTests
{
    [Fact]
    public void FromProfession_copies_savings_and_the_three_starting_debts()
    {
        Profession baker = ProfessionCatalog.GetById("night-baker");

        BalanceSheet sheet = BalanceSheet.FromProfession(baker);

        Assert.Equal(Money.FromDollars(900), sheet.Savings);
        Assert.Equal(90_000, sheet.Savings.Cents);
        Assert.Equal(baker.Liabilities.Mortgage, sheet.Mortgage);
        Assert.Equal(baker.Liabilities.CarLoan, sheet.CarLoan);
        Assert.Equal(baker.Liabilities.CreditCard, sheet.CreditCard);
        Assert.Empty(sheet.Stocks);
        Assert.Empty(sheet.RealEstate);
        Assert.Empty(sheet.Businesses);
        Assert.Empty(sheet.OtherLoans);
        Assert.Equal(Money.FromDollars(900), sheet.TotalAssets);
        Assert.Equal(Money.FromDollars(85_200), sheet.TotalLiabilities);
    }

    [Fact]
    public void FromProfession_rejects_a_null_profession()
    {
        Assert.Throws<ArgumentNullException>(() => BalanceSheet.FromProfession(null!));
    }

    [Fact]
    public void Totals_include_every_asset_cost_and_every_balance_owed()
    {
        StockHolding park = StockHolding.Create(" park ", 2, Money.FromDollars(50), Money.FromDollars(1));
        RealEstateHolding flat = RealEstateHolding.Create(
            "  Elm Street Flat  ",
            Money.FromDollars(200),
            Money.FromDollars(800),
            Money.FromDollars(-10));
        BusinessHolding cart = BusinessHolding.Create(
            "Harbor Cart",
            Money.FromDollars(300),
            Money.Zero,
            Money.FromDollars(40));
        LoanLiability note = LoanLiability.Create("  Bank note  ", Money.FromDollars(75), Money.FromDollars(5));

        BalanceSheet sheet = BalanceSheet.Create(
            Money.FromDollars(100),
            [park],
            [flat],
            [cart],
            Money.FromDollars(400),
            Money.FromDollars(50),
            Money.FromDollars(25),
            [note]);

        Assert.Equal("PARK", park.Symbol);
        Assert.Equal(Money.FromDollars(100), park.Cost);
        Assert.Equal(Money.FromDollars(2), park.MonthlyIncome);
        Assert.Equal("Elm Street Flat", flat.Name);
        Assert.Equal(Money.FromDollars(-10), flat.MonthlyCashFlow);
        Assert.Equal("Harbor Cart", cart.Name);
        Assert.Equal("Bank note", note.Name);
        Assert.Equal(Money.FromDollars(700), sheet.TotalAssets);
        Assert.Equal(Money.FromDollars(400), sheet.Mortgage);
        Assert.Equal(Money.FromDollars(1_350), sheet.TotalLiabilities);
    }

    [Fact]
    public void Separate_lots_of_the_same_symbol_both_count_as_assets()
    {
        StockHolding first = StockHolding.Create("PARK", 1, Money.FromDollars(10), Money.Zero);
        StockHolding second = StockHolding.Create("park", 1, Money.FromDollars(15), Money.Zero);

        BalanceSheet sheet = EmptySheet(stocks: [first, second]);

        Assert.Equal(2, sheet.Stocks.Count);
        Assert.Equal(Money.FromDollars(25), sheet.TotalAssets);
    }

    [Fact]
    public void Create_copies_the_lists_it_is_given()
    {
        List<StockHolding> stocks = [StockHolding.Create("PARK", 1, Money.FromDollars(10), Money.Zero)];

        BalanceSheet sheet = EmptySheet(stocks: stocks);
        stocks.Clear();

        Assert.Single(sheet.Stocks);
    }

    [Fact]
    public void Create_rejects_a_null_list()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            BalanceSheet.Create(Money.Zero, null!, [], [], Money.Zero, Money.Zero, Money.Zero, []));

        Assert.Equal("stocks", exception.ParamName);
    }

    [Fact]
    public void Create_reports_sheet_errors_together()
    {
        IReadOnlyList<StockHolding> stocks = new StockHolding[] { null! };

        BalanceSheetValidationException exception = Assert.Throws<BalanceSheetValidationException>(() =>
            BalanceSheet.Create(
                Money.FromDollars(-1),
                stocks,
                [],
                [],
                Money.FromDollars(-1),
                Money.FromDollars(-2),
                Money.FromDollars(-3),
                []));

        Assert.Contains("Savings cannot be negative.", exception.Errors);
        Assert.Contains("Mortgage balance cannot be negative.", exception.Errors);
        Assert.Contains("Car loan balance cannot be negative.", exception.Errors);
        Assert.Contains("Credit card balance cannot be negative.", exception.Errors);
        Assert.Contains("Stocks cannot contain an empty entry.", exception.Errors);
    }

    [Fact]
    public void Create_rejects_a_duplicated_loan_name()
    {
        LoanLiability first = LoanLiability.Create("Bank note", Money.FromDollars(10), Money.FromDollars(1));
        LoanLiability second = LoanLiability.Create(" bank note ", Money.FromDollars(10), Money.FromDollars(1));

        BalanceSheetValidationException exception = Assert.Throws<BalanceSheetValidationException>(() =>
            EmptySheet(otherLoans: [first, second]));

        Assert.Contains("Loan name 'bank note' is used more than once.", exception.Errors);
    }

    [Fact]
    public void A_property_mortgage_is_owed_on_top_of_the_home_mortgage()
    {
        RealEstateHolding flat = RealEstateHolding.Create(
            "Elm Street Flat",
            Money.Zero,
            Money.FromDollars(45_000),
            Money.FromDollars(120));

        BalanceSheet sheet = EmptySheet(realEstate: [flat], mortgage: Money.FromDollars(78_000));

        Assert.Equal(Money.FromDollars(78_000), sheet.Mortgage);
        Assert.Equal(Money.FromDollars(123_000), sheet.TotalLiabilities);
        Assert.Equal(Money.Zero, sheet.TotalAssets);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("PARK")]
    [InlineData("park1")]
    [InlineData("ABCDEFGH")]
    public void Stock_symbol_accepts_a_short_ticker(string symbol)
    {
        StockHolding stock = StockHolding.Create(symbol, 1, Money.FromDollars(1), Money.Zero);

        Assert.Equal(symbol.ToUpperInvariant(), stock.Symbol);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("1PARK")]
    [InlineData("PARK-1")]
    [InlineData("ABCDEFGHI")]
    public void Stock_symbol_rejects_anything_that_is_not_a_ticker(string symbol)
    {
        BalanceSheetValidationException exception = Assert.Throws<BalanceSheetValidationException>(() =>
            StockHolding.Create(symbol, 1, Money.FromDollars(1), Money.Zero));

        Assert.Contains(
            "Stock symbol must be 1 to 8 letters or digits and start with a letter.",
            exception.Errors);
    }

    [Fact]
    public void Stock_create_rejects_a_null_symbol()
    {
        Assert.Throws<ArgumentNullException>(() =>
            StockHolding.Create(null!, 1, Money.FromDollars(1), Money.Zero));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Stock_shares_must_be_positive(int shares)
    {
        BalanceSheetValidationException exception = Assert.Throws<BalanceSheetValidationException>(() =>
            StockHolding.Create("PARK", shares, Money.FromDollars(1), Money.Zero));

        Assert.Contains("Stock shares must be greater than zero.", exception.Errors);
    }

    [Fact]
    public void Stock_create_reports_every_broken_rule_together()
    {
        BalanceSheetValidationException exception = Assert.Throws<BalanceSheetValidationException>(() =>
            StockHolding.Create(" ", 0, Money.FromDollars(-1), Money.FromDollars(-1)));

        Assert.Contains("Stock symbol must be 1 to 8 letters or digits and start with a letter.", exception.Errors);
        Assert.Contains("Stock shares must be greater than zero.", exception.Errors);
        Assert.Contains("Stock cost per share cannot be negative.", exception.Errors);
        Assert.Contains("Stock dividend per share cannot be negative.", exception.Errors);
    }

    [Fact]
    public void Stock_cost_throws_when_the_product_overflows()
    {
        StockHolding stock = StockHolding.Create("PARK", int.MaxValue, Money.FromCents(5_000_000_000), Money.Zero);

        Assert.Throws<OverflowException>(() => stock.Cost);
    }

    [Fact]
    public void Create_throws_when_the_asset_total_overflows()
    {
        StockHolding stock = StockHolding.Create("PARK", 1, Money.FromCents(1), Money.Zero);

        Assert.Throws<OverflowException>(() => BalanceSheet.Create(
            Money.FromCents(long.MaxValue),
            [stock],
            [],
            [],
            Money.Zero,
            Money.Zero,
            Money.Zero,
            []));
    }

    [Fact]
    public void Real_estate_accepts_a_negative_cash_flow_and_rejects_a_bad_line()
    {
        RealEstateHolding property = RealEstateHolding.Create(
            "Elm Street Flat",
            Money.Zero,
            Money.Zero,
            Money.FromDollars(-25));

        Assert.Equal(Money.FromDollars(-25), property.MonthlyCashFlow);
        Assert.Equal(new string('A', 60), RealEstateHolding.Create(new string('A', 60), Money.Zero, Money.Zero, Money.Zero).Name);

        BalanceSheetValidationException exception = Assert.Throws<BalanceSheetValidationException>(() =>
            RealEstateHolding.Create("\u0001", Money.FromDollars(-1), Money.FromDollars(-1), Money.Zero));

        Assert.Contains("Real estate name must be between 2 and 60 characters.", exception.Errors);
        Assert.Contains("Real estate name cannot contain control characters.", exception.Errors);
        Assert.Contains("Real estate cost cannot be negative.", exception.Errors);
        Assert.Contains("Real estate mortgage cannot be negative.", exception.Errors);
    }

    [Fact]
    public void Real_estate_create_rejects_a_null_name()
    {
        Assert.Throws<ArgumentNullException>(() =>
            RealEstateHolding.Create(null!, Money.Zero, Money.Zero, Money.Zero));
    }

    [Fact]
    public void Business_stores_a_loan_and_rejects_a_negative_liability()
    {
        BusinessHolding business = BusinessHolding.Create(
            "  Harbor Cart  ",
            Money.FromDollars(3_000),
            Money.FromDollars(1_000),
            Money.FromDollars(80));

        Assert.Equal("Harbor Cart", business.Name);
        Assert.Equal(Money.FromDollars(3_000), business.Cost);
        Assert.Equal(Money.FromDollars(1_000), business.Liability);
        Assert.Equal(Money.FromDollars(80), business.MonthlyCashFlow);

        BalanceSheetValidationException exception = Assert.Throws<BalanceSheetValidationException>(() =>
            BusinessHolding.Create("No", Money.FromDollars(-1), Money.FromDollars(-1), Money.Zero));

        Assert.Contains("Business cost cannot be negative.", exception.Errors);
        Assert.Contains("Business liability cannot be negative.", exception.Errors);
    }

    [Fact]
    public void Business_create_rejects_a_null_name()
    {
        Assert.Throws<ArgumentNullException>(() =>
            BusinessHolding.Create(null!, Money.Zero, Money.Zero, Money.Zero));
    }

    [Fact]
    public void Loan_payment_has_to_fit_the_balance()
    {
        LoanLiability settled = LoanLiability.Create("Paid off", Money.Zero, Money.Zero);
        Assert.True(settled.Balance.IsZero);
        Assert.True(settled.MonthlyPayment.IsZero);

        AssertHasLoanError(
            () => LoanLiability.Create("Bank note", Money.FromDollars(1_000), Money.Zero),
            "Loan payment is required when the loan balance is greater than zero.");
        AssertHasLoanError(
            () => LoanLiability.Create("Bank note", Money.Zero, Money.FromDollars(10)),
            "Loan payment must be zero when there is no loan balance.");
        AssertHasLoanError(
            () => LoanLiability.Create("Bank note", Money.FromDollars(100), Money.FromDollars(101)),
            "Loan payment cannot exceed the loan balance.");
        AssertHasLoanError(
            () => LoanLiability.Create("Bank note", Money.FromDollars(-1), Money.FromDollars(1)),
            "Loan balance cannot be negative.");
        AssertHasLoanError(
            () => LoanLiability.Create("Bank note", Money.FromDollars(100), Money.FromDollars(-1)),
            "Loan payment cannot be negative.");
        AssertHasLoanError(
            () => LoanLiability.Create("A", Money.Zero, Money.Zero),
            "Loan name must be between 2 and 40 characters.");
        AssertHasLoanError(
            () => LoanLiability.Create("Bo\u0001", Money.Zero, Money.Zero),
            "Loan name cannot contain control characters.");
    }

    [Fact]
    public void Loan_create_rejects_a_null_name()
    {
        Assert.Throws<ArgumentNullException>(() => LoanLiability.Create(null!, Money.Zero, Money.Zero));
    }

    [Fact]
    public void Player_starts_on_the_profession_sheet_and_can_take_another()
    {
        Profession baker = ProfessionCatalog.GetById("night-baker");
        Player player = Player.Create("Ava", baker, TokenColour.Amber);

        Assert.Equal(baker.StartingSavings, player.Cash);
        Assert.Equal(baker.Liabilities.Total, player.BalanceSheet.TotalLiabilities);
        Assert.Empty(player.BalanceSheet.Stocks);

        BalanceSheet richer = EmptySheet(savings: Money.FromDollars(2_000));
        Player updated = player.WithBalanceSheet(richer);

        Assert.Equal("Ava", updated.Name);
        Assert.Same(baker, updated.Profession);
        Assert.Equal(TokenColour.Amber, updated.TokenColour);
        Assert.Equal(Money.FromDollars(2_000), updated.Cash);
        Assert.Equal(baker.StartingSavings, player.Cash);
        Assert.Throws<ArgumentNullException>(() => player.WithBalanceSheet(null!));
    }

    private static void AssertHasLoanError(Func<LoanLiability> create, string expected)
    {
        BalanceSheetValidationException exception = Assert.Throws<BalanceSheetValidationException>(create);
        Assert.Contains(expected, exception.Errors);
    }

    private static BalanceSheet EmptySheet(
        Money? savings = null,
        IReadOnlyList<StockHolding>? stocks = null,
        IReadOnlyList<RealEstateHolding>? realEstate = null,
        IReadOnlyList<BusinessHolding>? businesses = null,
        Money? mortgage = null,
        IReadOnlyList<LoanLiability>? otherLoans = null)
    {
        return BalanceSheet.Create(
            savings ?? Money.Zero,
            stocks ?? [],
            realEstate ?? [],
            businesses ?? [],
            mortgage ?? Money.Zero,
            Money.Zero,
            Money.Zero,
            otherLoans ?? []);
    }
}
