namespace RatRaceEscape.Engine;

/// <summary>
/// A deck of cards. Building the deck shuffles a copy of the cards. Drawing
/// takes the top card and leaves the caller with a new deck. Discarding puts
/// a card at the bottom of the discard pile. The next draw after the draw
/// pile runs out shuffles those discards into a new draw pile. If the discard
/// pile is empty too, the draw fails. The shuffle stays with the deck, so a
/// seeded shuffle continues one sequence across reshuffles.
/// </summary>
public sealed class Deck<T>
    where T : notnull
{
    private readonly Lock _gate = new();
    private readonly T[] _draw;
    private readonly T[] _discard;
    private readonly IShuffle _shuffle;
    private DeckDraw<T>? _drawn;

    private Deck(T[] draw, T[] discard, IShuffle shuffle)
    {
        _draw = draw;
        _discard = discard;
        _shuffle = shuffle;
    }

    public int DrawCount => _draw.Length;

    public int DiscardCount => _discard.Length;

    public static Deck<T> Create(IReadOnlyList<T> cards, IShuffle shuffle)
    {
        ArgumentNullException.ThrowIfNull(cards);
        ArgumentNullException.ThrowIfNull(shuffle);

        if (cards.Count == 0)
        {
            throw new ArgumentException("A deck needs at least one card.", nameof(cards));
        }

        T[] draw = new T[cards.Count];
        for (int index = 0; index < cards.Count; index++)
        {
            T card = cards[index];
            if (card is null)
            {
                throw new ArgumentException("A deck cannot contain an empty card.", nameof(cards));
            }

            draw[index] = card;
        }

        shuffle.Shuffle(draw);
        return new Deck<T>(draw, [], shuffle);
    }

    public DeckDraw<T> Draw()
    {
        lock (_gate)
        {
            // A discarded draw, such as a bill the player cannot pay, must
            // deal the same card again. Reshuffling the discards advances the
            // seeded order, so that work is kept with this deck.
            _drawn ??= Take();
            return _drawn;
        }
    }

    private DeckDraw<T> Take()
    {
        T[] draw = _draw;
        T[] discard = _discard;
        if (draw.Length == 0)
        {
            if (discard.Length == 0)
            {
                throw new InvalidOperationException("The deck has no cards left to draw.");
            }

            draw = Copy(_discard);
            _shuffle.Shuffle(draw);
            discard = [];
        }

        T[] rest = new T[draw.Length - 1];
        if (rest.Length > 0)
        {
            Array.Copy(draw, 1, rest, 0, rest.Length);
        }

        return new DeckDraw<T>(draw[0], new Deck<T>(rest, discard, _shuffle));
    }

    public Deck<T> Discard(T card)
    {
        ArgumentNullException.ThrowIfNull(card);

        T[] discard = new T[_discard.Length + 1];
        if (_discard.Length > 0)
        {
            Array.Copy(_discard, discard, _discard.Length);
        }

        discard[^1] = card;
        return new Deck<T>(_draw, discard, _shuffle);
    }

    private static T[] Copy(T[] cards)
    {
        T[] copy = new T[cards.Length];
        Array.Copy(cards, copy, cards.Length);
        return copy;
    }
}
