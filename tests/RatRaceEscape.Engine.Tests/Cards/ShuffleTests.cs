namespace RatRaceEscape.Engine.Tests;

public class ShuffleTests
{
    [Fact]
    public void A_seed_repeats_the_same_order()
    {
        int[] first = [1, 2, 3, 4, 5, 6, 7, 8];
        int[] second = [1, 2, 3, 4, 5, 6, 7, 8];

        new RandomShuffle(20261028).Shuffle(first);
        new RandomShuffle(20261028).Shuffle(second);

        Assert.Equal(first, second);
        Assert.Equal(8, first.Distinct().Count());
    }

    [Fact]
    public void A_seed_can_change_the_order()
    {
        bool moved = false;
        for (int seed = 1; seed <= 8; seed++)
        {
            int[] cards = [1, 2, 3, 4, 5, 6, 7, 8];
            new RandomShuffle(seed).Shuffle(cards);
            if (!cards.SequenceEqual([1, 2, 3, 4, 5, 6, 7, 8]))
            {
                moved = true;
                break;
            }
        }

        Assert.True(moved);
    }

    [Fact]
    public void Different_seeds_can_disagree()
    {
        int[] first = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        int[] second = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

        new RandomShuffle(1).Shuffle(first);
        new RandomShuffle(2).Shuffle(second);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void A_short_list_stays_put()
    {
        int[] empty = [];
        int[] one = [4];

        RandomShuffle shuffle = new(7);
        shuffle.Shuffle(empty);
        shuffle.Shuffle(one);

        Assert.Empty(empty);
        Assert.Equal([4], one);
    }

    [Fact]
    public void Random_shuffle_rejects_a_null_list_or_random()
    {
        RandomShuffle shuffle = new(1);

        Assert.Throws<ArgumentNullException>(() => shuffle.Shuffle<string>(null!));
        Assert.Throws<ArgumentNullException>(() => new RandomShuffle((Random)null!));
    }

    [Fact]
    public void The_shared_random_constructor_can_shuffle()
    {
        int[] cards = [1, 2, 3, 4];

        new RandomShuffle().Shuffle(cards);

        Assert.Equal(4, cards.Distinct().Count());
    }

    [Fact]
    public void Identity_shuffle_keeps_the_order_and_rejects_null()
    {
        string[] cards = ["a", "b", "c"];

        new IdentityShuffle().Shuffle(cards);

        Assert.Equal(["a", "b", "c"], cards);
        Assert.Throws<ArgumentNullException>(() => new IdentityShuffle().Shuffle<string>(null!));
    }

    [Fact]
    public void A_script_deals_the_listed_sources_and_then_stops()
    {
        ScriptedShuffle shuffle = new([2, 0, 1]);
        string[] cards = ["a", "b", "c"];

        shuffle.Shuffle(cards);

        Assert.Equal(["c", "a", "b"], cards);
        Assert.Equal(0, shuffle.Remaining);
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => shuffle.Shuffle(cards));
        Assert.Contains("No scripted shuffles remain.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_script_copies_the_order_it_is_given()
    {
        int[] order = [1, 0];
        ScriptedShuffle shuffle = new(order);
        order[0] = 0;
        string[] cards = ["a", "b"];

        shuffle.Shuffle(cards);

        Assert.Equal(["b", "a"], cards);
    }

    [Fact]
    public void A_script_rejects_an_order_that_is_not_a_permutation()
    {
        ArgumentException duplicated = Assert.Throws<ArgumentException>(() => new ScriptedShuffle([0, 0]));
        ArgumentException missing = Assert.Throws<ArgumentException>(() => new ScriptedShuffle([1, 2]));
        ArgumentException empty = Assert.Throws<ArgumentException>(() => new ScriptedShuffle(new List<int>()));
        ArgumentException none = Assert.Throws<ArgumentException>(() => new ScriptedShuffle());

        Assert.Contains("A scripted shuffle must list each index once.", duplicated.Message, StringComparison.Ordinal);
        Assert.Contains("A scripted shuffle must list each index once.", missing.Message, StringComparison.Ordinal);
        Assert.Contains("A scripted shuffle needs at least one index.", empty.Message, StringComparison.Ordinal);
        Assert.Equal("orders", none.ParamName);
        Assert.Contains("At least one scripted order is required.", none.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_script_rejects_a_null_order_a_null_list_and_the_wrong_length()
    {
        Assert.Throws<ArgumentNullException>(() => new ScriptedShuffle((IReadOnlyList<int>[])null!));
        ArgumentException missing = Assert.Throws<ArgumentException>(() => new ScriptedShuffle([0, 1], null!));
        ScriptedShuffle shuffle = new([0]);

        Assert.Contains("A scripted order is required.", missing.Message, StringComparison.Ordinal);
        Assert.Throws<ArgumentNullException>(() => shuffle.Shuffle<string>(null!));
        InvalidOperationException length = Assert.Throws<InvalidOperationException>(() => shuffle.Shuffle<string>(["a", "b"]));
        Assert.Contains("Scripted shuffle expected 2 cards and was given an order of 1.", length.Message, StringComparison.Ordinal);
        Assert.Equal(1, shuffle.Remaining);
        shuffle.Shuffle<string>(["a"]);
        Assert.Equal(0, shuffle.Remaining);
    }
}
