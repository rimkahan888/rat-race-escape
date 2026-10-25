using RatRaceEscape.Engine;

namespace RatRaceEscape.Api.Games;

public sealed record GameStateResponse(
    Guid Id,
    string Phase,
    int CurrentTurnIndex,
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
                cashFlow.PassiveIncomeExceedsExpenses);
        }

        return new GameStateResponse(game.Id, game.Phase.ToString(), game.CurrentTurnIndex, players);
    }
}

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
    bool PassiveIncomeExceedsExpenses);
