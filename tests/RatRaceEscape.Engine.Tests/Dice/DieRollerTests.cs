namespace RatRaceEscape.Engine.Tests;

public class DieRollerTests
{
    [Fact]
    public void Seeded_roller_repeats_the_same_sequence()
    {
        int[] first = RollSequence(new RandomDieRoller(42), 16);
        int[] second = RollSequence(new RandomDieRoller(42), 16);

        Assert.Equal(first, second);
        Assert.All(first, face => Assert.InRange(face, Dice.MinimumFace, Dice.MaximumFace));
    }

    [Fact]
    public void Different_seeds_do_not_share_a_sequence()
    {
        int[] first = RollSequence(new RandomDieRoller(1), 8);
        int[] second = RollSequence(new RandomDieRoller(2), 8);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Shared_roller_stays_on_the_cube()
    {
        RandomDieRoller roller = new();

        for (int index = 0; index < 64; index++)
        {
            Assert.InRange(roller.Roll(), Dice.MinimumFace, Dice.MaximumFace);
        }
    }

    [Fact]
    public void Random_roller_rejects_a_null_generator()
    {
        Assert.Throws<ArgumentNullException>(() => new RandomDieRoller(null!));
    }

    [Fact]
    public void Seeded_roller_is_safe_when_rolls_arrive_together()
    {
        RandomDieRoller roller = new(7);
        int[] faces = new int[32];

        Parallel.For(0, faces.Length, index => faces[index] = roller.Roll());

        Assert.All(faces, face => Assert.InRange(face, Dice.MinimumFace, Dice.MaximumFace));
    }

    [Fact]
    public void Scripted_roller_returns_faces_in_order_then_fails()
    {
        ScriptedDieRoller roller = new(new List<int> { 1, 6, 3 });

        Assert.Equal(3, roller.Remaining);
        Assert.Equal(1, roller.Roll());
        Assert.Equal(6, roller.Roll());
        Assert.Equal(3, roller.Roll());
        Assert.Equal(0, roller.Remaining);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => roller.Roll());
        Assert.Contains("No scripted faces remain.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Scripted_roller_copies_the_faces_it_was_given()
    {
        List<int> faces = [2, 4];
        ScriptedDieRoller roller = new(faces);

        faces[0] = 6;
        faces.Clear();

        Assert.Equal(2, roller.Roll());
        Assert.Equal(4, roller.Roll());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(-3)]
    public void Scripted_roller_rejects_a_face_off_the_cube(int face)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ScriptedDieRoller([1, face]));

        Assert.Contains("A die face must be from 1 to 6.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Scripted_roller_rejects_an_empty_sequence()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new ScriptedDieRoller([]));

        Assert.Equal("faces", exception.ParamName);
        Assert.Contains("At least one scripted face is required.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Scripted_roller_rejects_a_null_sequence()
    {
        Assert.Throws<ArgumentNullException>(() => new ScriptedDieRoller(null!));
    }

    private static int[] RollSequence(IDieRoller roller, int count)
    {
        int[] faces = new int[count];
        for (int index = 0; index < count; index++)
        {
            faces[index] = roller.Roll();
        }

        return faces;
    }
}
