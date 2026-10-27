namespace RatRaceEscape.Engine.Tests;

public class StockBuyTests
{
    private static readonly Guid GameId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    [Fact]
    public void Buying_the_drawn_stock_uses_the_card_price_and_discards_it()
    {
        Game drawn = CutTo("north-pier").Roll(new ScriptedDieRoller([2])).Resolve(SpaceChoice.SmallDeal);
        Money cash = drawn.CurrentPlayer.Cash;

        Game bought = drawn.BuyShares(2);
        Game closed = bought.EndTurn();

        Assert.Equal("PIER", bought.CurrentPlayer.BalanceSheet.Stocks[0].Symbol);
        Assert.Equal(2, bought.CurrentPlayer.BalanceSheet.Stocks[0].Shares);
        Assert.Equal(Money.FromDollars(1), bought.CurrentPlayer.BalanceSheet.Stocks[0].MonthlyDividendPerShare);
        Assert.Equal(cash - Money.FromDollars(50), bought.CurrentPlayer.Cash);
        Assert.Equal(Money.FromDollars(2), IncomeStatement.For(bought.CurrentPlayer).PassiveIncome);
        Assert.Null(bought.Drawn);
        Assert.Null(bought.PendingDeck);
        Assert.Equal(TurnPhase.AwaitingEnd, bought.TurnPhase);
        Assert.Equal(cash, drawn.CurrentPlayer.Cash);
        Assert.Equal(1, bought.Piles.SmallDeals.DiscardCount);
        Assert.Equal(1, closed.Piles.SmallDeals.DiscardCount);
        Assert.NotNull(drawn.Drawn);
        TurnException again = Assert.Throws<TurnException>(() => bought.BuyShares(1));
        Assert.Contains("There is no stock offer to buy.", again.Errors);
    }

    [Fact]
    public void Ninety_shares_of_lumen_glass_spend_the_whole_savings()
    {
        Game drawn = Solo().Roll(new ScriptedDieRoller([2])).Resolve(SpaceChoice.SmallDeal);

        Game bought = drawn.BuyShares(90);
        TurnException over = Assert.Throws<TurnException>(() => drawn.BuyShares(91));

        Assert.Equal(Money.Zero, bought.CurrentPlayer.Cash);
        Assert.Equal(90, bought.CurrentPlayer.BalanceSheet.Stocks[0].Shares);
        Assert.Equal(drawn.CurrentPlayer.BalanceSheet.TotalAssets, bought.CurrentPlayer.BalanceSheet.TotalAssets);
        Assert.Contains("Cash cannot cover the stock purchase.", over.Errors);
        Assert.Equal(Money.FromCents(90_000), drawn.CurrentPlayer.Cash);
        Assert.NotNull(drawn.Drawn);
        Assert.Equal(0, drawn.Piles.SmallDeals.DiscardCount);
    }

    [Fact]
    public void A_property_a_big_deal_or_the_market_is_not_a_stock_offer()
    {
        Game property = CutTo("cedar-lane-studio").Roll(new ScriptedDieRoller([2])).Resolve(SpaceChoice.SmallDeal);
        Game big = Solo().Roll(new ScriptedDieRoller([2])).Resolve(SpaceChoice.BigDeal);
        Game market = Solo().Roll(new ScriptedDieRoller([5])).Resolve();

        TurnException small = Assert.Throws<TurnException>(() => property.BuyShares(1));
        TurnException larger = Assert.Throws<TurnException>(() => big.BuyShares(1));
        TurnException offered = Assert.Throws<TurnException>(() => market.BuyShares(1));

        Assert.Contains("There is no stock offer to buy.", small.Errors);
        Assert.Contains("There is no stock offer to buy.", larger.Errors);
        Assert.Contains("There is no stock offer to buy.", offered.Errors);
        Assert.NotNull(property.Drawn);
        Assert.Empty(property.CurrentPlayer.BalanceSheet.Stocks);
        Assert.Equal(Money.FromCents(90_000), property.CurrentPlayer.Cash);
    }

    [Fact]
    public void Buying_before_the_card_is_drawn_is_rejected()
    {
        Game waiting = Solo();
        Game rolled = waiting.Roll(new ScriptedDieRoller([2]));

        TurnException early = Assert.Throws<TurnException>(() => waiting.BuyShares(1));
        TurnException unresolved = Assert.Throws<TurnException>(() => rolled.BuyShares(1));
        TurnException none = Assert.Throws<TurnException>(() =>
            waiting.Roll(new ScriptedDieRoller([2])).Resolve(SpaceChoice.SmallDeal).BuyShares(0));

        Assert.Contains("Roll the dice before buying shares.", early.Errors);
        Assert.Contains("Resolve the space before buying shares.", unresolved.Errors);
        Assert.Contains("Stock shares must be greater than zero.", none.Errors);
        Assert.Equal(TurnPhase.AwaitingResolve, rolled.TurnPhase);
    }

    private static Game Solo()
    {
        return Game.Create(GameId, [new PlayerSetup("Ava", "night-baker", TokenColour.Amber)]);
    }

    private static Game CutTo(string smallDealId)
    {
        int index = 0;
        for (int card = 0; card < SmallDealCatalog.All.Count; card++)
        {
            if (SmallDealCatalog.All[card].Id == smallDealId)
            {
                index = card;
                break;
            }
        }

        int[] order = new int[SmallDealCatalog.All.Count];
        order[0] = index;
        int cursor = 1;
        for (int card = 0; card < order.Length; card++)
        {
            if (card == index)
            {
                continue;
            }

            order[cursor] = card;
            cursor++;
        }

        int[] keep = [0, 1, 2, 3, 4, 5, 6, 7];
        return Game.Create(
            GameId,
            [new PlayerSetup("Ava", "night-baker", TokenColour.Amber)],
            new ScriptedShuffle(order, keep, keep, keep));
    }
}
