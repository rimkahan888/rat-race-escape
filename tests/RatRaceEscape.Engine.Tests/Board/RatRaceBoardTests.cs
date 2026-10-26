namespace RatRaceEscape.Engine.Tests;

public class RatRaceBoardTests
{
    [Fact]
    public void Layout_is_a_24_space_loop()
    {
        SpaceKind[] expected =
        [
            SpaceKind.Deal,
            SpaceKind.Doodad,
            SpaceKind.Deal,
            SpaceKind.Charity,
            SpaceKind.Deal,
            SpaceKind.Market,
            SpaceKind.Payday,
            SpaceKind.Deal,
            SpaceKind.Baby,
            SpaceKind.Deal,
            SpaceKind.Doodad,
            SpaceKind.Deal,
            SpaceKind.Payday,
            SpaceKind.Downsized,
            SpaceKind.Deal,
            SpaceKind.Market,
            SpaceKind.Deal,
            SpaceKind.Doodad,
            SpaceKind.Payday,
            SpaceKind.Charity,
            SpaceKind.Deal,
            SpaceKind.Market,
            SpaceKind.Deal,
            SpaceKind.Doodad,
        ];

        Assert.Equal(RatRaceBoard.SpaceCount, expected.Length);
        Assert.Equal(expected, RatRaceBoard.Spaces.Select(space => space.Kind));

        for (int index = 0; index < expected.Length; index++)
        {
            BoardSpace space = RatRaceBoard.At(index);
            Assert.Same(RatRaceBoard.Spaces[index], space);
            Assert.Equal(index, space.Index);
            Assert.Equal(expected[index], space.Kind);
        }
    }

    [Fact]
    public void Opening_space_is_an_offer_and_paydays_are_six_spaces_apart()
    {
        Assert.Equal(SpaceKind.Deal, RatRaceBoard.At(0).Kind);
        Assert.Equal("Offer Table", RatRaceBoard.At(0).Title);

        Assert.Equal(SpaceKind.Payday, RatRaceBoard.At(6).Kind);
        Assert.Equal(SpaceKind.Payday, RatRaceBoard.At(12).Kind);
        Assert.Equal(SpaceKind.Payday, RatRaceBoard.At(18).Kind);
        Assert.Equal("Pay Window", RatRaceBoard.At(6).Title);
        Assert.Equal("Wages for the month are paid out at the booth.", RatRaceBoard.At(6).Summary);
    }

    [Fact]
    public void Life_spaces_and_deck_hooks_sit_where_the_loop_says()
    {
        Assert.Equal(SpaceKind.Charity, RatRaceBoard.At(3).Kind);
        Assert.Equal(SpaceKind.Charity, RatRaceBoard.At(19).Kind);
        Assert.Equal("Mutual Aid", RatRaceBoard.At(3).Title);

        Assert.Equal(SpaceKind.Baby, RatRaceBoard.At(8).Kind);
        Assert.Equal("New Arrival", RatRaceBoard.At(8).Title);
        Assert.Equal("Another dependent joins the household.", RatRaceBoard.At(8).Summary);

        Assert.Equal(SpaceKind.Downsized, RatRaceBoard.At(13).Kind);
        Assert.Equal("Hours Cut", RatRaceBoard.At(13).Title);
        Assert.Equal("The shift list drops your hours, and the bills stay.", RatRaceBoard.At(13).Summary);

        Assert.Equal(3, Count(SpaceKind.Market));
        Assert.Equal("Corner Market", RatRaceBoard.At(5).Title);
        Assert.Equal(4, Count(SpaceKind.Doodad));
        Assert.Equal("Odd Bill", RatRaceBoard.At(1).Title);
        Assert.Equal(10, Count(SpaceKind.Deal));
    }

    [Fact]
    public void Every_kind_is_on_the_loop_once_at_least()
    {
        foreach (SpaceKind kind in Enum.GetValues<SpaceKind>())
        {
            Assert.Contains(RatRaceBoard.Spaces, space => space.Kind == kind);
        }
    }

    [Fact]
    public void Space_text_is_original()
    {
        foreach (BoardSpace space in RatRaceBoard.Spaces)
        {
            Assert.InRange(space.Title.Length, 2, 40);
            Assert.InRange(space.Summary.Length, 12, 160);
            string text = $"{space.Title} {space.Summary}";
            Assert.DoesNotContain("CASHFLOW", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Rich Dad", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Kiyosaki", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(24)]
    public void At_rejects_an_index_off_the_loop(int index)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            RatRaceBoard.At(index));

        Assert.Equal("index", exception.ParamName);
        Assert.Contains("Board index must be from 0 to 23.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_trims_the_title_and_summary()
    {
        BoardSpace space = BoardSpace.Create(
            4,
            SpaceKind.Market,
            "  Corner Market  ",
            "  Buyers on the square are naming prices for assets.  ");

        Assert.Equal(4, space.Index);
        Assert.Equal(SpaceKind.Market, space.Kind);
        Assert.Equal("Corner Market", space.Title);
        Assert.Equal("Buyers on the square are naming prices for assets.", space.Summary);
    }

    [Fact]
    public void Create_accepts_text_at_the_length_bounds()
    {
        BoardSpace shortest = BoardSpace.Create(0, SpaceKind.Payday, "Ab", new string('B', 12));
        BoardSpace longest = BoardSpace.Create(23, SpaceKind.Deal, new string('A', 40), new string('B', 160));

        Assert.Equal(2, shortest.Title.Length);
        Assert.Equal(12, shortest.Summary.Length);
        Assert.Equal(40, longest.Title.Length);
        Assert.Equal(160, longest.Summary.Length);
    }

    [Fact]
    public void Create_rejects_a_null_title_or_summary()
    {
        Assert.Throws<ArgumentNullException>(() => BoardSpace.Create(0, SpaceKind.Payday, null!, "Wages for the month."));
        Assert.Throws<ArgumentNullException>(() => BoardSpace.Create(0, SpaceKind.Payday, "Pay Window", null!));
    }

    [Fact]
    public void Create_reports_every_broken_rule_together()
    {
        BoardSpaceValidationException exception = Assert.Throws<BoardSpaceValidationException>(() =>
            BoardSpace.Create(-1, (SpaceKind)99, "\u0001", "short\u0001"));

        Assert.Contains("Index must be from 0 to 23.", exception.Errors);
        Assert.Contains("Space kind is not a board space.", exception.Errors);
        Assert.Contains("Title must be between 2 and 40 characters.", exception.Errors);
        Assert.Contains("Title cannot contain control characters.", exception.Errors);
        Assert.Contains("Summary must be between 12 and 160 characters.", exception.Errors);
        Assert.Contains("Summary cannot contain control characters.", exception.Errors);
        Assert.Equal(6, exception.Errors.Count);
    }

    [Fact]
    public void Create_rejects_text_past_the_length_bounds()
    {
        BoardSpaceValidationException exception = Assert.Throws<BoardSpaceValidationException>(() =>
            BoardSpace.Create(24, SpaceKind.Payday, new string('A', 41), new string('B', 161)));

        Assert.Contains("Index must be from 0 to 23.", exception.Errors);
        Assert.Contains("Title must be between 2 and 40 characters.", exception.Errors);
        Assert.Contains("Summary must be between 12 and 160 characters.", exception.Errors);
    }

    [Fact]
    public void Validation_exception_requires_an_error()
    {
        Assert.Throws<ArgumentNullException>(() => new BoardSpaceValidationException(null!));
        Assert.Throws<ArgumentException>(() => new BoardSpaceValidationException([]));
    }

    private static int Count(SpaceKind kind)
    {
        int count = 0;
        foreach (BoardSpace space in RatRaceBoard.Spaces)
        {
            if (space.Kind == kind)
            {
                count++;
            }
        }

        return count;
    }
}
