namespace RatRaceEscape.Engine;

/// <summary>
/// The dice thrown on this turn and the walk they produced.
/// </summary>
public sealed class TurnRoll
{
    public TurnRoll(DiceRoll dice, int fromIndex)
    {
        ArgumentNullException.ThrowIfNull(dice);

        Dice = dice;
        Move = RatRaceTrack.Move(fromIndex, dice.Total);
    }

    public DiceRoll Dice { get; }

    public TrackMove Move { get; }

    public IReadOnlyList<int> Faces => Dice.Faces;

    public int Total => Dice.Total;

    public int FromIndex => Move.FromIndex;

    public int ToIndex => Move.ToIndex;
}
