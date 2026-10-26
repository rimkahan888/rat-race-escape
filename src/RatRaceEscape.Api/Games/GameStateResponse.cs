using RatRaceEscape.Engine;

namespace RatRaceEscape.Api.Games;

public sealed record GameStateResponse(
    Guid Id,
    string Phase,
    string TurnPhase,
    int CurrentTurnIndex,
    TurnRollResponse? LastRoll,
    string? PendingDeck,
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
            players);
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
