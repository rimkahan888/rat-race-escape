namespace RatRaceEscape.Engine;

/// <summary>
/// The dice thrown on this turn and the walk they produced.
/// </summary>
public sealed class TurnRoll
{
    private readonly int? _toIndex;

    public TurnRoll(DiceRoll dice, int fromIndex, Money paydayCash)
    {
        ArgumentNullException.ThrowIfNull(dice);

        Dice = dice;
        Move = RatRaceTrack.Move(fromIndex, dice.Total);
        PaydayCash = paydayCash;
    }

    public TurnRoll(DiceRoll dice, int fromIndex, int toIndex, Money paydayCash)
        : this(dice, fromIndex, paydayCash)
    {
        if (toIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(toIndex), "Board index cannot be negative.");
        }

        _toIndex = toIndex;
    }

    public DiceRoll Dice { get; }

    /// <summary>
    /// The walk as it would look on the rat race. On the fast track,
    /// <see cref="ToIndex"/> is the square actually reached.
    /// </summary>
    public TrackMove Move { get; }

    public IReadOnlyList<int> Faces => Dice.Faces;

    public int Total => Dice.Total;

    public int FromIndex => Move.FromIndex;

    public int ToIndex => _toIndex ?? Move.ToIndex;

    /// <summary>
    /// Cash collected for the paydays on this roll, or the shortfall that was paid
    /// when monthly cash flow is negative. Zero when the walk entered no pay window.
    /// </summary>
    public Money PaydayCash { get; }
}
