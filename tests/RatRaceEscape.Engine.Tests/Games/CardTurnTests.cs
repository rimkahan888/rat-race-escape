namespace RatRaceEscape.Engine.Tests;

public class CardTurnTests
{
    private static readonly Guid GameId = Guid.Parse("88888888-8888-8888-8888-888888888888");

    [Fact]
    public void A_new_game_deals_each_catalog_from_the_top()
    {
        Game game = Solo();

        Assert.Null(game.Drawn);
        Assert.Null(game.PendingDeck);
        Assert.Equal(SmallDealCatalog.All.Count, game.Piles.SmallDeals.DrawCount);
        Assert.Equal(0, game.Piles.SmallDeals.DiscardCount);
        Assert.Equal(BigDealCatalog.All.Count, game.Piles.BigDeals.DrawCount);
        Assert.Equal(MarketCatalog.All.Count, game.Piles.Market.DrawCount);
        Assert.Equal(DoodadCatalog.All.Count, game.Piles.Doodads.DrawCount);
    }

    [Fact]
    public void An_offer_does_not_draw_until_the_player_picks_a_deck()
    {
        Game landed = Solo().Roll(new ScriptedDieRoller([2]));
        int small = landed.Piles.SmallDeals.DrawCount;
        int big = landed.Piles.BigDeals.DrawCount;

        TurnException missing = Assert.Throws<TurnException>(() => landed.Resolve());
        TurnException donate = Assert.Throws<TurnException>(() => landed.Resolve(SpaceChoice.Donate));

        Assert.Contains("An offer needs a small-deal or big-deal choice.", missing.Errors);
        Assert.Contains("An offer needs a small-deal or big-deal choice.", donate.Errors);
        Assert.Equal(TurnPhase.AwaitingResolve, landed.TurnPhase);
        Assert.Null(landed.Drawn);
        Assert.Equal(small, landed.Piles.SmallDeals.DrawCount);
        Assert.Equal(big, landed.Piles.BigDeals.DrawCount);
    }

    [Fact]
    public void Choosing_small_or_big_draws_that_deck_and_leaves_the_cash()
    {
        Game start = Solo();
        Game small = start.Roll(new ScriptedDieRoller([2])).Resolve(SpaceChoice.SmallDeal);
        Game big = start.Roll(new ScriptedDieRoller([2])).Resolve(SpaceChoice.BigDeal);

        Assert.Equal("lumen-glass", small.Drawn!.Id);
        Assert.Equal(SmallDealKind.Stock, small.Drawn.SmallDeal!.Kind);
        Assert.Equal("LUMEN", small.Drawn.SmallDeal.Symbol);
        Assert.Equal(DeckHook.Deal, small.PendingDeck);
        Assert.Equal(TurnPhase.AwaitingEnd, small.TurnPhase);
        Assert.Equal(start.CurrentPlayer.Cash, small.CurrentPlayer.Cash);
        Assert.Equal(start.Piles.SmallDeals.DrawCount - 1, small.Piles.SmallDeals.DrawCount);
        Assert.Equal(start.Piles.BigDeals.DrawCount, small.Piles.BigDeals.DrawCount);
        Assert.Null(start.Drawn);

        Assert.Equal("brickworks-lofts", big.Drawn!.Id);
        Assert.Equal(BigDealKind.Property, big.Drawn.BigDeal!.Kind);
        Assert.Null(big.Drawn.SmallDeal);
        Assert.Equal(start.Piles.BigDeals.DrawCount - 1, big.Piles.BigDeals.DrawCount);
        Assert.Equal(start.Piles.SmallDeals.DrawCount, big.Piles.SmallDeals.DrawCount);
        Assert.Equal(start.CurrentPlayer.Cash, big.CurrentPlayer.Cash);
    }

    [Fact]
    public void The_market_draws_the_top_offer_and_does_not_move_cash()
    {
        Game start = Solo();

        Game drawn = start.Roll(new ScriptedDieRoller([5])).Resolve();

        Assert.Equal(SpaceKind.Market, RatRaceBoard.At(drawn.CurrentPlayer.BoardIndex).Kind);
        Assert.Equal("pier-bid", drawn.Drawn!.Id);
        Assert.Equal(MarketKind.Offer, drawn.Drawn.Market!.Kind);
        Assert.Equal("PIER", drawn.Drawn.Market.AssetName);
        Assert.Equal(DeckHook.Market, drawn.PendingDeck);
        Assert.Equal(start.CurrentPlayer.Cash, drawn.CurrentPlayer.Cash);
        Assert.Equal(start.Piles.Market.DrawCount - 1, drawn.Piles.Market.DrawCount);
        Assert.Equal(0, drawn.Piles.Market.DiscardCount);
    }

    [Fact]
    public void A_doodad_pays_the_top_bill_and_the_next_player_gets_the_next_one()
    {
        Game table = Two();
        Game ava = table.Roll(new ScriptedDieRoller([1])).Resolve();

        Assert.Equal("cracked-oven-door", ava.Drawn!.Doodad!.Id);
        Assert.Equal(Money.FromDollars(180), ava.Drawn.Doodad.Cost);
        Assert.Equal(Money.FromCents(72_000), ava.Players[0].Cash);
        Assert.Equal(Money.FromCents(140_000), ava.Players[1].Cash);
        Assert.Equal(table.Piles.Doodads.DrawCount - 1, ava.Piles.Doodads.DrawCount);
        Assert.Equal(Money.FromCents(90_000), table.Players[0].Cash);

        Game ended = ava.EndTurn();
        Game bo = ended.Roll(new ScriptedDieRoller([1])).Resolve();

        Assert.Null(ended.Drawn);
        Assert.Equal(1, ended.Piles.Doodads.DiscardCount);
        Assert.Equal("harbor-parking-fine", bo.Drawn!.Doodad!.Id);
        Assert.Equal(Money.FromCents(136_000), bo.Players[1].Cash);
        Assert.Equal(Money.FromCents(72_000), bo.Players[0].Cash);
        Assert.Equal(1, bo.Piles.Doodads.DiscardCount);
        Assert.Equal(DoodadCatalog.All.Count - 2, bo.Piles.Doodads.DrawCount);
    }

    [Fact]
    public void Ending_the_turn_puts_the_market_card_on_the_discard_pile()
    {
        Game ended = Solo().Roll(new ScriptedDieRoller([5])).Resolve().EndTurn();

        Assert.Null(ended.Drawn);
        Assert.Null(ended.PendingDeck);
        Assert.Equal(1, ended.Piles.Market.DiscardCount);
        Assert.Equal("lumen-bid", ended.Piles.Market.Draw().Card.Id);
    }

    [Fact]
    public void A_bill_the_player_cannot_cover_leaves_the_turn_and_the_deck()
    {
        Doodad expensive = Doodad.Create(
            "gold-crown",
            "Gold Crown",
            "A crown the dentist did not plan for.",
            Money.FromDollars(1_000));
        CardPiles piles = CardPiles.Create(new IdentityShuffle())
            .WithDoodads(Deck<Doodad>.Create([expensive], new IdentityShuffle()));
        Game landed = Game.Create(
                GameId,
                [new PlayerSetup("Ava", "night-baker", TokenColour.Amber)],
                piles)
            .Roll(new ScriptedDieRoller([1]));

        TurnException exception = Assert.Throws<TurnException>(() => landed.Resolve());

        Assert.Contains("Cash cannot cover the doodad.", exception.Errors);
        Assert.Equal(TurnPhase.AwaitingResolve, landed.TurnPhase);
        Assert.Equal(Money.FromCents(90_000), landed.CurrentPlayer.Cash);
        Assert.Null(landed.Drawn);
        Assert.Equal(1, landed.Piles.Doodads.DrawCount);
        Assert.Equal(0, landed.Piles.Doodads.DiscardCount);
    }

    [Fact]
    public void A_deal_choice_is_rejected_off_an_offer()
    {
        Game doodad = Solo().Roll(new ScriptedDieRoller([1]));

        TurnException exception = Assert.Throws<TurnException>(() => doodad.Resolve(SpaceChoice.BigDeal));

        Assert.Contains("This space does not take a choice.", exception.Errors);
        Assert.Equal(DoodadCatalog.All.Count, doodad.Piles.Doodads.DrawCount);
        Assert.Equal(Money.FromCents(90_000), doodad.CurrentPlayer.Cash);
    }

    [Fact]
    public void A_scripted_shuffle_cuts_the_small_deals_before_the_other_decks()
    {
        int[] keep = [0, 1, 2, 3, 4, 5, 6, 7];
        int[] swap = [1, 0, 2, 3, 4, 5, 6, 7];
        Game game = Game.Create(
            GameId,
            [new PlayerSetup("Ava", "night-baker", TokenColour.Amber)],
            new ScriptedShuffle(swap, keep, keep, keep));

        Game drawn = game.Roll(new ScriptedDieRoller([2])).Resolve(SpaceChoice.SmallDeal);

        Assert.Equal("north-pier", drawn.Drawn!.Id);
        Assert.Equal("brickworks-lofts", drawn.Piles.BigDeals.Draw().Card.Id);
    }

    [Fact]
    public void Create_rejects_a_null_shuffle_or_a_null_set_of_decks()
    {
        PlayerSetup ava = new("Ava", "night-baker", TokenColour.Amber);

        Assert.Throws<ArgumentNullException>(() => Game.Create(GameId, [ava], (IShuffle)null!));
        Assert.Throws<ArgumentNullException>(() => Game.Create(GameId, [ava], (CardPiles)null!));
        Assert.Throws<ArgumentNullException>(() => CardPiles.Create(null!));
    }

    private static Game Solo()
    {
        return Game.Create(GameId, [new PlayerSetup("Ava", "night-baker", TokenColour.Amber)]);
    }

    private static Game Two()
    {
        return Game.Create(
            GameId,
            [
                new PlayerSetup("Ava", "night-baker", TokenColour.Amber),
                new PlayerSetup("Bo", "municipal-archivist", TokenColour.Cobalt),
            ]);
    }
}
