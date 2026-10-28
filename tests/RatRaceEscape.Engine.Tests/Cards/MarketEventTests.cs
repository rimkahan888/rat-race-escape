namespace RatRaceEscape.Engine.Tests;

public class MarketEventTests
{
    [Fact]
    public void A_payment_reaches_every_player_and_a_charge_does_too()
    {
        Player baker = Baker();
        Player archivist = Archivist();
        Money bakerCash = baker.Cash;
        Money archivistCash = archivist.Cash;

        Player[] paid = MarketEvent.Apply([baker, archivist], Money.FromDollars(200));
        Player[] billed = MarketEvent.Apply([baker, archivist], Money.FromDollars(-150));

        Assert.Equal(bakerCash + Money.FromDollars(200), paid[0].Cash);
        Assert.Equal(archivistCash + Money.FromDollars(200), paid[1].Cash);
        Assert.Equal(bakerCash - Money.FromDollars(150), billed[0].Cash);
        Assert.Equal(archivistCash - Money.FromDollars(150), billed[1].Cash);
        Assert.Equal(bakerCash, baker.Cash);
        Assert.Equal(archivistCash, archivist.Cash);
        Assert.Equal(baker.BoardIndex, paid[0].BoardIndex);
    }

    [Fact]
    public void One_short_player_stops_the_charge_for_the_whole_table()
    {
        Player baker = Baker().WithCash(Money.FromDollars(100));
        Player archivist = Archivist();
        Money archivistCash = archivist.Cash;

        TurnException exception = Assert.Throws<TurnException>(() =>
            MarketEvent.Apply([baker, archivist], Money.FromDollars(-150)));

        Assert.Contains("Cash cannot cover the market event.", exception.Errors);
        Assert.Equal(Money.FromDollars(100), baker.Cash);
        Assert.Equal(archivistCash, archivist.Cash);
    }

    [Fact]
    public void Apply_rejects_a_zero_delta_and_a_null_table()
    {
        TurnException exception = Assert.Throws<TurnException>(() =>
            MarketEvent.Apply([Baker()], Money.Zero));

        Assert.Contains("An event must change everyone's cash.", exception.Errors);
        Assert.Throws<ArgumentNullException>(() => MarketEvent.Apply(null!, Money.FromDollars(1)));
    }

    private static Player Baker()
    {
        return Player.Create("Ava", ProfessionCatalog.GetById("night-baker"), TokenColour.Amber);
    }

    private static Player Archivist()
    {
        return Player.Create("Bo", ProfessionCatalog.GetById("municipal-archivist"), TokenColour.Cobalt);
    }
}
