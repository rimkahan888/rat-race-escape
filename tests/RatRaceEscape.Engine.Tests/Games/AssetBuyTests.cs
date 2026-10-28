namespace RatRaceEscape.Engine.Tests;

public class AssetBuyTests
{
    private static readonly Guid GameId = Guid.Parse("88888888-8888-8888-8888-888888888888");

    [Fact]
    public void Buying_the_drawn_small_property_uses_the_card_and_discards_it()
    {
        Game drawn = CutSmall("cedar-lane-studio").Roll(new ScriptedDieRoller([2])).Resolve(SpaceChoice.SmallDeal);
        Player before = drawn.CurrentPlayer;
        Money expenses = IncomeStatement.For(before).TotalExpenses;

        Game bought = drawn.BuyAsset();
        Game closed = bought.EndTurn();

        RealEstateHolding property = Assert.Single(bought.CurrentPlayer.BalanceSheet.RealEstate);
        Assert.Equal("Cedar Lane Studio", property.Name);
        Assert.Equal(Money.FromDollars(500), property.Cost);
        Assert.Equal(Money.FromDollars(4_500), property.Mortgage);
        Assert.Equal(Money.FromDollars(20), property.MonthlyCashFlow);
        Assert.Equal(before.Cash - Money.FromDollars(500), bought.CurrentPlayer.Cash);
        Assert.Equal(before.BalanceSheet.Mortgage, bought.CurrentPlayer.BalanceSheet.Mortgage);
        Assert.Empty(bought.CurrentPlayer.BalanceSheet.OtherLoans);
        Assert.Equal(before.BalanceSheet.TotalAssets, bought.CurrentPlayer.BalanceSheet.TotalAssets);
        Assert.Equal(before.BalanceSheet.TotalLiabilities + Money.FromDollars(4_500), bought.CurrentPlayer.BalanceSheet.TotalLiabilities);
        Assert.Equal(Money.FromDollars(20), IncomeStatement.For(bought.CurrentPlayer).PassiveIncome);
        Assert.Equal(expenses, IncomeStatement.For(bought.CurrentPlayer).TotalExpenses);
        Assert.Null(bought.Drawn);
        Assert.Null(bought.PendingDeck);
        Assert.Equal(TurnPhase.AwaitingEnd, bought.TurnPhase);
        Assert.Equal(before.Cash, drawn.CurrentPlayer.Cash);
        Assert.Equal(1, bought.Piles.SmallDeals.DiscardCount);
        Assert.Equal(1, closed.Piles.SmallDeals.DiscardCount);
        TurnException again = Assert.Throws<TurnException>(() => bought.BuyAsset());
        Assert.Contains("There is no offer to buy.", again.Errors);
    }

    [Fact]
    public void A_small_property_the_player_cannot_afford_stays_on_the_table()
    {
        Game drawn = CutSmall("maple-court-cottage").Roll(new ScriptedDieRoller([2])).Resolve(SpaceChoice.SmallDeal);

        TurnException over = Assert.Throws<TurnException>(() => drawn.BuyAsset());

        Assert.Contains("Cash cannot cover the down payment.", over.Errors);
        Assert.Equal("maple-court-cottage", drawn.Drawn!.Id);
        Assert.Equal(Money.FromDollars(900), drawn.CurrentPlayer.Cash);
        Assert.Empty(drawn.CurrentPlayer.BalanceSheet.RealEstate);
        Assert.Equal(0, drawn.Piles.SmallDeals.DiscardCount);
    }

    [Fact]
    public void A_negative_small_property_lowers_income_without_a_new_expense()
    {
        Game drawn = CutSmall("maple-court-cottage", "Bo", "municipal-archivist", TokenColour.Cobalt)
            .Roll(new ScriptedDieRoller([2]))
            .Resolve(SpaceChoice.SmallDeal);
        Money expenses = IncomeStatement.For(drawn.CurrentPlayer).TotalExpenses;
        Money cashFlow = CashFlow.For(drawn.CurrentPlayer).Monthly;

        Game bought = drawn.Buy(shares: null);

        Assert.Equal(Money.FromDollars(-10), IncomeStatement.For(bought.CurrentPlayer).PassiveIncome);
        Assert.Equal(expenses, IncomeStatement.For(bought.CurrentPlayer).TotalExpenses);
        Assert.Equal(cashFlow - Money.FromDollars(10), CashFlow.For(bought.CurrentPlayer).Monthly);
        Assert.Equal(Money.FromDollars(200), bought.CurrentPlayer.Cash);
    }

    [Fact]
    public void Buying_the_drawn_big_property_adds_its_mortgage_and_cash_flow()
    {
        Game drawn = Open("Nia", "reef-surveyor", TokenColour.Violet)
            .Roll(new ScriptedDieRoller([2]))
            .Resolve(SpaceChoice.BigDeal);
        Money liabilities = drawn.CurrentPlayer.BalanceSheet.TotalLiabilities;

        Game bought = drawn.BuyAsset();

        RealEstateHolding property = Assert.Single(bought.CurrentPlayer.BalanceSheet.RealEstate);
        Assert.Equal("brickworks-lofts", drawn.Drawn!.Id);
        Assert.Equal("Brickworks Lofts", property.Name);
        Assert.Equal(Money.FromDollars(4_000), property.Cost);
        Assert.Equal(Money.FromDollars(28_000), property.Mortgage);
        Assert.Equal(Money.FromDollars(1_400), bought.CurrentPlayer.Cash);
        Assert.Equal(liabilities + Money.FromDollars(28_000), bought.CurrentPlayer.BalanceSheet.TotalLiabilities);
        Assert.Equal(Money.FromDollars(220), IncomeStatement.For(bought.CurrentPlayer).PassiveIncome);
        Assert.Equal(1, bought.Piles.BigDeals.DiscardCount);
        Assert.Empty(bought.CurrentPlayer.BalanceSheet.Businesses);
    }

    [Fact]
    public void Buying_the_drawn_business_adds_its_note_and_cash_flow()
    {
        Game drawn = CutBig("lantern-ferry", "Nia", "reef-surveyor", TokenColour.Violet)
            .Roll(new ScriptedDieRoller([2]))
            .Resolve(SpaceChoice.BigDeal);
        Money home = drawn.CurrentPlayer.BalanceSheet.Mortgage;
        Money liabilities = drawn.CurrentPlayer.BalanceSheet.TotalLiabilities;
        Money expenses = IncomeStatement.For(drawn.CurrentPlayer).TotalExpenses;

        Game bought = drawn.BuyAsset();

        BusinessHolding business = Assert.Single(bought.CurrentPlayer.BalanceSheet.Businesses);
        Assert.Equal("Lantern Ferry", business.Name);
        Assert.Equal(Money.FromDollars(5_000), business.Cost);
        Assert.Equal(Money.FromDollars(15_000), business.Liability);
        Assert.Equal(Money.FromDollars(320), business.MonthlyCashFlow);
        Assert.Equal(Money.FromDollars(400), bought.CurrentPlayer.Cash);
        Assert.Equal(home, bought.CurrentPlayer.BalanceSheet.Mortgage);
        Assert.Empty(bought.CurrentPlayer.BalanceSheet.OtherLoans);
        Assert.Equal(liabilities + Money.FromDollars(15_000), bought.CurrentPlayer.BalanceSheet.TotalLiabilities);
        Assert.Equal(expenses, IncomeStatement.For(bought.CurrentPlayer).TotalExpenses);
        Assert.Equal(Money.FromDollars(320), IncomeStatement.For(bought.CurrentPlayer).PassiveIncome);
        Assert.Equal(1, bought.Piles.BigDeals.DiscardCount);
    }

    [Fact]
    public void A_stock_the_market_and_a_bill_are_not_bought_as_assets()
    {
        Game stock = Open("Ava", "night-baker", TokenColour.Amber)
            .Roll(new ScriptedDieRoller([2]))
            .Resolve(SpaceChoice.SmallDeal);
        Game market = Open("Ava", "night-baker", TokenColour.Amber)
            .Roll(new ScriptedDieRoller([5]))
            .Resolve();
        Game bill = Open("Ava", "night-baker", TokenColour.Amber)
            .Roll(new ScriptedDieRoller([1]))
            .Resolve();

        TurnException shares = Assert.Throws<TurnException>(() => stock.BuyAsset());
        TurnException offered = Assert.Throws<TurnException>(() => market.BuyAsset());
        TurnException paid = Assert.Throws<TurnException>(() => bill.BuyAsset());

        Assert.Contains("This offer is sold in shares.", shares.Errors);
        Assert.Contains("This card is not for sale.", offered.Errors);
        Assert.Contains("This bill is already paid.", paid.Errors);
        Assert.NotNull(stock.Drawn);
        Assert.Equal(Money.FromDollars(900), stock.CurrentPlayer.Cash);
        Assert.Equal(0, stock.Piles.SmallDeals.DiscardCount);
        Assert.Equal(Money.FromDollars(720), bill.CurrentPlayer.Cash);
    }

    [Fact]
    public void Buying_names_a_share_count_only_for_a_stock()
    {
        Game stock = Open("Ava", "night-baker", TokenColour.Amber)
            .Roll(new ScriptedDieRoller([2]))
            .Resolve(SpaceChoice.SmallDeal);
        Game property = CutSmall("cedar-lane-studio").Roll(new ScriptedDieRoller([2])).Resolve(SpaceChoice.SmallDeal);

        TurnException missing = Assert.Throws<TurnException>(() => stock.Buy(shares: null));
        Game bought = stock.Buy(2);
        TurnException counted = Assert.Throws<TurnException>(() => property.Buy(1));

        Assert.Contains("Stock purchases need a share count.", missing.Errors);
        Assert.Equal(2, Assert.Single(bought.CurrentPlayer.BalanceSheet.Stocks).Shares);
        Assert.Equal(Money.FromDollars(880), bought.CurrentPlayer.Cash);
        Assert.Null(bought.Drawn);
        Assert.Contains("This offer is not sold in shares.", counted.Errors);
        Assert.NotNull(property.Drawn);
        Assert.Empty(property.CurrentPlayer.BalanceSheet.RealEstate);
    }

    [Fact]
    public void Passing_discards_the_card_and_does_not_refund_a_bill()
    {
        Game stock = Open("Ava", "night-baker", TokenColour.Amber)
            .Roll(new ScriptedDieRoller([2]))
            .Resolve(SpaceChoice.SmallDeal);
        Game bill = Open("Ava", "night-baker", TokenColour.Amber)
            .Roll(new ScriptedDieRoller([1]))
            .Resolve();
        Game market = Open("Ava", "night-baker", TokenColour.Amber)
            .Roll(new ScriptedDieRoller([5]))
            .Resolve();

        Game passed = stock.Pass();
        Game closed = passed.EndTurn();
        Game unpaid = bill.Pass();
        Game declined = market.Pass();

        Assert.Null(passed.Drawn);
        Assert.Equal(Money.FromDollars(900), passed.CurrentPlayer.Cash);
        Assert.Empty(passed.CurrentPlayer.BalanceSheet.Stocks);
        Assert.Equal(1, passed.Piles.SmallDeals.DiscardCount);
        Assert.Equal(1, closed.Piles.SmallDeals.DiscardCount);
        Assert.Equal(Money.FromDollars(720), unpaid.CurrentPlayer.Cash);
        Assert.Equal(1, unpaid.Piles.Doodads.DiscardCount);
        Assert.Equal(1, declined.Piles.Market.DiscardCount);
        Assert.Null(declined.Drawn);
        TurnException again = Assert.Throws<TurnException>(() => passed.Pass());
        Assert.Contains("There is no card to pass.", again.Errors);
    }

    [Fact]
    public void Buying_or_passing_before_the_card_is_drawn_is_rejected()
    {
        Game waiting = Open("Ava", "night-baker", TokenColour.Amber);
        Game rolled = waiting.Roll(new ScriptedDieRoller([2]));
        Game settled = waiting.Roll(new ScriptedDieRoller([3])).Resolve(SpaceChoice.Decline);

        TurnException earlyBuy = Assert.Throws<TurnException>(() => waiting.BuyAsset());
        TurnException unresolved = Assert.Throws<TurnException>(() => rolled.Buy(shares: null));
        TurnException earlyPass = Assert.Throws<TurnException>(() => waiting.Pass());
        TurnException openPass = Assert.Throws<TurnException>(() => rolled.Pass());
        TurnException noCard = Assert.Throws<TurnException>(() => settled.Pass());
        TurnException noOffer = Assert.Throws<TurnException>(() => settled.BuyAsset());

        Assert.Contains("Roll the dice before buying.", earlyBuy.Errors);
        Assert.Contains("Resolve the space before buying.", unresolved.Errors);
        Assert.Contains("Roll the dice before passing.", earlyPass.Errors);
        Assert.Contains("Resolve the space before passing.", openPass.Errors);
        Assert.Contains("There is no card to pass.", noCard.Errors);
        Assert.Contains("There is no offer to buy.", noOffer.Errors);
        Assert.Equal(TurnPhase.AwaitingResolve, rolled.TurnPhase);
        Assert.Equal(Money.FromDollars(900), settled.CurrentPlayer.Cash);
    }

    private static Game Open(string name, string profession, TokenColour colour)
    {
        return Game.Create(GameId, [new PlayerSetup(name, profession, colour)]);
    }

    private static Game CutSmall(string smallDealId)
    {
        return CutSmall(smallDealId, "Ava", "night-baker", TokenColour.Amber);
    }

    private static Game CutSmall(string smallDealId, string name, string profession, TokenColour colour)
    {
        int[] keep = [0, 1, 2, 3, 4, 5, 6, 7];
        int[] market = Enumerable.Range(0, MarketCatalog.All.Count).ToArray();
        return Game.Create(
            GameId,
            [new PlayerSetup(name, profession, colour)],
            new ScriptedShuffle(Promote(SmallDealCatalog.All, smallDealId), keep, market, keep));
    }

    private static Game CutBig(string bigDealId, string name, string profession, TokenColour colour)
    {
        int[] keep = [0, 1, 2, 3, 4, 5, 6, 7];
        int[] market = Enumerable.Range(0, MarketCatalog.All.Count).ToArray();
        return Game.Create(
            GameId,
            [new PlayerSetup(name, profession, colour)],
            new ScriptedShuffle(keep, Promote(BigDealCatalog.All, bigDealId), market, keep));
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
                _ => throw new InvalidOperationException("Only a deal catalog can be cut."),
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
