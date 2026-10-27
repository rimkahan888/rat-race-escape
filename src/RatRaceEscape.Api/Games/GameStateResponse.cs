using RatRaceEscape.Engine;

namespace RatRaceEscape.Api.Games;

public sealed record GameStateResponse(
    Guid Id,
    string Phase,
    string TurnPhase,
    int CurrentTurnIndex,
    TurnRollResponse? LastRoll,
    string? PendingDeck,
    DrawnCardResponse? Drawn,
    IReadOnlyList<PlayerStateResponse> Players)
{
    public static GameStateResponse From(Game game)
    {
        ArgumentNullException.ThrowIfNull(game);

        PlayerStateResponse[] players = new PlayerStateResponse[game.Players.Count];
        for (int index = 0; index < game.Players.Count; index++)
        {
            Player player = game.Players[index];
            IncomeStatement income = IncomeStatement.For(player);
            CashFlow cashFlow = CashFlow.From(income);
            BoardSpace space = RatRaceBoard.At(player.BoardIndex);
            players[index] = new PlayerStateResponse(
                player.Name,
                player.Profession.Id,
                player.Profession.Title,
                player.TokenColour.ToString(),
                player.Cash.Cents,
                player.ChildCount,
                player.BalanceSheet.TotalAssets.Cents,
                player.BalanceSheet.TotalLiabilities.Cents,
                income.Salary.Cents,
                income.PassiveIncome.Cents,
                income.TotalIncome.Cents,
                income.TotalExpenses.Cents,
                cashFlow.Monthly.Cents,
                cashFlow.PassiveIncomeExceedsExpenses,
                player.BoardIndex,
                space.Kind.ToString(),
                space.Title,
                player.SkipTurnsRemaining,
                player.ExtraDieTurnsRemaining);
        }

        TurnRoll? roll = game.LastRoll;
        TurnRollResponse? lastRoll = roll is null
            ? null
            : new TurnRollResponse(roll.Faces, roll.Total, roll.FromIndex, roll.ToIndex, roll.PaydayCash.Cents);

        return new GameStateResponse(
            game.Id,
            game.Phase.ToString(),
            game.TurnPhase.ToString(),
            game.CurrentTurnIndex,
            lastRoll,
            game.PendingDeck?.ToString(),
            DrawnCardResponse.From(game.Drawn),
            players);
    }
}

public sealed record DrawnCardResponse(
    string Deck,
    string Id,
    string Title,
    string Summary,
    string? Kind,
    long? CostCents,
    string? Symbol,
    long? PricePerShareCents,
    long? DividendPerShareCents,
    long? DownPaymentCents,
    long? LoanCents,
    long? MonthlyCashFlowCents,
    string? AssetName,
    bool? PerShare,
    long? PriceCents,
    long? CashDeltaCents)
{
    public static DrawnCardResponse? From(DrawnCard? drawn)
    {
        if (drawn is null)
        {
            return null;
        }

        if (drawn.SmallDeal is SmallDeal small)
        {
            return small.Kind == SmallDealKind.Stock
                ? Card(drawn, "Stock", symbol: small.Symbol, pricePerShare: small.PricePerShare, dividend: small.DividendPerShare)
                : Card(drawn, "Property", downPayment: small.DownPayment, loan: small.Mortgage, cashFlow: small.MonthlyCashFlow);
        }

        if (drawn.BigDeal is BigDeal big)
        {
            string kind = big.Kind == BigDealKind.Business ? "Business" : "Property";
            return Card(drawn, kind, downPayment: big.DownPayment, loan: big.Loan, cashFlow: big.MonthlyCashFlow);
        }

        if (drawn.Market is MarketCard market)
        {
            return market.Kind == MarketKind.Event
                ? Card(drawn, "Event", cashDelta: market.CashDelta)
                : Card(drawn, "Offer", assetName: market.AssetName, perShare: market.PerShare, price: market.Price);
        }

        return Card(drawn, kind: null, cost: drawn.Doodad!.Cost);
    }

    private static DrawnCardResponse Card(
        DrawnCard drawn,
        string? kind,
        Money? cost = null,
        string? symbol = null,
        Money? pricePerShare = null,
        Money? dividend = null,
        Money? downPayment = null,
        Money? loan = null,
        Money? cashFlow = null,
        string? assetName = null,
        bool? perShare = null,
        Money? price = null,
        Money? cashDelta = null)
    {
        return new DrawnCardResponse(
            drawn.Deck.ToString(),
            drawn.Id,
            drawn.Title,
            drawn.Summary,
            kind,
            cost?.Cents,
            symbol,
            pricePerShare?.Cents,
            dividend?.Cents,
            downPayment?.Cents,
            loan?.Cents,
            cashFlow?.Cents,
            assetName,
            perShare,
            price?.Cents,
            cashDelta?.Cents);
    }
}

public sealed record TurnRollResponse(
    IReadOnlyList<int> Faces,
    int Total,
    int FromIndex,
    int ToIndex,
    long PaydayCashCents);

public sealed record PlayerStateResponse(
    string Name,
    string ProfessionId,
    string ProfessionTitle,
    string TokenColour,
    long CashCents,
    int ChildCount,
    long TotalAssetsCents,
    long TotalLiabilitiesCents,
    long SalaryCents,
    long PassiveIncomeCents,
    long TotalIncomeCents,
    long TotalExpensesCents,
    long MonthlyCashFlowCents,
    bool PassiveIncomeExceedsExpenses,
    int BoardIndex,
    string SpaceKind,
    string SpaceTitle,
    int SkipTurnsRemaining,
    int ExtraDieTurnsRemaining);
