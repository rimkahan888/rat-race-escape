namespace RatRaceEscape.Engine;

/// <summary>
/// One visible action for the clone. The caller repeats this until the clone's
/// turn is over, so each roll, card, repayment, and exit can be shown.
/// </summary>
public static class CloneTurn
{
    private static readonly Money Cushion = Money.FromDollars(1_000);

    public static bool IsTheirTurn(Game game)
    {
        ArgumentNullException.ThrowIfNull(game);
        return game.CloneIndex == game.CurrentTurnIndex
            && game.Phase == GamePhase.RatRace
            && !game.CurrentPlayer.IsBankrupt;
    }

    public static Game Step(Game game, IDieRoller? dice = null)
    {
        ArgumentNullException.ThrowIfNull(game);

        if (game.Phase is GamePhase.Over or GamePhase.Won)
        {
            throw new TurnException("The game is already over.");
        }

        if (game.CloneIndex == game.CurrentTurnIndex
            && game.CurrentPlayer.IsBankrupt
            && game.TurnPhase == TurnPhase.AwaitingEnd)
        {
            return game.EndTurn();
        }

        if (!IsTheirTurn(game))
        {
            throw new TurnException("It is not the clone's turn.");
        }

        if (game.TurnPhase == TurnPhase.AwaitingRoll)
        {
            return Open(game, dice ?? new RandomDieRoller());
        }

        if (game.TurnPhase == TurnPhase.AwaitingResolve)
        {
            return Resolve(game);
        }

        return Close(game);
    }

    public static bool KeepsCashFlow(Player player, Money monthlyChange)
    {
        ArgumentNullException.ThrowIfNull(player);
        Money next = CashFlow.RawMonthly(player) + monthlyChange;
        return !next.IsNegative;
    }

    private static Game Open(Game game, IDieRoller dice)
    {
        if (RatRaceExit.CanLeave(game.CurrentPlayer))
        {
            return game.LeaveRatRace();
        }

        if (CanRepay(game.CurrentPlayer))
        {
            return game.Repay(1);
        }

        if (game.CurrentPlayer.Track == BoardTrack.FastTrack && AffordableDeal(game.CurrentPlayer) is FastTrackDeal deal)
        {
            return game.BuyFastTrackDeal(deal.Id);
        }

        int count = game.CurrentPlayer.ExtraDieTurnsRemaining > 0 ? 2 : 1;
        return game.Roll(dice, count);
    }

    private static Game Resolve(Game game)
    {
        if (game.CurrentPlayer.Track == BoardTrack.FastTrack)
        {
            FastTrackKind kind = FastTrackBoard.At(game.CurrentPlayer.BoardIndex).Kind;
            if (kind == FastTrackKind.Dream)
            {
                Money price = FastTrackBoard.PriceAt(game.CurrentPlayer.BoardIndex);
                SpaceChoice choice = game.CurrentPlayer.Cash >= price
                    ? SpaceChoice.ClaimDream
                    : SpaceChoice.Decline;
                return game.Resolve(choice);
            }

            return ResolveOrSettle(game, SpaceChoice.None);
        }

        SpaceKind space = RatRaceBoard.At(game.CurrentPlayer.BoardIndex).Kind;
        if (space == SpaceKind.Charity)
        {
            Money donation = Charity.Donation(IncomeStatement.For(game.CurrentPlayer).TotalIncome);
            SpaceChoice choice = game.CurrentPlayer.Cash >= donation + Cushion
                ? SpaceChoice.Donate
                : SpaceChoice.Decline;
            return game.Resolve(choice);
        }

        if (space == SpaceKind.Deal)
        {
            SpaceChoice choice = game.CurrentPlayer.Cash >= Money.FromDollars(3_000)
                ? SpaceChoice.BigDeal
                : SpaceChoice.SmallDeal;
            return game.Resolve(choice);
        }

        return ResolveOrSettle(game, SpaceChoice.None);
    }

    private static Game ResolveOrSettle(Game game, SpaceChoice choice)
    {
        try
        {
            return game.Resolve(choice);
        }
        catch (TurnException exception) when (IsBill(exception))
        {
            return game.Settle();
        }
    }

    private static Game Close(Game game)
    {
        if (TryBuy(game) is Game bought)
        {
            return bought;
        }

        if (game.Drawn is not null)
        {
            return game.Pass();
        }

        if (CanRepay(game.CurrentPlayer))
        {
            return game.Repay(1);
        }

        if (RatRaceExit.CanLeave(game.CurrentPlayer))
        {
            return game.LeaveRatRace();
        }

        if (game.CurrentPlayer.Track == BoardTrack.FastTrack && AffordableDeal(game.CurrentPlayer) is FastTrackDeal deal)
        {
            return game.BuyFastTrackDeal(deal.Id);
        }

        return game.EndTurn();
    }

    private static Game? TryBuy(Game game)
    {
        if (game.Drawn?.SmallDeal is SmallDeal stock && stock.Kind == SmallDealKind.Stock)
        {
            if (!stock.PricePerShare.IsPositive || !KeepsCashFlow(game.CurrentPlayer, Money.Zero))
            {
                return null;
            }

            int shares = (int)(game.CurrentPlayer.Cash.Cents / stock.PricePerShare.Cents);
            if (shares < 1)
            {
                return null;
            }

            return game.Buy(shares);
        }

        Money down = DownPayment(game);
        Money flow = CardFlow(game);
        if (!down.IsPositive)
        {
            return null;
        }

        if (game.CurrentPlayer.Cash < down || !KeepsCashFlow(game.CurrentPlayer, flow))
        {
            return null;
        }

        return game.Buy(shares: null);
    }

    private static Money DownPayment(Game game)
    {
        if (game.Drawn?.SmallDeal is SmallDeal small && small.Kind != SmallDealKind.Stock)
        {
            return small.DownPayment;
        }

        if (game.Drawn?.BigDeal is BigDeal big)
        {
            return big.DownPayment;
        }

        return Money.Zero;
    }

    private static Money CardFlow(Game game)
    {
        if (game.Drawn?.SmallDeal is SmallDeal small && small.Kind != SmallDealKind.Stock)
        {
            return small.MonthlyCashFlow;
        }

        if (game.Drawn?.BigDeal is BigDeal big)
        {
            return big.MonthlyCashFlow;
        }

        return Money.Zero;
    }

    private static FastTrackDeal? AffordableDeal(Player player)
    {
        FastTrackDeal? best = null;
        foreach (FastTrackDeal deal in FastTrackBoard.Deals)
        {
            if (player.Cash < deal.Cost + Cushion || !KeepsCashFlow(player, deal.MonthlyCashFlow))
            {
                continue;
            }

            if (best is null || deal.MonthlyCashFlow > best.MonthlyCashFlow)
            {
                best = deal;
            }
        }

        return best;
    }

    private static bool CanRepay(Player player)
    {
        LoanLiability? bank = null;
        foreach (LoanLiability loan in player.BalanceSheet.OtherLoans)
        {
            if (loan.Name.Equals(BankLoan.Name, StringComparison.OrdinalIgnoreCase))
            {
                bank = loan;
                break;
            }
        }

        return bank is not null && player.Cash >= BankLoan.Increment + Cushion;
    }

    private static bool IsBill(TurnException exception)
    {
        foreach (string error in exception.Errors)
        {
            if (error.Contains("doodad", StringComparison.Ordinal)
                || error.Contains("downsized", StringComparison.Ordinal)
                || error.Contains("dry dock", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
