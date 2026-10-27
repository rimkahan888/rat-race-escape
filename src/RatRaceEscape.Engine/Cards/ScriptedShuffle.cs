namespace RatRaceEscape.Engine;

/// <summary>
/// Applies fixed permutations in order. Each entry lists source indexes for
/// the new sequence, so <c>[2, 0, 1]</c> deals the third card, then the first,
/// then the second. When the script runs out, the next shuffle fails instead
/// of inventing an order.
/// </summary>
public sealed class ScriptedShuffle : IShuffle
{
    private readonly Queue<int[]> _orders;

    public ScriptedShuffle(params IReadOnlyList<int>[] orders)
    {
        ArgumentNullException.ThrowIfNull(orders);

        if (orders.Length == 0)
        {
            throw new ArgumentException("At least one scripted order is required.", nameof(orders));
        }

        Queue<int[]> pending = new();
        foreach (IReadOnlyList<int>? order in orders)
        {
            pending.Enqueue(CopyOrder(order));
        }

        _orders = pending;
    }

    public int Remaining => _orders.Count;

    public void Shuffle<T>(IList<T> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);

        if (_orders.Count == 0)
        {
            throw new InvalidOperationException("No scripted shuffles remain.");
        }

        int[] order = _orders.Peek();
        if (order.Length != cards.Count)
        {
            throw new InvalidOperationException(
                $"Scripted shuffle expected {cards.Count} cards and was given an order of {order.Length}.");
        }

        _orders.Dequeue();

        T[] next = new T[order.Length];
        for (int index = 0; index < order.Length; index++)
        {
            next[index] = cards[order[index]];
        }

        for (int index = 0; index < next.Length; index++)
        {
            cards[index] = next[index];
        }
    }

    private static int[] CopyOrder(IReadOnlyList<int>? order)
    {
        if (order is null)
        {
            throw new ArgumentException("A scripted order is required.", nameof(order));
        }

        if (order.Count == 0)
        {
            throw new ArgumentException("A scripted shuffle needs at least one index.", nameof(order));
        }

        int[] copy = new int[order.Count];
        bool[] seen = new bool[order.Count];
        for (int index = 0; index < order.Count; index++)
        {
            int source = order[index];
            if (source < 0 || source >= order.Count || seen[source])
            {
                throw new ArgumentException("A scripted shuffle must list each index once.", nameof(order));
            }

            seen[source] = true;
            copy[index] = source;
        }

        return copy;
    }
}
