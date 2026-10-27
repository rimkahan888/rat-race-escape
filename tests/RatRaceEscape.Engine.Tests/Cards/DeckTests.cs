namespace RatRaceEscape.Engine.Tests;

public class DeckTests
{
    [Fact]
    public void Create_shuffles_a_copy_and_deals_from_the_top()
    {
        List<string> cards = ["a", "b", "c"];
        ScriptedShuffle shuffle = new([2, 0, 1]);

        Deck<string> deck = Deck<string>.Create(cards, shuffle);
        cards[0] = "z";
        DeckDraw<string> first = deck.Draw();
        DeckDraw<string> second = first.Deck.Draw();

        Assert.Equal(0, shuffle.Remaining);
        Assert.Equal(3, deck.DrawCount);
        Assert.Equal(0, deck.DiscardCount);
        Assert.Equal("c", first.Card);
        Assert.Equal(2, first.Deck.DrawCount);
        Assert.Equal("a", second.Card);
        Assert.Equal(1, second.Deck.DrawCount);
        Assert.Equal("b", second.Deck.Draw().Card);
    }

    [Fact]
    public void Draw_does_not_discard_the_card()
    {
        Deck<string> deck = Deck<string>.Create(["a", "b"], new IdentityShuffle());

        DeckDraw<string> drawn = deck.Draw();

        Assert.Equal("a", drawn.Card);
        Assert.Equal(0, drawn.Deck.DiscardCount);
        Assert.Equal(1, drawn.Deck.DrawCount);
        Assert.Equal(2, deck.DrawCount);
    }

    [Fact]
    public void Discard_puts_the_card_at_the_bottom_and_leaves_the_draw_pile()
    {
        Deck<string> deck = Deck<string>.Create(["a", "b"], new IdentityShuffle());
        DeckDraw<string> drawn = deck.Draw();

        Deck<string> discarded = drawn.Deck.Discard(drawn.Card);

        Assert.Equal(0, drawn.Deck.DiscardCount);
        Assert.Equal(1, discarded.DiscardCount);
        Assert.Equal(1, discarded.DrawCount);
        Assert.Equal("b", discarded.Draw().Card);
    }

    [Fact]
    public void An_empty_draw_pile_reshuffles_the_discards_in_script_order()
    {
        ScriptedShuffle shuffle = new([0, 1, 2], [2, 0, 1]);
        Deck<string> deck = Deck<string>.Create(["a", "b", "c"], shuffle);
        DeckDraw<string> first = deck.Draw();
        DeckDraw<string> second = first.Deck.Draw();
        DeckDraw<string> third = second.Deck.Draw();
        Deck<string> spent = third.Deck.Discard(first.Card).Discard(second.Card).Discard(third.Card);

        Assert.Equal(0, spent.DrawCount);
        Assert.Equal(3, spent.DiscardCount);
        Assert.Equal(1, shuffle.Remaining);

        DeckDraw<string> recycled = spent.Draw();
        DeckDraw<string> next = recycled.Deck.Draw();

        Assert.Equal(0, shuffle.Remaining);
        Assert.Equal("c", recycled.Card);
        Assert.Equal(0, recycled.Deck.DiscardCount);
        Assert.Equal(2, recycled.Deck.DrawCount);
        Assert.Equal("a", next.Card);
        Assert.Equal("b", next.Deck.Draw().Card);
    }

    [Fact]
    public void Identity_reshuffle_deals_discards_from_oldest_to_newest()
    {
        Deck<string> deck = Deck<string>.Create(["a"], new IdentityShuffle());
        DeckDraw<string> drawn = deck.Draw();
        Deck<string> discarded = drawn.Deck.Discard("a").Discard("b");

        DeckDraw<string> first = discarded.Draw();
        DeckDraw<string> second = first.Deck.Draw();

        Assert.Equal("a", first.Card);
        Assert.Equal("b", second.Card);
        Assert.Equal(0, second.Deck.DrawCount);
    }

    [Fact]
    public void Draw_fails_when_both_piles_are_empty_and_leaves_the_deck()
    {
        Deck<string> spent = Deck<string>.Create(["only"], new IdentityShuffle()).Draw().Deck;

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => spent.Draw());

        Assert.Contains("The deck has no cards left to draw.", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, spent.DrawCount);
        Assert.Equal(0, spent.DiscardCount);
    }

    [Fact]
    public void A_seeded_shuffle_deals_the_same_deck_twice()
    {
        string[] cards = ["a", "b", "c", "d", "e", "f"];

        string[] first = DealAll(Deck<string>.Create(cards, new RandomShuffle(11)));
        string[] second = DealAll(Deck<string>.Create(cards, new RandomShuffle(11)));

        Assert.Equal(first, second);
        Assert.Equal(6, first.Distinct().Count());
    }

    [Fact]
    public void Create_rejects_a_null_shuffle_a_null_list_an_empty_list_and_a_null_card()
    {
        Assert.Throws<ArgumentNullException>(() => Deck<string>.Create(["a"], null!));
        Assert.Throws<ArgumentNullException>(() => Deck<string>.Create(null!, new IdentityShuffle()));

        ArgumentException empty = Assert.Throws<ArgumentException>(() =>
            Deck<string>.Create([], new IdentityShuffle()));
        ArgumentException hole = Assert.Throws<ArgumentException>(() =>
            Deck<string>.Create(new string?[] { "a", null! }!, new IdentityShuffle()));

        Assert.Equal("cards", empty.ParamName);
        Assert.Contains("A deck needs at least one card.", empty.Message, StringComparison.Ordinal);
        Assert.Equal("cards", hole.ParamName);
        Assert.Contains("A deck cannot contain an empty card.", hole.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Discard_and_the_draw_result_reject_null()
    {
        Deck<string> deck = Deck<string>.Create(["a"], new IdentityShuffle());

        Assert.Throws<ArgumentNullException>(() => deck.Discard(null!));
        Assert.Throws<ArgumentNullException>(() => new DeckDraw<string>(null!, deck));
        Assert.Throws<ArgumentNullException>(() => new DeckDraw<string>("a", null!));
    }

    private static string[] DealAll(Deck<string> deck)
    {
        List<string> dealt = [];
        while (deck.DrawCount > 0)
        {
            DeckDraw<string> drawn = deck.Draw();
            dealt.Add(drawn.Card);
            deck = drawn.Deck;
        }

        return dealt.ToArray();
    }
}
