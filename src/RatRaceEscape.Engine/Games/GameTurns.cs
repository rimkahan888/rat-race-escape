namespace RatRaceEscape.Engine;

public sealed partial class Game
{
    /// <summary>
    /// Rolls one die for the current player, or two while charity dice remain.
    /// Using the turn spends one charity die whether the player rolls one or two.
    /// Each payday the token lands on or passes pays that player's monthly cash flow once.
    /// On the fast track, a payday pays passive income only. The landed square is not resolved yet.
    /// </summary>
    public Game Roll(IDieRoller roller, int dieCount = 1)
    {
        ArgumentNullException.ThrowIfNull(roller);
        RequireAction();

        if (TurnPhase != TurnPhase.AwaitingRoll)
        {
            throw new TurnException(
                TurnPhase == TurnPhase.AwaitingResolve
                    ? "Resolve the space before rolling again."
                    : "End the turn before rolling again.");
        }

        if (dieCount is < Dice.MinimumCount or > Dice.MaximumCount)
        {
            throw new TurnException("Roll one or two dice.");
        }

        if (dieCount > Dice.MinimumCount && CurrentPlayer.ExtraDieTurnsRemaining == 0)
        {
            throw new TurnException("Two dice are available only while charity dice remain.");
        }

        int extraDieTurns = CurrentPlayer.ExtraDieTurnsRemaining;
        DiceRoll dice = Dice.Roll(roller, dieCount);
        if (CurrentPlayer.Track == BoardTrack.FastTrack)
        {
            return RollFastTrack(dice, extraDieTurns);
        }

        int fromIndex = CurrentPlayer.BoardIndex;
        TrackMove move = RatRaceTrack.Move(fromIndex, dice.Total);
        Player paid = Payday.Collect(CurrentPlayer, move.Paydays.Count);
        TurnRoll roll = new(dice, fromIndex, paid.Cash - CurrentPlayer.Cash);
        Player moved = paid.WithBoardIndex(move.ToIndex);
        if (extraDieTurns > 0)
        {
            moved = moved.WithExtraDieTurns(extraDieTurns - 1);
        }

        return WithPlayer(moved, TurnPhase.AwaitingResolve, roll, pendingDeck: null, drawn: null, Piles);
    }

    private Game RollFastTrack(DiceRoll dice, int extraDieTurns)
    {
        int fromIndex = CurrentPlayer.BoardIndex;
        FastTrackMove move = FastTrackBoard.Move(fromIndex, dice.Total);
        Player paid = CollectFastPayday(CurrentPlayer, move.Paydays);
        TurnRoll roll = new(dice, fromIndex, move.ToIndex, paid.Cash - CurrentPlayer.Cash);
        Player moved = paid.WithBoardIndex(move.ToIndex);
        if (extraDieTurns > 0)
        {
            moved = moved.WithExtraDieTurns(extraDieTurns - 1);
        }

        return WithPlayer(moved, TurnPhase.AwaitingResolve, roll, pendingDeck: null, drawn: null, Piles);
    }

    private static Player CollectFastPayday(Player player, int paydayCount)
    {
        if (paydayCount == 0)
        {
            return player;
        }

        Money change = IncomeStatement.For(player).PassiveIncome * paydayCount;
        if (change.IsNegative && player.Cash < change.Negate())
        {
            throw new TurnException("Cash cannot cover the fast-track distribution.");
        }

        return player.WithCash(player.Cash + change);
    }

    private Game ResolveFastTrack(SpaceChoice choice)
    {
        FastTrackSpace space = FastTrackBoard.At(CurrentPlayer.BoardIndex);
        if (space.Kind == FastTrackKind.Dream)
        {
            return ResolveDream(choice);
        }

        if (choice != SpaceChoice.None)
        {
            throw new TurnException("This space does not take a choice.");
        }

        if (space.Kind == FastTrackKind.Windfall)
        {
            Player paid = CurrentPlayer.WithCash(CurrentPlayer.Cash + FastTrackBoard.Windfall);
            return WithPlayer(paid, TurnPhase.AwaitingEnd, LastRoll, pendingDeck: null, drawn: null, Piles);
        }

        if (space.Kind == FastTrackKind.Setback)
        {
            if (CurrentPlayer.Cash < FastTrackBoard.Setback)
            {
                throw new TurnException("Cash cannot cover the dry dock.");
            }

            Player payer = CurrentPlayer.WithCash(CurrentPlayer.Cash - FastTrackBoard.Setback);
            return WithPlayer(payer, TurnPhase.AwaitingEnd, LastRoll, pendingDeck: null, drawn: null, Piles);
        }

        return WithTurn(CurrentTurnIndex, TurnPhase.AwaitingEnd, LastRoll, pendingDeck: null, drawn: null, Piles);
    }

    private Game ResolveDream(SpaceChoice choice)
    {
        if (choice == SpaceChoice.Decline)
        {
            return WithTurn(CurrentTurnIndex, TurnPhase.AwaitingEnd, LastRoll, pendingDeck: null, drawn: null, Piles);
        }

        if (choice != SpaceChoice.ClaimDream)
        {
            throw new TurnException("A dream listing needs a claim or a decline.");
        }

        if (CurrentPlayer.Cash < FastTrackBoard.DreamPrice)
        {
            throw new TurnException("Cash cannot cover the dream.");
        }

        Player buyer = CurrentPlayer.WithCash(CurrentPlayer.Cash - FastTrackBoard.DreamPrice);
        Player[] players = Players.ToArray();
        players[CurrentTurnIndex] = buyer;
        return new Game(
            Id,
            players,
            CurrentTurnIndex,
            GamePhase.Won,
            TurnPhase.AwaitingEnd,
            LastRoll,
            pendingDeck: null,
            Piles,
            drawn: null,
            CurrentTurnIndex);
    }

    /// <summary>
    /// Resolves the square the current player landed on. An offer draws a small
    /// or big deal, whichever the player chooses. The market and an unplanned
    /// bill draw the next card of that deck. The bill is paid from cash on hand.
    /// Charity asks for a donation or a decline. A new arrival adds a child, up
    /// to the household cap. Hours cut collects total expenses and skips the
    /// player's next two turns. The other squares are acknowledged.
    /// </summary>
    public Game Resolve(SpaceChoice choice = SpaceChoice.None)
    {
        RequireAction();

        if (TurnPhase != TurnPhase.AwaitingResolve || LastRoll is null)
        {
            throw new TurnException(
                TurnPhase == TurnPhase.AwaitingEnd
                    ? "The space is already resolved."
                    : "Roll the dice before resolving the space.");
        }

        if (!Enum.IsDefined(choice))
        {
            throw new TurnException("The space choice is not one this turn understands.");
        }

        if (CurrentPlayer.Track == BoardTrack.FastTrack)
        {
            return ResolveFastTrack(choice);
        }

        BoardSpace space = RatRaceBoard.At(CurrentPlayer.BoardIndex);
        if (space.Kind == SpaceKind.Charity)
        {
            return ResolveCharity(choice);
        }

        if (space.Kind == SpaceKind.Deal)
        {
            return ResolveDeal(choice);
        }

        if (choice != SpaceChoice.None)
        {
            throw new TurnException("This space does not take a choice.");
        }

        if (space.Kind == SpaceKind.Baby)
        {
            Player parent = Baby.Arrive(CurrentPlayer);
            return WithPlayer(parent, TurnPhase.AwaitingEnd, LastRoll, pendingDeck: null, drawn: null, Piles);
        }

        if (space.Kind == SpaceKind.Downsized)
        {
            Player benched = Downsized.Apply(CurrentPlayer);
            return WithPlayer(benched, TurnPhase.AwaitingEnd, LastRoll, pendingDeck: null, drawn: null, Piles);
        }

        if (space.Kind == SpaceKind.Market)
        {
            return DrawMarket();
        }

        if (space.Kind == SpaceKind.Doodad)
        {
            return DrawDoodad();
        }

        return WithTurn(CurrentTurnIndex, TurnPhase.AwaitingEnd, LastRoll, pendingDeck: null, drawn: null, Piles);
    }

    private Game ResolveCharity(SpaceChoice choice)
    {
        if (choice == SpaceChoice.Decline)
        {
            return WithTurn(CurrentTurnIndex, TurnPhase.AwaitingEnd, LastRoll, pendingDeck: null, drawn: null, Piles);
        }

        if (choice != SpaceChoice.Donate)
        {
            throw new TurnException("Charity needs a donate or decline choice.");
        }

        Player donor = Charity.Donate(CurrentPlayer);
        return WithPlayer(donor, TurnPhase.AwaitingEnd, LastRoll, pendingDeck: null, drawn: null, Piles);
    }

    private Game ResolveDeal(SpaceChoice choice)
    {
        if (choice == SpaceChoice.SmallDeal)
        {
            DeckDraw<SmallDeal> drawn = Piles.SmallDeals.Draw();
            return WithTurn(
                CurrentTurnIndex,
                TurnPhase.AwaitingEnd,
                LastRoll,
                DeckHook.Deal,
                DrawnCard.From(drawn.Card),
                Piles.WithSmallDeals(drawn.Deck));
        }

        if (choice == SpaceChoice.BigDeal)
        {
            DeckDraw<BigDeal> drawn = Piles.BigDeals.Draw();
            return WithTurn(
                CurrentTurnIndex,
                TurnPhase.AwaitingEnd,
                LastRoll,
                DeckHook.Deal,
                DrawnCard.From(drawn.Card),
                Piles.WithBigDeals(drawn.Deck));
        }

        throw new TurnException("An offer needs a small-deal or big-deal choice.");
    }

    private Game DrawMarket()
    {
        DeckDraw<MarketCard> drawn = Piles.Market.Draw();
        CardPiles piles = Piles.WithMarket(drawn.Deck);
        IReadOnlyList<Player> players = drawn.Card.Kind switch
        {
            MarketKind.Event => MarketEvent.Apply(Players, drawn.Card.CashDelta),
            MarketKind.Split => StockSplit.ApplyAll(Players, drawn.Card.AssetName!),
            _ => Players,
        };

        return new Game(
            Id,
            players,
            CurrentTurnIndex,
            Phase,
            TurnPhase.AwaitingEnd,
            LastRoll,
            DeckHook.Market,
            piles,
            DrawnCard.From(drawn.Card));
    }

    private Game DrawDoodad()
    {
        DeckDraw<Doodad> drawn = Piles.Doodads.Draw();
        Player payer = DoodadCharge.Pay(CurrentPlayer, drawn.Card);
        return WithPlayer(
            payer,
            TurnPhase.AwaitingEnd,
            LastRoll,
            DeckHook.Doodad,
            DrawnCard.From(drawn.Card),
            Piles.WithDoodads(drawn.Deck));
    }

    /// <summary>
    /// Closes the turn and gives the dice to the next player who can take it.
    /// A drawn card goes onto its discard pile. A player with skipped turns
    /// misses this pass and loses one skipped turn. A bankrupt player is
    /// skipped for good. When nobody is left, the table is over.
    /// </summary>
    public Game EndTurn()
    {
        if (Phase is GamePhase.Over or GamePhase.Won)
        {
            throw new TurnException("The game is already over.");
        }

        if (TurnPhase != TurnPhase.AwaitingEnd)
        {
            throw new TurnException(
                TurnPhase == TurnPhase.AwaitingRoll
                    ? "Roll the dice before ending the turn."
                    : "Resolve the space before ending the turn.");
        }

        CardPiles piles = Discard(Piles, Drawn);
        if (!AnySolvent(Players))
        {
            return new Game(
                Id,
                Players,
                CurrentTurnIndex,
                GamePhase.Over,
                TurnPhase.AwaitingRoll,
                lastRoll: null,
                pendingDeck: null,
                piles,
                drawn: null,
                WinnerIndex);
        }

        Player[] players = Players.ToArray();
        int index = CurrentTurnIndex;
        int skipped = 0;
        int guardLimit = players.Length * 32;

        while (true)
        {
            index = (index + 1) % players.Length;
            if (players[index].IsBankrupt)
            {
                skipped++;
                if (skipped > guardLimit)
                {
                    throw new InvalidOperationException("Skip turns did not resolve.");
                }

                continue;
            }

            int skips = players[index].SkipTurnsRemaining;
            if (skips == 0)
            {
                break;
            }

            players[index] = players[index].WithSkipTurnsRemaining(skips - 1);
            skipped++;
            if (skipped > guardLimit)
            {
                throw new InvalidOperationException("Skip turns did not resolve.");
            }
        }

        return new Game(
            Id,
            players,
            index,
            Phase,
            TurnPhase.AwaitingRoll,
            lastRoll: null,
            pendingDeck: null,
            piles,
            drawn: null,
            WinnerIndex);
    }

    private static bool AnySolvent(IReadOnlyList<Player> players)
    {
        foreach (Player player in players)
        {
            if (!player.IsBankrupt)
            {
                return true;
            }
        }

        return false;
    }

    private static CardPiles Discard(CardPiles piles, DrawnCard? drawn)
    {
        if (drawn is null)
        {
            return piles;
        }

        if (drawn.SmallDeal is not null)
        {
            return piles.WithSmallDeals(piles.SmallDeals.Discard(drawn.SmallDeal));
        }

        if (drawn.BigDeal is not null)
        {
            return piles.WithBigDeals(piles.BigDeals.Discard(drawn.BigDeal));
        }

        if (drawn.Market is not null)
        {
            return piles.WithMarket(piles.Market.Discard(drawn.Market));
        }

        if (drawn.Doodad is not null)
        {
            return piles.WithDoodads(piles.Doodads.Discard(drawn.Doodad));
        }

        throw new InvalidOperationException("A drawn card is missing its source.");
    }

    private Game WithPlayer(
        Player player,
        TurnPhase turnPhase,
        TurnRoll? lastRoll,
        DeckHook? pendingDeck,
        DrawnCard? drawn,
        CardPiles piles)
    {
        Player[] players = Players.ToArray();
        players[CurrentTurnIndex] = player;
        return new Game(Id, players, CurrentTurnIndex, Phase, turnPhase, lastRoll, pendingDeck, piles, drawn, WinnerIndex);
    }

    private Game WithTurn(
        int currentTurnIndex,
        TurnPhase turnPhase,
        TurnRoll? lastRoll,
        DeckHook? pendingDeck,
        DrawnCard? drawn,
        CardPiles piles)
    {
        return new Game(Id, Players, currentTurnIndex, Phase, turnPhase, lastRoll, pendingDeck, piles, drawn, WinnerIndex);
    }
}
