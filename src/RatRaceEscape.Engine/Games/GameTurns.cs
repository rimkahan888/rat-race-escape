namespace RatRaceEscape.Engine;

public sealed partial class Game
{
    /// <summary>
    /// Rolls one die for the current player and moves their token that many squares.
    /// Each payday the token lands on or passes pays that player's monthly cash flow once.
    /// The landed square is not resolved yet.
    /// </summary>
    public Game Roll(IDieRoller roller, int dieCount = 1)
    {
        ArgumentNullException.ThrowIfNull(roller);

        if (TurnPhase != TurnPhase.AwaitingRoll)
        {
            throw new TurnException(
                TurnPhase == TurnPhase.AwaitingResolve
                    ? "Resolve the space before rolling again."
                    : "End the turn before rolling again.");
        }

        if (dieCount != Dice.MinimumCount)
        {
            throw new TurnException("Roll one die.");
        }

        DiceRoll dice = Dice.Roll(roller, dieCount);
        int fromIndex = CurrentPlayer.BoardIndex;
        TrackMove move = RatRaceTrack.Move(fromIndex, dice.Total);
        Player paid = Payday.Collect(CurrentPlayer, move.Paydays.Count);
        TurnRoll roll = new(dice, fromIndex, paid.Cash - CurrentPlayer.Cash);
        Player moved = paid.WithBoardIndex(move.ToIndex);
        return WithPlayer(moved, TurnPhase.AwaitingResolve, roll, pendingDeck: null);
    }

    /// <summary>
    /// Resolves the square the current player landed on. Deal, market, and doodad
    /// squares ask for that deck. The other squares are acknowledged.
    /// </summary>
    public Game Resolve()
    {
        if (TurnPhase != TurnPhase.AwaitingResolve || LastRoll is null)
        {
            throw new TurnException(
                TurnPhase == TurnPhase.AwaitingEnd
                    ? "The space is already resolved."
                    : "Roll the dice before resolving the space.");
        }

        DeckHook? pendingDeck = RatRaceBoard.At(CurrentPlayer.BoardIndex).Kind switch
        {
            SpaceKind.Deal => DeckHook.Deal,
            SpaceKind.Market => DeckHook.Market,
            SpaceKind.Doodad => DeckHook.Doodad,
            _ => null,
        };

        return WithTurn(CurrentTurnIndex, TurnPhase.AwaitingEnd, LastRoll, pendingDeck);
    }

    /// <summary>
    /// Closes the turn and gives the dice to the next player.
    /// </summary>
    public Game EndTurn()
    {
        if (TurnPhase != TurnPhase.AwaitingEnd)
        {
            throw new TurnException(
                TurnPhase == TurnPhase.AwaitingRoll
                    ? "Roll the dice before ending the turn."
                    : "Resolve the space before ending the turn.");
        }

        int nextPlayer = (CurrentTurnIndex + 1) % Players.Count;
        return WithTurn(nextPlayer, TurnPhase.AwaitingRoll, lastRoll: null, pendingDeck: null);
    }

    private Game WithPlayer(Player player, TurnPhase turnPhase, TurnRoll? lastRoll, DeckHook? pendingDeck)
    {
        Player[] players = Players.ToArray();
        players[CurrentTurnIndex] = player;
        return new Game(Id, players, CurrentTurnIndex, Phase, turnPhase, lastRoll, pendingDeck);
    }

    private Game WithTurn(int currentTurnIndex, TurnPhase turnPhase, TurnRoll? lastRoll, DeckHook? pendingDeck)
    {
        return new Game(Id, Players, currentTurnIndex, Phase, turnPhase, lastRoll, pendingDeck);
    }
}
