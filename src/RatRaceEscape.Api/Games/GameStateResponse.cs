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
    IReadOnlyList<PlayerStateResponse> Players,
    int? WinnerIndex,
    IReadOnlyList<FastTrackDealResponse> FastTrackDeals)
{
    public static GameStateResponse From(Game game)
    {
        ArgumentNullException.ThrowIfNull(game);

        PlayerStateResponse[] players = new PlayerStateResponse[game.Players.Count];
        for (int index = 0; index < game.Players.Count; index++)
        {
            Player player = game.Players[index];
            IncomeStatement income = IncomeStatement.For(player);
            CashFlow cashFlow = CashFlow.For(player);
            string spaceKind;
            string spaceTitle;
            if (player.Track == BoardTrack.FastTrack)
            {
                FastTrackSpace fastTrack = FastTrackBoard.At(player.BoardIndex);
                spaceKind = fastTrack.Kind.ToString();
                spaceTitle = fastTrack.Title;
            }
            else
            {
                BoardSpace space = RatRaceBoard.At(player.BoardIndex);
                spaceKind = space.Kind.ToString();
                spaceTitle = space.Title;
            }

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
                spaceKind,
                spaceTitle,
                player.SkipTurnsRemaining,
                player.ExtraDieTurnsRemaining,
                StocksOf(player),
                RealEstateOf(player),
                BusinessesOf(player),
                player.Track.ToString(),
                player.IsBankrupt,
                LoansOf(player));
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
            players,
            game.WinnerIndex,
            DealResponses());
    }

    private static FastTrackDealResponse[] DealResponses()
    {
        FastTrackDealResponse[] deals = new FastTrackDealResponse[FastTrackBoard.Deals.Count];
        for (int index = 0; index < deals.Length; index++)
        {
            FastTrackDeal deal = FastTrackBoard.Deals[index];
            deals[index] = new FastTrackDealResponse(
                deal.Id,
                deal.Title,
                deal.Summary,
                deal.Cost.Cents,
                deal.MonthlyCashFlow.Cents);
        }

        return deals;
    }

    private static LoanResponse[] LoansOf(Player player)
    {
        LoanResponse[] loans = new LoanResponse[player.BalanceSheet.OtherLoans.Count];
        for (int index = 0; index < loans.Length; index++)
        {
            LoanLiability loan = player.BalanceSheet.OtherLoans[index];
            loans[index] = new LoanResponse(loan.Name, loan.Balance.Cents, loan.MonthlyPayment.Cents);
        }

        return loans;
    }

    private static StockHoldingResponse[] StocksOf(Player player)
    {
        StockHoldingResponse[] stocks = new StockHoldingResponse[player.BalanceSheet.Stocks.Count];
        for (int index = 0; index < stocks.Length; index++)
        {
            StockHolding stock = player.BalanceSheet.Stocks[index];
            stocks[index] = new StockHoldingResponse(
                stock.Symbol,
                stock.Shares,
                stock.CostPerShare.Cents,
                stock.MonthlyDividendPerShare.Cents);
        }

        return stocks;
    }

    private static RealEstateHoldingResponse[] RealEstateOf(Player player)
    {
        RealEstateHoldingResponse[] properties = new RealEstateHoldingResponse[player.BalanceSheet.RealEstate.Count];
        for (int index = 0; index < properties.Length; index++)
        {
            RealEstateHolding property = player.BalanceSheet.RealEstate[index];
            properties[index] = new RealEstateHoldingResponse(
                property.Name,
                property.Cost.Cents,
                property.Mortgage.Cents,
                property.MonthlyCashFlow.Cents);
        }

        return properties;
    }

    private static BusinessHoldingResponse[] BusinessesOf(Player player)
    {
        BusinessHoldingResponse[] businesses = new BusinessHoldingResponse[player.BalanceSheet.Businesses.Count];
        for (int index = 0; index < businesses.Length; index++)
        {
            BusinessHolding business = player.BalanceSheet.Businesses[index];
            businesses[index] = new BusinessHoldingResponse(
                business.Name,
                business.Cost.Cents,
                business.Liability.Cents,
                business.MonthlyCashFlow.Cents);
        }

        return businesses;
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

public sealed record FastTrackDealResponse(
    string Id,
    string Title,
    string Summary,
    long CostCents,
    long MonthlyCashFlowCents);

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
    int ExtraDieTurnsRemaining,
    IReadOnlyList<StockHoldingResponse> Stocks,
    IReadOnlyList<RealEstateHoldingResponse> RealEstate,
    IReadOnlyList<BusinessHoldingResponse> Businesses,
    string Track,
    bool IsBankrupt,
    IReadOnlyList<LoanResponse> Loans);

public sealed record LoanResponse(string Name, long BalanceCents, long MonthlyPaymentCents);

public sealed record StockHoldingResponse(string Symbol, int Shares, long CostPerShareCents, long DividendPerShareCents);

public sealed record RealEstateHoldingResponse(string Name, long CostCents, long MortgageCents, long MonthlyCashFlowCents);

public sealed record BusinessHoldingResponse(string Name, long CostCents, long LiabilityCents, long MonthlyCashFlowCents);
