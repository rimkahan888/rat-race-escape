namespace RatRaceEscape.Engine.Tests;

public class DiceTests
{
    [Fact]
    public void Roll_returns_one_face_and_that_total()
    {
        DiceRoll roll = Dice.Roll(new ScriptedDieRoller([4]), 1);

        Assert.Equal([4], roll.Faces);
        Assert.Equal(4, roll.Total);
    }

    [Fact]
    public void Roll_sums_two_dice()
    {
        ScriptedDieRoller roller = new([2, 5, 6]);

        DiceRoll roll = Dice.Roll(roller, 2);

        Assert.Equal([2, 5], roll.Faces);
        Assert.Equal(7, roll.Total);
        Assert.Equal(1, roller.Remaining);
    }

    [Fact]
    public void Constructor_copies_the_faces()
    {
        int[] faces = [3, 1];

        DiceRoll roll = new(faces, 4);
        faces[0] = 6;

        Assert.Equal([3, 1], roll.Faces);
        Assert.Equal(4, roll.Total);
    }

    [Fact]
    public void Constructor_rejects_a_total_that_is_not_the_faces()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new DiceRoll([3, 1], 5));

        Assert.Equal("total", exception.ParamName);
        Assert.Contains("Total must equal the faces.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_rejects_a_face_count_other_than_one_or_two()
    {
        ArgumentException empty = Assert.Throws<ArgumentException>(() => new DiceRoll([], 0));
        ArgumentException three = Assert.Throws<ArgumentException>(() => new DiceRoll([1, 2, 3], 6));

        Assert.Equal("faces", empty.ParamName);
        Assert.Contains("A roll has one or two faces.", empty.Message, StringComparison.Ordinal);
        Assert.Equal("faces", three.ParamName);
        Assert.Contains("A roll has one or two faces.", three.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_rejects_a_face_off_the_cube()
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new DiceRoll([0], 0));

        Assert.Contains("A die face must be from 1 to 6.", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(-1)]
    public void Roll_rejects_a_count_other_than_one_or_two(int count)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            Dice.Roll(new ScriptedDieRoller([1, 1, 1]), count));

        Assert.Equal("count", exception.ParamName);
        Assert.Contains("Roll one or two dice.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Roll_rejects_a_null_roller()
    {
        Assert.Throws<ArgumentNullException>(() => Dice.Roll(null!, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void Roll_rejects_a_face_off_the_cube(int face)
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            Dice.Roll(new FixedFaceRoller(face), 1));

        Assert.Contains("A die face must be from 1 to 6.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Roll_rejects_a_null_face_list()
    {
        Assert.Throws<ArgumentNullException>(() => new DiceRoll(null!, 0));
    }

    private sealed class FixedFaceRoller : IDieRoller
    {
        private readonly int _face;

        public FixedFaceRoller(int face)
        {
            _face = face;
        }

        public int Roll()
        {
            return _face;
        }
    }
}
