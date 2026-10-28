namespace RatRaceEscape.Engine.Tests;

public class FastTrackBoardTests
{
    [Fact]
    public void Layout_is_a_12_space_loop_with_paydays_four_apart()
    {
        FastTrackKind[] expected =
        [
            FastTrackKind.Payday,
            FastTrackKind.Windfall,
            FastTrackKind.Setback,
            FastTrackKind.Dream,
            FastTrackKind.Payday,
            FastTrackKind.Windfall,
            FastTrackKind.Setback,
            FastTrackKind.Dream,
            FastTrackKind.Payday,
            FastTrackKind.Windfall,
            FastTrackKind.Setback,
            FastTrackKind.Dream,
        ];

        Assert.Equal(FastTrackBoard.SpaceCount, expected.Length);
        Assert.Equal(expected, FastTrackBoard.Spaces.Select(space => space.Kind));
        Assert.Equal("Distribution Desk", FastTrackBoard.At(0).Title);
        Assert.Equal("Harbor Bonus", FastTrackBoard.At(1).Title);
        Assert.Equal("Dry Dock", FastTrackBoard.At(2).Title);
        Assert.Equal("Dream Listing", FastTrackBoard.At(3).Title);
        Assert.Equal(Money.FromDollars(1_000), FastTrackBoard.Windfall);
        Assert.Equal(Money.FromDollars(300), FastTrackBoard.Setback);

        foreach (FastTrackSpace space in FastTrackBoard.Spaces)
        {
            Assert.DoesNotContain("CASHFLOW", space.Summary, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Rich Dad", space.Title, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Move_counts_paydays_it_enters_and_wraps_the_loop()
    {
        FastTrackMove one = FastTrackBoard.Move(0, 1);
        FastTrackMove payday = FastTrackBoard.Move(0, 4);
        FastTrackMove lap = FastTrackBoard.Move(0, 12);

        Assert.Equal(1, one.ToIndex);
        Assert.Equal(0, one.Paydays);
        Assert.Equal(4, payday.ToIndex);
        Assert.Equal(1, payday.Paydays);
        Assert.Equal(0, lap.ToIndex);
        Assert.Equal(3, lap.Paydays);
        Assert.Throws<ArgumentOutOfRangeException>(() => FastTrackBoard.Move(0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => FastTrackBoard.At(FastTrackBoard.SpaceCount));
    }

    [Fact]
    public void Create_rejects_an_index_off_the_fast_track()
    {
        BoardSpaceValidationException exception = Assert.Throws<BoardSpaceValidationException>(() =>
            FastTrackSpace.Create(20, FastTrackKind.Payday, "Distribution Desk", "Passive income is paid out for the month."));

        Assert.Contains("Index must be from 0 to 11.", exception.Errors);
    }
}
