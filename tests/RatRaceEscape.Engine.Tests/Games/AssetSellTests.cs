namespace RatRaceEscape.Engine.Tests;

public class AssetSellTests
{
    private static readonly Guid GameId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public void Selling_the_offered_studio_pays_the_card_and_drops_its_mortgage()
    {
        Game offer = OfferedStudio();
        Player before = offer.CurrentPlayer;
        Money expenses = IncomeStatement.For(before).TotalExpenses;

        Game sold = offer.SellAsset();
        Game closed = sold.EndTurn();

        Assert.Empty(sold.CurrentPlayer.BalanceSheet.RealEstate);
        Assert.Equal(before.Cash + Money.FromDollars(2_500), sold.CurrentPlayer.Cash);
        Assert.Equal(before.BalanceSheet.Mortgage, sold.CurrentPlayer.BalanceSheet.Mortgage);
        Assert.Equal(before.BalanceSheet.TotalLiabilities - Money.FromDollars(4_500), sold.CurrentPlayer.BalanceSheet.TotalLiabilities);
        Assert.Equal(Money.Zero, IncomeStatement.For(sold.CurrentPlayer).PassiveIncome);
        Assert.Equal(expenses, IncomeStatement.For(sold.CurrentPlayer).TotalExpenses);
        Assert.Null(sold.Drawn);
        Assert.Null(sold.PendingDeck);
        Assert.Equal(TurnPhase.AwaitingEnd, sold.TurnPhase);
        Assert.Single(offer.CurrentPlayer.BalanceSheet.RealEstate);
        Assert.Equal(1, sold.Piles.Market.DiscardCount);
        Assert.Equal(1, closed.Piles.Market.DiscardCount);
        Assert.Equal("pier-bid", sold.Piles.Market.Draw().Card.Id);
        TurnException again = Assert.Throws<TurnException>(() => sold.SellAsset());
        Assert.Contains("There is no asset offer to sell.", again.Errors);
    }

    [Fact]
    public void Selling_the_offered_workshop_pays_the_card_and_drops_its_note()
    {
        Game offer = OfferedWorkshop();
        Player before = offer.CurrentPlayer;

        Game sold = offer.SellAsset();

        Assert.Empty(sold.CurrentPlayer.BalanceSheet.Businesses);
        Assert.Equal(before.Cash + Money.FromDollars(6_000), sold.CurrentPlayer.Cash);
        Assert.Equal(before.BalanceSheet.Mortgage, sold.CurrentPlayer.BalanceSheet.Mortgage);
        Assert.Equal(before.BalanceSheet.TotalLiabilities, sold.CurrentPlayer.BalanceSheet.TotalLiabilities);
        Assert.Equal(Money.Zero, IncomeStatement.For(sold.CurrentPlayer).PassiveIncome);
        Assert.Null(sold.Drawn);
        Assert.Equal(1, sold.Piles.Market.DiscardCount);
        Assert.Equal("Kiln and Clay", Assert.Single(offer.CurrentPlayer.BalanceSheet.Businesses).Name);
    }

    [Fact]
    public void An_offer_for_an_asset_the_player_does_not_own_stays_on_the_table()
    {
        Game offer = CutMarket("cedar-lane-offer").Roll(new ScriptedDieRoller([5])).Resolve();

        TurnException missing = Assert.Throws<TurnException>(() => offer.SellAsset());

        Assert.Contains("The player does not own that asset.", missing.Errors);
        Assert.Equal("cedar-lane-offer", offer.Drawn!.Id);
        Assert.Empty(offer.CurrentPlayer.BalanceSheet.RealEstate);
        Assert.Equal(Money.FromDollars(900), offer.CurrentPlayer.Cash);
        Assert.Equal(0, offer.Piles.Market.DiscardCount);
    }

    [Fact]
    public void A_share_bid_a_deal_and_a_bill_are_not_asset_offers()
    {
        Game shares = Open().Roll(new ScriptedDieRoller([5])).Resolve();
        Game deal = Open().Roll(new ScriptedDieRoller([2])).Resolve(SpaceChoice.SmallDeal);
        Game bill = Open().Roll(new ScriptedDieRoller([1])).Resolve();

        TurnException bid = Assert.Throws<TurnException>(() => shares.SellAsset());
        TurnException stock = Assert.Throws<TurnException>(() => deal.SellAsset());
        TurnException doodad = Assert.Throws<TurnException>(() => bill.SellAsset());

        Assert.Contains("There is no asset offer to sell.", bid.Errors);
        Assert.Contains("There is no asset offer to sell.", stock.Errors);
        Assert.Contains("There is no asset offer to sell.", doodad.Errors);
        Assert.Equal("pier-bid", shares.Drawn!.Id);
        Assert.NotNull(deal.Drawn);
        Assert.NotNull(bill.Drawn);
    }

    [Fact]
    public void Selling_before_the_offer_is_drawn_is_rejected()
    {
        Game waiting = Open();
        Game rolled = waiting.Roll(new ScriptedDieRoller([5]));

        TurnException early = Assert.Throws<TurnException>(() => waiting.SellAsset());
        TurnException unresolved = Assert.Throws<TurnException>(() => rolled.SellAsset());

        Assert.Contains("Roll the dice before selling.", early.Errors);
        Assert.Contains("Resolve the space before selling.", unresolved.Errors);
        Assert.Equal(TurnPhase.AwaitingResolve, rolled.TurnPhase);
        Assert.Null(rolled.Drawn);
    }

    private static Game OfferedStudio()
    {
        int[] keep = [0, 1, 2, 3, 4, 5, 6, 7];
        return Game.Create(
                GameId,
                [new PlayerSetup("Ava", "night-baker", TokenColour.Amber)],
                new ScriptedShuffle(
                    Promote(SmallDealCatalog.All, "cedar-lane-studio"),
                    keep,
                    Promote(MarketCatalog.All, "cedar-lane-offer"),
                    keep))
            .Roll(new ScriptedDieRoller([2]))
            .Resolve(SpaceChoice.SmallDeal)
            .Buy(shares: null)
            .EndTurn()
            .Roll(new ScriptedDieRoller([3]))
            .Resolve();
    }

    private static Game OfferedWorkshop()
    {
        int[] keep = [0, 1, 2, 3, 4, 5, 6, 7];
        return Game.Create(
                GameId,
                [new PlayerSetup("Nia", "reef-surveyor", TokenColour.Violet)],
                new ScriptedShuffle(
                    keep,
                    Promote(BigDealCatalog.All, "kiln-and-clay"),
                    Promote(MarketCatalog.All, "kiln-and-clay-offer"),
                    keep))
            .Roll(new ScriptedDieRoller([2]))
            .Resolve(SpaceChoice.BigDeal)
            .Buy(shares: null)
            .EndTurn()
            .Roll(new ScriptedDieRoller([3]))
            .Resolve();
    }

    private static Game Open()
    {
        return Game.Create(GameId, [new PlayerSetup("Ava", "night-baker", TokenColour.Amber)]);
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
                BigDeal big => big.Id,
                MarketCard market => market.Id,
                _ => throw new InvalidOperationException("Only a deal or a market card can be cut."),
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
