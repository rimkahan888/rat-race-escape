namespace RatRaceEscape.Engine.Tests;

public class PaydayTests
{
    private static readonly Guid GameId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    [Fact]
    public void Landing_on_payday_collects_monthly_cash_flow_once()
    {
        Game start = Baker();
        Game landed = start.Roll(new ScriptedDieRoller([6]));
        Game resolved = landed.Resolve();

        Assert.Equal(6, landed.CurrentPlayer.BoardIndex);
        Assert.Equal(Money.FromCents(63_000), CashFlow.For(start.CurrentPlayer).Monthly);
        Assert.Equal(Money.FromCents(63_000), landed.LastRoll!.PaydayCash);
        Assert.Equal(Money.FromCents(153_000), landed.CurrentPlayer.Cash);
        Assert.Equal(landed.CurrentPlayer.Cash, resolved.CurrentPlayer.Cash);
        Assert.Equal(Money.FromCents(90_000), start.CurrentPlayer.Cash);
    }

    [Fact]
    public void Passing_payday_collects_once_even_when_the_token_stops_later()
    {
        Game passed = Baker().Roll(new ScriptedDieRoller([5])).Resolve().EndTurn().Roll(new ScriptedDieRoller([2]));

        Assert.Equal(7, passed.CurrentPlayer.BoardIndex);
        Assert.Equal(SpaceKind.Deal, RatRaceBoard.At(passed.CurrentPlayer.BoardIndex).Kind);
        Assert.Equal([6], passed.LastRoll!.Move.Paydays.Select(step => step.Index));
        Assert.Equal(Money.FromCents(63_000), passed.LastRoll.PaydayCash);
        Assert.Equal(Money.FromCents(153_000), passed.CurrentPlayer.Cash);
        Assert.Equal(passed.CurrentPlayer.Cash, passed.Resolve(SpaceChoice.SmallDeal).CurrentPlayer.Cash);
    }

    [Fact]
    public void Leaving_a_payday_does_not_pay_for_the_square_the_token_was_on()
    {
        Game first = Baker().Roll(new ScriptedDieRoller([6])).Resolve().EndTurn();
        Game second = first.Roll(new ScriptedDieRoller([6]));

        Assert.Equal(6, first.CurrentPlayer.BoardIndex);
        Assert.Equal(Money.FromCents(153_000), first.CurrentPlayer.Cash);
        Assert.Equal(12, second.CurrentPlayer.BoardIndex);
        Assert.Equal([12], second.LastRoll!.Move.Paydays.Select(step => step.Index));
        Assert.Equal(Money.FromCents(63_000), second.LastRoll.PaydayCash);
        Assert.Equal(Money.FromCents(216_000), second.CurrentPlayer.Cash);
    }

    [Fact]
    public void A_roll_that_misses_payday_pays_nothing()
    {
        Game missed = Baker().Roll(new ScriptedDieRoller([1]));

        Assert.Empty(missed.LastRoll!.Move.Paydays);
        Assert.Equal(Money.Zero, missed.LastRoll.PaydayCash);
        Assert.Equal(Money.FromCents(90_000), missed.CurrentPlayer.Cash);
    }

    [Fact]
    public void Only_the_player_who_passes_payday_is_paid()
    {
        Game game = Game.Create(
            GameId,
            [
                new PlayerSetup("Ava", "night-baker", TokenColour.Amber),
                new PlayerSetup("Bo", "municipal-archivist", TokenColour.Cobalt),
            ]);
        Money boCash = game.Players[1].Cash;

        Game avaPaid = game.Roll(new ScriptedDieRoller([6]));

        Assert.Equal(Money.FromCents(153_000), avaPaid.Players[0].Cash);
        Assert.Equal(boCash, avaPaid.Players[1].Cash);

        Game boPaid = avaPaid.Resolve().EndTurn().Roll(new ScriptedDieRoller([6]));

        Assert.Equal(Money.FromCents(153_000), boPaid.Players[0].Cash);
        Assert.Equal(boCash + CashFlow.For(game.Players[1]).Monthly, boPaid.Players[1].Cash);
        Assert.Equal(Money.FromCents(55_000), boPaid.LastRoll!.PaydayCash);
    }

    [Fact]
    public void Two_paydays_pay_twice_the_monthly_cash_flow()
    {
        Player baker = BakerPlayer();

        Player paid = Payday.Collect(baker, 2);

        Assert.Equal(baker.Cash + (CashFlow.For(baker).Monthly * 2), paid.Cash);
        Assert.Equal(Money.FromCents(90_000 + (63_000 * 2)), paid.Cash);
    }

    [Fact]
    public void No_payday_leaves_the_same_player()
    {
        Player baker = BakerPlayer();

        Assert.Same(baker, Payday.Collect(baker, 0));
    }

    [Fact]
    public void A_month_below_zero_pays_nothing_and_leaves_the_cash()
    {
        Player parent = BakerPlayer().WithChildCount(3);
        Player broke = parent.WithCash(Money.FromCents(20_999));

        Assert.Equal(Money.Zero, CashFlow.For(parent).Monthly);
        Assert.Equal(Money.FromCents(-21_000), parent.Profession.MonthlyCashFlow(3));

        Player paid = Payday.Collect(parent, 1);
        Player still = Payday.Collect(broke, 2);

        Assert.Equal(parent.Cash, paid.Cash);
        Assert.Equal(broke.Cash, still.Cash);
        Assert.Equal(3, paid.ChildCount);
        Assert.Equal(3, still.ChildCount);
    }

    [Fact]
    public void Collect_rejects_a_null_player_or_a_negative_count()
    {
        Assert.Throws<ArgumentNullException>(() => Payday.Collect(null!, 1));

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            Payday.Collect(BakerPlayer(), -1));

        Assert.Equal("paydayCount", exception.ParamName);
        Assert.Contains("Payday count cannot be negative.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void WithCash_replaces_savings_and_rejects_a_negative_amount()
    {
        Player baker = BakerPlayer();

        Player paid = baker.WithCash(Money.FromCents(20_999));

        Assert.Equal(Money.FromCents(20_999), paid.Cash);
        Assert.Equal(baker.BoardIndex, paid.BoardIndex);
        Assert.Equal(baker.ChildCount, paid.ChildCount);
        Assert.Throws<BalanceSheetValidationException>(() => baker.WithCash(Money.FromCents(-1)));
    }

    private static Game Baker()
    {
        return Game.Create(GameId, [new PlayerSetup("Ava", "night-baker", TokenColour.Amber)]);
    }

    private static Player BakerPlayer()
    {
        return Player.Create("Ava", ProfessionCatalog.GetById("night-baker"), TokenColour.Amber);
    }
}
