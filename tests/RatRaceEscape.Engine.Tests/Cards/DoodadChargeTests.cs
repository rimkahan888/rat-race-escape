namespace RatRaceEscape.Engine.Tests;

public class DoodadChargeTests
{
    [Fact]
    public void Pay_takes_the_cost_and_leaves_a_short_player_unchanged()
    {
        Player baker = Player.Create("Ava", ProfessionCatalog.GetById("night-baker"), TokenColour.Amber);
        Doodad roof = DoodadCatalog.GetById("roof-leak-patch");
        Player shortCash = baker.WithCash(Money.FromDollars(399));

        TurnException exception = Assert.Throws<TurnException>(() => DoodadCharge.Pay(shortCash, roof));
        Player paid = DoodadCharge.Pay(baker.WithCash(roof.Cost), roof);

        Assert.Contains("Cash cannot cover the doodad.", exception.Errors);
        Assert.Equal(Money.FromDollars(399), shortCash.Cash);
        Assert.Equal(Money.Zero, paid.Cash);
        Assert.Equal(baker.BoardIndex, paid.BoardIndex);
    }

    [Fact]
    public void Pay_rejects_a_null_player_or_card()
    {
        Player baker = Player.Create("Ava", ProfessionCatalog.GetById("night-baker"), TokenColour.Amber);
        Doodad fine = DoodadCatalog.GetById("harbor-parking-fine");

        Assert.Throws<ArgumentNullException>(() => DoodadCharge.Pay(null!, fine));
        Assert.Throws<ArgumentNullException>(() => DoodadCharge.Pay(baker, null!));
    }
}
