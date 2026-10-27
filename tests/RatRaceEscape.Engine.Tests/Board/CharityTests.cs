namespace RatRaceEscape.Engine.Tests;

public class CharityTests
{
    private static readonly Guid GameId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    [Fact]
    public void Donation_is_one_tenth_of_total_income_and_keeps_the_leftover_cents()
    {
        Assert.Equal(Money.FromCents(28_000), Charity.Donation(Money.FromCents(280_000)));
        Assert.Equal(Money.FromCents(1), Charity.Donation(Money.FromCents(10)));
        Assert.Equal(Money.FromCents(1), Charity.Donation(Money.FromCents(19)));
        Assert.Equal(Money.Zero, Charity.Donation(Money.FromCents(9)));
        Assert.Equal(Money.Zero, Charity.Donation(Money.Zero));
        Assert.Equal(Money.Zero, Charity.Donation(Money.FromCents(-50)));
    }

    [Fact]
    public void Donation_includes_passive_income()
    {
        Profession baker = ProfessionCatalog.GetById("night-baker");
        Player player = Player.Create("Ava", baker, TokenColour.Amber);
        StockHolding park = StockHolding.Create("PARK", 1, Money.FromCents(100), Money.FromCents(10));
        Player investor = player.WithBalanceSheet(BalanceSheet.Create(
            player.Cash,
            [park],
            [],
            [],
            baker.Liabilities.Mortgage,
            baker.Liabilities.CarLoan,
            baker.Liabilities.CreditCard,
            []));

        Money totalIncome = IncomeStatement.For(investor).TotalIncome;
        Money donation = Charity.Donation(totalIncome);
        Player donated = Charity.Donate(investor);

        Assert.Equal(Money.FromCents(280_010), totalIncome);
        Assert.Equal(Money.FromCents(28_001), donation);
        Assert.Equal(investor.Cash - donation, donated.Cash);
        Assert.Equal(Charity.ExtraDieTurns, donated.ExtraDieTurnsRemaining);
    }

    [Fact]
    public void A_second_donation_adds_three_more_turns()
    {
        Player baker = Baker();
        Money donation = Charity.Donation(IncomeStatement.For(baker).TotalIncome);

        Player twice = Charity.Donate(Charity.Donate(baker));

        Assert.Equal(Charity.ExtraDieTurns * 2, twice.ExtraDieTurnsRemaining);
        Assert.Equal(baker.Cash - (donation * 2), twice.Cash);
    }

    [Fact]
    public void Donate_fails_when_cash_cannot_cover_the_share()
    {
        Player broke = Baker().WithCash(Money.FromCents(27_999));

        TurnException exception = Assert.Throws<TurnException>(() => Charity.Donate(broke));

        Assert.Contains("Cash cannot cover the charity donation.", exception.Errors);
        Assert.Equal(Money.FromCents(27_999), broke.Cash);
        Assert.Equal(0, broke.ExtraDieTurnsRemaining);
    }

    [Fact]
    public void Donate_rejects_a_null_player()
    {
        Assert.Throws<ArgumentNullException>(() => Charity.Donate(null!));
    }

    [Fact]
    public void Donating_on_the_charity_square_buys_two_dice_for_the_next_three_turns()
    {
        Game landed = Table().Roll(new ScriptedDieRoller([3]));
        Assert.Equal(SpaceKind.Charity, RatRaceBoard.At(landed.CurrentPlayer.BoardIndex).Kind);
        Assert.Equal(0, landed.CurrentPlayer.ExtraDieTurnsRemaining);
        Assert.Equal(Money.FromCents(90_000), landed.CurrentPlayer.Cash);

        Game donated = landed.Resolve(SpaceChoice.Donate);

        Assert.Equal(TurnPhase.AwaitingEnd, donated.TurnPhase);
        Assert.Equal(Money.FromCents(62_000), donated.CurrentPlayer.Cash);
        Assert.Equal(3, donated.CurrentPlayer.ExtraDieTurnsRemaining);
        Assert.Equal(Money.FromCents(90_000), landed.CurrentPlayer.Cash);

        Game twoDice = donated.EndTurn().Roll(new ScriptedDieRoller([2, 5]), 2);

        Assert.Equal(10, twoDice.CurrentPlayer.BoardIndex);
        Assert.Equal(2, twoDice.CurrentPlayer.ExtraDieTurnsRemaining);
        Assert.Equal(Money.FromCents(63_000), twoDice.LastRoll!.PaydayCash);
        Assert.Equal(Money.FromCents(125_000), twoDice.CurrentPlayer.Cash);

        Game oneDie = twoDice.Resolve().EndTurn().Roll(new ScriptedDieRoller([1]));
        Assert.Equal(SpaceKind.Deal, RatRaceBoard.At(oneDie.CurrentPlayer.BoardIndex).Kind);
        Assert.Equal(1, oneDie.CurrentPlayer.ExtraDieTurnsRemaining);

        Game lastCharge = oneDie.Resolve(SpaceChoice.SmallDeal).EndTurn().Roll(new ScriptedDieRoller([1]));
        Assert.Equal(0, lastCharge.CurrentPlayer.ExtraDieTurnsRemaining);

        Game spent = lastCharge.Resolve().EndTurn();
        ScriptedDieRoller blocked = new([1, 1]);
        TurnException exception = Assert.Throws<TurnException>(() => spent.Roll(blocked, 2));

        Assert.Contains("Two dice are available only while charity dice remain.", exception.Errors);
        Assert.Equal(2, blocked.Remaining);
        Assert.Equal(1, spent.Roll(blocked).LastRoll!.Total);
        Assert.Equal(1, blocked.Remaining);
    }

    [Fact]
    public void Declining_charity_keeps_the_cash_and_one_die()
    {
        Game declined = Table().Roll(new ScriptedDieRoller([3])).Resolve(SpaceChoice.Decline);
        ScriptedDieRoller dice = new([1, 1]);

        Assert.Equal(Money.FromCents(90_000), declined.CurrentPlayer.Cash);
        Assert.Equal(0, declined.CurrentPlayer.ExtraDieTurnsRemaining);
        Assert.Null(declined.PendingDeck);

        TurnException exception = Assert.Throws<TurnException>(() => declined.EndTurn().Roll(dice, 2));

        Assert.Contains("Two dice are available only while charity dice remain.", exception.Errors);
        Assert.Equal(2, dice.Remaining);
    }

    [Fact]
    public void Charity_requires_a_choice_and_other_squares_reject_one()
    {
        Game charity = Table().Roll(new ScriptedDieRoller([3]));
        Game doodad = Table().Roll(new ScriptedDieRoller([1]));

        TurnException missing = Assert.Throws<TurnException>(() => charity.Resolve());
        TurnException unknown = Assert.Throws<TurnException>(() => charity.Resolve((SpaceChoice)99));
        TurnException unwanted = Assert.Throws<TurnException>(() => doodad.Resolve(SpaceChoice.Donate));

        Assert.Contains("Charity needs a donate or decline choice.", missing.Errors);
        Assert.Contains("The space choice is not one this turn understands.", unknown.Errors);
        Assert.Contains("This space does not take a choice.", unwanted.Errors);
        Assert.Equal(TurnPhase.AwaitingResolve, charity.TurnPhase);
        Assert.Equal(Money.FromCents(90_000), charity.CurrentPlayer.Cash);
        Assert.Equal(TurnPhase.AwaitingResolve, doodad.TurnPhase);
    }

    [Fact]
    public void Only_the_donor_may_roll_two_dice()
    {
        Game game = Game.Create(
            GameId,
            [
                new PlayerSetup("Ava", "night-baker", TokenColour.Amber),
                new PlayerSetup("Bo", "municipal-archivist", TokenColour.Cobalt),
            ]);
        ScriptedDieRoller dice = new([3, 1, 2, 5]);

        Game donated = game.Roll(dice).Resolve(SpaceChoice.Donate);
        Assert.Equal(3, donated.Players[0].ExtraDieTurnsRemaining);
        Assert.Equal(0, donated.Players[1].ExtraDieTurnsRemaining);
        Assert.Equal(Money.FromCents(140_000), donated.Players[1].Cash);

        Game bo = donated.EndTurn();
        ScriptedDieRoller blocked = new([6, 6]);
        TurnException exception = Assert.Throws<TurnException>(() => bo.Roll(blocked, 2));
        Assert.Equal(2, blocked.Remaining);
        Assert.Contains("Two dice are available only while charity dice remain.", exception.Errors);

        Game avaAgain = bo.Roll(dice).Resolve().EndTurn();
        Game twoDice = avaAgain.Roll(dice, 2);

        Assert.Equal("Ava", twoDice.CurrentPlayer.Name);
        Assert.Equal(2, twoDice.CurrentPlayer.ExtraDieTurnsRemaining);
        Assert.Equal(0, twoDice.Players[1].ExtraDieTurnsRemaining);
        Assert.Equal(10, twoDice.CurrentPlayer.BoardIndex);
        Assert.Equal(Money.FromCents(125_000), twoDice.CurrentPlayer.Cash);
    }

    [Fact]
    public void Extra_die_turns_stay_with_the_player()
    {
        Player donor = Baker().WithExtraDieTurns(2);

        Assert.Equal(0, Baker().ExtraDieTurnsRemaining);
        Assert.Equal(2, donor.WithBoardIndex(4).ExtraDieTurnsRemaining);
        Assert.Equal(2, donor.WithChildCount(1).ExtraDieTurnsRemaining);
        Assert.Equal(2, donor.WithCash(donor.Cash).ExtraDieTurnsRemaining);

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            donor.WithExtraDieTurns(-1));

        Assert.Equal("extraDieTurnsRemaining", exception.ParamName);
        Assert.Contains("Extra die turns cannot be negative.", exception.Message, StringComparison.Ordinal);
        Assert.Equal(2, donor.ExtraDieTurnsRemaining);
    }

    private static Game Table()
    {
        return Game.Create(GameId, [new PlayerSetup("Ava", "night-baker", TokenColour.Amber)]);
    }

    private static Player Baker()
    {
        return Player.Create("Ava", ProfessionCatalog.GetById("night-baker"), TokenColour.Amber);
    }
}
