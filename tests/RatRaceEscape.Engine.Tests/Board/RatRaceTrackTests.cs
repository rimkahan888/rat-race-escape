namespace RatRaceEscape.Engine.Tests;

public class RatRaceTrackTests
{
    [Fact]
    public void Move_enters_the_next_square_and_leaves_the_start_behind()
    {
        TrackMove move = RatRaceTrack.Move(0, 1);

        Assert.Equal(0, move.FromIndex);
        Assert.Equal(1, move.Steps);
        Assert.Equal(1, move.ToIndex);
        Assert.Equal(1, move.Visited.Single().Index);
        Assert.Equal(SpaceKind.Doodad, move.Visited[0].Kind);
        Assert.Empty(move.Paydays);
    }

    [Fact]
    public void Move_lands_on_payday_without_counting_the_square_it_left()
    {
        TrackMove landed = RatRaceTrack.Move(0, 6);

        Assert.Equal(6, landed.ToIndex);
        Assert.Equal([1, 2, 3, 4, 5, 6], landed.Visited.Select(step => step.Index));
        Assert.Equal([6], landed.Paydays.Select(step => step.Index));
        Assert.Equal(SpaceKind.Payday, RatRaceBoard.At(landed.ToIndex).Kind);

        TrackMove leavingPayday = RatRaceTrack.Move(18, 6);

        Assert.Equal(0, leavingPayday.ToIndex);
        Assert.Equal([19, 20, 21, 22, 23, 0], leavingPayday.Visited.Select(step => step.Index));
        Assert.Empty(leavingPayday.Paydays);
    }

    [Fact]
    public void Move_counts_a_payday_it_passes_on_the_way()
    {
        TrackMove move = RatRaceTrack.Move(0, 7);

        Assert.Equal(7, move.ToIndex);
        Assert.Equal(SpaceKind.Deal, move.Visited[^1].Kind);
        Assert.Equal([6], move.Paydays.Select(step => step.Index));
    }

    [Fact]
    public void Move_counts_every_payday_in_a_longer_walk()
    {
        TrackMove move = RatRaceTrack.Move(5, 8);

        Assert.Equal(13, move.ToIndex);
        Assert.Equal(SpaceKind.Downsized, RatRaceBoard.At(move.ToIndex).Kind);
        Assert.Equal([6, 7, 8, 9, 10, 11, 12, 13], move.Visited.Select(step => step.Index));
        Assert.Equal([6, 12], move.Paydays.Select(step => step.Index));
    }

    [Fact]
    public void Move_wraps_to_the_opening_space()
    {
        TrackMove one = RatRaceTrack.Move(23, 1);
        TrackMove across = RatRaceTrack.Move(22, 4);

        Assert.Equal(0, one.ToIndex);
        Assert.Equal(SpaceKind.Deal, one.Visited[0].Kind);
        Assert.Equal([23, 0, 1, 2], across.Visited.Select(step => step.Index));
        Assert.Equal(2, across.ToIndex);
        Assert.Empty(across.Paydays);
    }

    [Fact]
    public void Move_reaches_the_far_side_on_two_dice()
    {
        TrackMove move = RatRaceTrack.Move(0, RatRaceTrack.MaximumSteps);

        Assert.Equal(12, RatRaceTrack.MaximumSteps);
        Assert.Equal(12, move.ToIndex);
        Assert.Equal(12, move.Visited.Count);
        Assert.Equal([6, 12], move.Paydays.Select(step => step.Index));
    }

    [Fact]
    public void Move_copies_the_steps()
    {
        TrackStep[] steps =
        [
            new TrackStep(1, SpaceKind.Doodad),
            new TrackStep(2, SpaceKind.Deal),
        ];

        TrackMove move = new(0, 2, steps);
        steps[0] = new TrackStep(6, SpaceKind.Payday);

        Assert.Equal([1, 2], move.Visited.Select(step => step.Index));
        Assert.Empty(move.Paydays);
    }

    [Fact]
    public void Move_puts_the_token_on_the_square_it_stops_on()
    {
        Player player = Player.Create("Ava", ProfessionCatalog.GetById("night-baker"), TokenColour.Amber);
        TrackMove move = RatRaceTrack.Move(player.BoardIndex, 6);

        Player moved = player.WithBoardIndex(move.ToIndex);

        Assert.Equal(0, player.BoardIndex);
        Assert.Equal(6, moved.BoardIndex);
        Assert.Equal(player.Cash, moved.Cash);
        Assert.Equal(player.ChildCount, moved.ChildCount);
        Assert.Same(player.Profession, moved.Profession);
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(24, 1)]
    public void Move_rejects_a_start_off_the_loop(int fromIndex, int steps)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            RatRaceTrack.Move(fromIndex, steps));

        Assert.Equal("fromIndex", exception.ParamName);
        Assert.Contains("Board index must be from 0 to 23.", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-1)]
    public void Move_rejects_a_distance_outside_one_to_twelve(int steps)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            RatRaceTrack.Move(0, steps));

        Assert.Equal("steps", exception.ParamName);
        Assert.Contains("A move is from 1 to 12 spaces.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_rejects_a_null_step_list()
    {
        Assert.Throws<ArgumentNullException>(() => new TrackMove(0, 1, null!));
    }

    [Fact]
    public void Constructor_rejects_a_missing_or_skipped_step()
    {
        ArgumentException count = Assert.Throws<ArgumentException>(() =>
            new TrackMove(0, 2, [new TrackStep(1, SpaceKind.Doodad)]));
        ArgumentException gap = Assert.Throws<ArgumentException>(() =>
            new TrackMove(0, 2, [new TrackStep(1, SpaceKind.Doodad), new TrackStep(3, SpaceKind.Charity)]));
        TrackStep[] holeSteps = new TrackStep[1];
        ArgumentException hole = Assert.Throws<ArgumentException>(() => new TrackMove(0, 1, holeSteps));

        Assert.Contains("Every step must be recorded.", count.Message, StringComparison.Ordinal);
        Assert.Contains("Steps must follow the loop.", gap.Message, StringComparison.Ordinal);
        Assert.Contains("A step is required.", hole.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Step_rejects_a_kind_that_is_not_on_that_square()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new TrackStep(6, SpaceKind.Deal));

        Assert.Equal("kind", exception.ParamName);
        Assert.Contains("Kind does not match the space on the loop.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Step_rejects_an_index_off_the_loop()
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TrackStep(24, SpaceKind.Deal));

        Assert.Contains("Board index must be from 0 to 23.", exception.Message, StringComparison.Ordinal);
    }
}
