namespace RatRaceEscape.Engine.Tests;

public class StockSellTests
{
    private static readonly Guid GameId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    [Fact]
    public void Selling_into_the_bid_pays_the_card_price_and_discards_it()
    {
        Game bid = BidOnPier(shares: 4);
        Player before = bid.CurrentPlayer;

        Game sold = bid.SellShares(4);
        Game closed = sold.EndTurn();

        Assert.Empty(sold.CurrentPlayer.BalanceSheet.Stocks);
        Assert.Equal(before.Cash + Money.FromDollars(160), sold.CurrentPlayer.Cash);
        Assert.Equal(Money.Zero, IncomeStatement.For(sold.CurrentPlayer).PassiveIncome);
        Assert.Equal(before.BalanceSheet.TotalLiabilities, sold.CurrentPlayer.BalanceSheet.TotalLiabilities);
        Assert.Null(sold.Drawn);
        Assert.Null(sold.PendingDeck);
        Assert.Equal(TurnPhase.AwaitingEnd, sold.TurnPhase);
        Assert.Equal(4, Assert.Single(bid.CurrentPlayer.BalanceSheet.Stocks).Shares);
        Assert.Equal(1, sold.Piles.Market.DiscardCount);
        Assert.Equal(1, closed.Piles.Market.DiscardCount);
        TurnException again = Assert.Throws<TurnException>(() => sold.SellShares(1));
        Assert.Contains("There is no share bid to sell against.", again.Errors);
    }

    [Fact]
    public void A_partial_sale_keeps_the_remaining_shares()
    {
        Game bid = BidOnPier(shares: 4);

        Game sold = bid.SellShares(1);

        StockHolding lot = Assert.Single(sold.CurrentPlayer.BalanceSheet.Stocks);
        Assert.Equal(3, lot.Shares);
        Assert.Equal(Money.FromDollars(25), lot.CostPerShare);
        Assert.Equal(Money.FromDollars(1), lot.MonthlyDividendPerShare);
        Assert.Equal(Money.FromDollars(840), sold.CurrentPlayer.Cash);
        Assert.Equal(Money.FromDollars(3), IncomeStatement.For(sold.CurrentPlayer).PassiveIncome);
        Assert.Null(sold.Drawn);
        Assert.Equal(1, sold.Piles.Market.DiscardCount);
        Assert.Equal("lumen-bid", sold.Piles.Market.Draw().Card.Id);
    }

    [Fact]
    public void Selling_more_shares_than_owned_leaves_the_bid_on_the_table()
    {
        Game bid = BidOnPier(shares: 2);

        TurnException over = Assert.Throws<TurnException>(() => bid.SellShares(3));

        Assert.Contains("The player does not have that many shares.", over.Errors);
        Assert.Equal("pier-bid", bid.Drawn!.Id);
        Assert.Equal(2, Assert.Single(bid.CurrentPlayer.BalanceSheet.Stocks).Shares);
        Assert.Equal(Money.FromDollars(850), bid.CurrentPlayer.Cash);
        Assert.Equal(0, bid.Piles.Market.DiscardCount);
    }

    [Fact]
    public void A_bid_for_a_stock_the_player_does_not_own_cannot_be_filled()
    {
        Game bid = Open()
            .Roll(new ScriptedDieRoller([2]))
            .Resolve(SpaceChoice.SmallDeal)
            .Buy(2)
            .EndTurn()
            .Roll(new ScriptedDieRoller([3]))
            .Resolve();

        TurnException missing = Assert.Throws<TurnException>(() => bid.SellShares(1));

        Assert.Equal("LUMEN", Assert.Single(bid.CurrentPlayer.BalanceSheet.Stocks).Symbol);
        Assert.Equal("pier-bid", bid.Drawn!.Id);
        Assert.Contains("The player does not own that stock.", missing.Errors);
        Assert.Equal(Money.FromDollars(880), bid.CurrentPlayer.Cash);
        Assert.Equal(0, bid.Piles.Market.DiscardCount);
    }

    [Fact]
    public void A_whole_asset_a_deal_and_a_bill_are_not_share_bids()
    {
        Game studio = CutMarket("cedar-lane-offer").Roll(new ScriptedDieRoller([5])).Resolve();
        Game deal = Open().Roll(new ScriptedDieRoller([2])).Resolve(SpaceChoice.SmallDeal);
        Game bill = Open().Roll(new ScriptedDieRoller([1])).Resolve();

        TurnException property = Assert.Throws<TurnException>(() => studio.SellShares(1));
        TurnException stock = Assert.Throws<TurnException>(() => deal.SellShares(1));
        TurnException doodad = Assert.Throws<TurnException>(() => bill.SellShares(1));

        Assert.Contains("There is no share bid to sell against.", property.Errors);
        Assert.Contains("There is no share bid to sell against.", stock.Errors);
        Assert.Contains("There is no share bid to sell against.", doodad.Errors);
        Assert.Equal("cedar-lane-offer", studio.Drawn!.Id);
        Assert.NotNull(deal.Drawn);
        Assert.NotNull(bill.Drawn);
    }

    [Fact]
    public void Selling_before_the_bid_is_drawn_is_rejected()
    {
        Game waiting = Open();
        Game rolled = waiting.Roll(new ScriptedDieRoller([5]));
        Game bid = BidOnPier(shares: 1);

        TurnException early = Assert.Throws<TurnException>(() => waiting.SellShares(1));
        TurnException unresolved = Assert.Throws<TurnException>(() => rolled.SellShares(1));
        TurnException none = Assert.Throws<TurnException>(() => bid.SellShares(0));

        Assert.Contains("Roll the dice before selling shares.", early.Errors);
        Assert.Contains("Resolve the space before selling shares.", unresolved.Errors);
        Assert.Contains("Stock shares must be greater than zero.", none.Errors);
        Assert.Equal(TurnPhase.AwaitingResolve, rolled.TurnPhase);
        Assert.NotNull(bid.Drawn);
    }

    private static Game BidOnPier(int shares)
    {
        return CutSmall("north-pier")
            .Roll(new ScriptedDieRoller([2]))
            .Resolve(SpaceChoice.SmallDeal)
            .Buy(shares)
            .EndTurn()
            .Roll(new ScriptedDieRoller([3]))
            .Resolve();
    }

    private static Game Open()
    {
        return Game.Create(GameId, [new PlayerSetup("Ava", "night-baker", TokenColour.Amber)]);
    }

    private static Game CutSmall(string smallDealId)
    {
        int[] keep = [0, 1, 2, 3, 4, 5, 6, 7];
        return Game.Create(
            GameId,
            [new PlayerSetup("Ava", "night-baker", TokenColour.Amber)],
            new ScriptedShuffle(Promote(SmallDealCatalog.All, smallDealId), keep, keep, keep));
    }

    private static Game CutMarket(string marketId)
    {
        int[] keep = [0, 1, 2, 3, 4, 5, 6, 7];
        return Game.Create(
            GameId,
            [new PlayerSetup("Ava", "night-baker", TokenColour.Amber)],
            new ScriptedShuffle(keep, keep, Promote(MarketCatalog.All, marketId), keep));
    }

    private static int[] Promote<T>(IReadOnlyList<T> cards, string id)
        where T : class
    {
        int index = -1;
        for (int card = 0; card < cards.Count; card++)
        {
            string cardId = cards[card] switch
            {
                SmallDeal small => small.Id,
                MarketCard market => market.Id,
                _ => throw new InvalidOperationException("Only a small deal or a market card can be cut."),
            };
            if (cardId == id)
            {
                index = card;
                break;
            }
        }

        if (index < 0)
        {
            throw new InvalidOperationException($"No card with id '{id}'.");
        }

        int[] order = new int[cards.Count];
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

        return order;
    }
}
