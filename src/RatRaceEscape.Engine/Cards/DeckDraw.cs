namespace RatRaceEscape.Engine;

/// <summary>
/// The card taken from the top of a deck, and the deck that remains.
/// </summary>
public sealed class DeckDraw<T>
    where T : notnull
{
    public DeckDraw(T card, Deck<T> deck)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(deck);
        Card = card;
        Deck = deck;
    }

    public T Card { get; }

    public Deck<T> Deck { get; }
}
