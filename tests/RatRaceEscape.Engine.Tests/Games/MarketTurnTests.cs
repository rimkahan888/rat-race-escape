namespace RatRaceEscape.Engine.Tests;

public class MarketTurnTests
{
    private static readonly Guid GameId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void A_cash_event_pays_every_player_and_passing_does_not_take_it_back()
    {
        Game landed = Table().Roll(new ScriptedDieRoller([5])).Resolve();

        Assert.Equal("extra-mill-shift", landed.Drawn!.Id);
        Assert.Equal(MarketKind.Event, landed.Drawn.Market!.Kind);
        Assert.Equal(Money.FromDollars(1_100), landed.Players[0].Cash);
        Assert.Equal(Money.FromDollars(1_600), landed.Players[1].Cash);
        Assert.Equal(0, landed.Players[1].BoardIndex);
        Assert.Equal(TurnPhase.AwaitingEnd, landed.TurnPhase);
        Assert.Equal(0, landed.Piles.Market.DiscardCount);

        Game passed = landed.Pass();

        Assert.Null(passed.Drawn);
        Assert.Equal(Money.FromDollars(1_100), passed.Players[0].Cash);
        Assert.Equal(Money.FromDollars(1_600), passed.Players[1].Cash);
        Assert.Equal(1, passed.Piles.Market.DiscardCount);
    }

    [Fact]
    public void A_charge_is_collected_from_every_player()
    {
        Game landed = CutMarket("storm-week").Roll(new ScriptedDieRoller([5])).Resolve();

        Assert.Equal(Money.FromDollars(750), landed.Players[0].Cash);
        Assert.Equal(Money.FromDollars(1_250), landed.Players[1].Cash);
        Assert.Equal("storm-week", landed.Drawn!.Id);
    }

    [Fact]
    public void A_charge_bigger_than_cash_is_covered_by_a_loan_when_cash_flow_allows()
    {
        MarketCard levy = MarketCard.Event(
            "wharf-levy",
            "Wharf Levy",
            "The wharf levy comes due for every household.",
            Money.FromDollars(-1_000));
        CardPiles piles = CardPiles.Create(new IdentityShuffle())
            .WithMarket(Deck<MarketCard>.Create([levy], new IdentityShuffle()));
        Game landed = Game.Create(GameId, Seats(), piles).Roll(new ScriptedDieRoller([5]));

        Game settled = landed.Resolve();

        Assert.Equal("wharf-levy", settled.Drawn!.Id);
        Assert.Equal(TurnPhase.AwaitingEnd, settled.TurnPhase);
        Assert.False(settled.Players[0].IsBankrupt);
        Assert.Equal(Money.FromDollars(900), settled.Players[0].Cash);
        Assert.Equal(BankLoan.Increment, Assert.Single(settled.Players[0].BalanceSheet.OtherLoans).Balance);
        Assert.False(CashFlow.For(settled.Players[0]).Monthly.IsNegative);
        Assert.False(settled.Players[1].IsBankrupt);
        Assert.Equal(Money.FromDollars(400), settled.Players[1].Cash);
        Assert.Equal(Money.FromDollars(900), landed.Players[0].Cash);
        Assert.Equal(0, settled.Piles.Market.DiscardCount);
    }

    [Fact]
    public void A_split_doubles_the_stock_for_a_player_who_is_not_taking_the_turn()
    {
        Game bought = Cut("north-pier", "pier-split")
            .Roll(new ScriptedDieRoller([2]))
            .Resolve(SpaceChoice.SmallDeal)
            .Buy(4)
            .EndTurn();
        Game landed = bought.Roll(new ScriptedDieRoller([5])).Resolve();

        StockHolding ava = Assert.Single(landed.Players[0].BalanceSheet.Stocks);
        Assert.Equal("Bo", landed.CurrentPlayer.Name);
        Assert.Equal("pier-split", landed.Drawn!.Id);
        Assert.Equal(8, ava.Shares);
        Assert.Equal(Money.FromCents(1_250), ava.CostPerShare);
        Assert.Equal(Money.FromDollars(1), ava.MonthlyDividendPerShare);
        Assert.Equal(Money.FromDollars(8), IncomeStatement.For(landed.Players[0]).PassiveIncome);
        Assert.Equal(Money.FromDollars(800), landed.Players[0].Cash);
        Assert.Empty(landed.Players[1].BalanceSheet.Stocks);
        Assert.Equal(Money.FromDollars(1_400), landed.Players[1].Cash);
        Assert.Equal(4, Assert.Single(bought.Players[0].BalanceSheet.Stocks).Shares);
    }

    [Fact]
    public void A_split_for_a_stock_nobody_holds_changes_no_sheet()
    {
        Game landed = CutMarket("lumen-split").Roll(new ScriptedDieRoller([5])).Resolve();

        Assert.Equal("lumen-split", landed.Drawn!.Id);
        Assert.Empty(landed.Players[0].BalanceSheet.Stocks);
        Assert.Empty(landed.Players[1].BalanceSheet.Stocks);
        Assert.Equal(Money.FromDollars(900), landed.Players[0].Cash);
        Assert.Equal(Money.FromDollars(1_400), landed.Players[1].Cash);
    }

    private static Game Table()
    {
        int[] keep = [0, 1, 2, 3, 4, 5, 6, 7];
        int[] market = [4, 1, 2, 3, 0, 5, 6, 7, 8, 9];
        return Game.Create(GameId, Seats(), new ScriptedShuffle(keep, keep, market, keep));
    }

    private static Game CutMarket(string marketId)
    {
        int[] keep = [0, 1, 2, 3, 4, 5, 6, 7];
        return Game.Create(
            GameId,
            Seats(),
            new ScriptedShuffle(keep, keep, Promote(MarketCatalog.All, marketId), keep));
    }

    private static Game Cut(string smallDealId, string marketId)
    {
        int[] keep = [0, 1, 2, 3, 4, 5, 6, 7];
        return Game.Create(
            GameId,
            Seats(),
            new ScriptedShuffle(
                Promote(SmallDealCatalog.All, smallDealId),
                keep,
                Promote(MarketCatalog.All, marketId),
                keep));
    }

    private static PlayerSetup[] Seats()
    {
        return
        [
            new PlayerSetup("Ava", "night-baker", TokenColour.Amber),
            new PlayerSetup("Bo", "municipal-archivist", TokenColour.Cobalt),
        ];
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
