namespace RatRaceEscape.Engine.Tests;

public class RatRaceExitTests
{
    [Fact]
    public void Passive_income_over_the_bills_moves_the_token_to_the_fast_track()
    {
        Player baker = Baker();
        Money expenses = IncomeStatement.For(baker).TotalExpenses;
        Player ready = Buy(baker, expenses + Money.FromCents(1));

        Assert.True(RatRaceExit.CanLeave(ready));
        Player left = RatRaceExit.Leave(ready);

        Assert.Equal(BoardTrack.FastTrack, left.Track);
        Assert.Equal(0, left.BoardIndex);
        Assert.Equal(ready.Cash, left.Cash);
        Assert.Equal(BoardTrack.RatRace, ready.Track);
        Assert.False(RatRaceExit.CanLeave(baker));
    }

    [Fact]
    public void Passive_income_equal_to_the_bills_does_not_leave()
    {
        Player baker = Baker();
        Money expenses = IncomeStatement.For(baker).TotalExpenses;
        Player even = Buy(baker, expenses);

        Assert.False(even.BalanceSheet.TotalAssets.IsNegative);
        Assert.False(RatRaceExit.CanLeave(even));
        TurnException exception = Assert.Throws<TurnException>(() => RatRaceExit.Leave(even));

        Assert.Contains("Passive income does not yet cover the bills.", exception.Errors);
        Assert.Equal(BoardTrack.RatRace, even.Track);
    }

    [Fact]
    public void A_bankrupt_player_and_a_player_already_out_cannot_leave()
    {
        Player baker = Baker();
        Player left = RatRaceExit.Leave(Buy(baker, IncomeStatement.For(baker).TotalExpenses + Money.FromCents(1)));
        Player broke = Bankruptcy.Apply(baker);

        TurnException again = Assert.Throws<TurnException>(() => RatRaceExit.Leave(left));
        TurnException bankrupt = Assert.Throws<TurnException>(() => RatRaceExit.Leave(broke));

        Assert.Contains("This player has already left the rat race.", again.Errors);
        Assert.Contains("A bankrupt player cannot leave the rat race.", bankrupt.Errors);
        Assert.False(RatRaceExit.CanLeave(broke));
        Assert.False(RatRaceExit.CanLeave(left));
        Assert.Throws<ArgumentNullException>(() => RatRaceExit.Leave(null!));
        Assert.Throws<ArgumentNullException>(() => RatRaceExit.CanLeave(null!));
    }

    [Fact]
    public void The_fast_track_index_stops_at_the_end_of_that_board()
    {
        Player baker = Baker();
        Player left = baker.WithTrack(BoardTrack.FastTrack, 11);

        Assert.Equal(11, left.BoardIndex);
        Assert.Equal(BoardTrack.RatRace, baker.Track);
        ArgumentOutOfRangeException high = Assert.Throws<ArgumentOutOfRangeException>(() => left.WithBoardIndex(12));
        ArgumentOutOfRangeException off = Assert.Throws<ArgumentOutOfRangeException>(() =>
            baker.WithTrack(BoardTrack.FastTrack, FastTrackBoard.SpaceCount));

        Assert.Equal("boardIndex", high.ParamName);
        Assert.Equal("boardIndex", off.ParamName);
    }

    private static Player Buy(Player player, Money cashFlow)
    {
        return AssetPurchase.BuyBusiness(
            player.WithCash(Money.FromDollars(10)),
            "Tide Pool",
            Money.FromDollars(1),
            Money.Zero,
            cashFlow);
    }

    private static Player Baker()
    {
        return Player.Create("Ava", ProfessionCatalog.GetById("night-baker"), TokenColour.Amber);
    }
}
