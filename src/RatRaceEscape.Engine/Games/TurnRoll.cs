namespace RatRaceEscape.Engine;

/// <summary>
/// The dice thrown on this turn and the walk they produced.
/// </summary>
public sealed class TurnRoll
{
    public TurnRoll(DiceRoll dice, int fromIndex, Money paydayCash)
    {
        ArgumentNullException.ThrowIfNull(dice);

        Dice = dice;
        Move = RatRaceTrack.Move(fromIndex, dice.Total);
        PaydayCash = paydayCash;
    }

    public DiceRoll Dice { get; }

    public TrackMove Move { get; }

    public IReadOnlyList<int> Faces => Dice.Faces;

    public int Total => Dice.Total;

    public int FromIndex => Move.FromIndex;

    public int ToIndex => Move.ToIndex;

    /// <summary>
    /// Cash collected for the paydays on this roll, or the shortfall that was paid
    /// when monthly cash flow is negative. Zero when the walk entered no pay window.
    /// </summary>
    public Money PaydayCash { get; }
}
