using RatRaceEscape.Engine;

namespace RatRaceEscape.Api.Games;

public static class GameEndpoints
{
    public static IEndpointRouteBuilder MapGameEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/games", StartGame)
            .WithName("StartGame")
            .WithTags("Games");

        endpoints.MapGet("/games/{id:guid}", GetGame)
            .WithName("GetGame")
            .WithTags("Games");

        endpoints.MapPost("/games/{id:guid}/roll", Roll)
            .WithName("RollTurn")
            .WithTags("Games");

        endpoints.MapPost("/games/{id:guid}/resolve", ResolveTurn)
            .WithName("ResolveTurn")
            .WithTags("Games");

        endpoints.MapPost("/games/{id:guid}/end-turn", EndTurn)
            .WithName("EndTurn")
            .WithTags("Games");

        endpoints.MapPost("/games/{id:guid}/buy", Buy)
            .WithName("BuyCard")
            .WithTags("Games");

        endpoints.MapPost("/games/{id:guid}/pass", Pass)
            .WithName("PassCard")
            .WithTags("Games");

        endpoints.MapPost("/games/{id:guid}/sell-shares", SellShares)
            .WithName("SellShares")
            .WithTags("Games");

        endpoints.MapPost("/games/{id:guid}/sell-asset", SellAsset)
            .WithName("SellAsset")
            .WithTags("Games");

        endpoints.MapPost("/games/{id:guid}/borrow", Borrow)
            .WithName("Borrow")
            .WithTags("Games");

        endpoints.MapPost("/games/{id:guid}/repay", Repay)
            .WithName("Repay")
            .WithTags("Games");

        endpoints.MapPost("/games/{id:guid}/settle", Settle)
            .WithName("Settle")
            .WithTags("Games");

        endpoints.MapPost("/games/{id:guid}/leave", Leave)
            .WithName("LeaveRatRace")
            .WithTags("Games");

        return endpoints;
    }

    private static IResult SellShares(Guid id, BuyRequest? request, IGameRepository games)
    {
        return Play(id, games, game => game.SellShares(request?.Shares ?? 0));
    }

    private static IResult SellAsset(Guid id, IGameRepository games)
    {
        return Play(id, games, game => game.SellAsset());
    }

    private static IResult Borrow(Guid id, LoanRequest? request, IGameRepository games)
    {
        return Play(id, games, game => game.Borrow(request?.Increments ?? 0));
    }

    private static IResult Repay(Guid id, LoanRequest? request, IGameRepository games)
    {
        return Play(id, games, game => game.Repay(request?.Increments ?? 0));
    }

    private static IResult Settle(Guid id, IGameRepository games)
    {
        return Play(id, games, game => game.Settle());
    }

    private static IResult Leave(Guid id, IGameRepository games)
    {
        return Play(id, games, game => game.LeaveRatRace());
    }

    private static IResult Buy(Guid id, BuyRequest? request, IGameRepository games)
    {
        return Play(id, games, game => game.Buy(request?.Shares));
    }

    private static IResult Pass(Guid id, IGameRepository games)
    {
        return Play(id, games, game => game.Pass());
    }

    private static IResult Roll(Guid id, RollRequest? request, IGameRepository games, IDieRoller dice)
    {
        return Play(id, games, game => game.Roll(dice, request?.DieCount ?? 1));
    }

    private static IResult ResolveTurn(Guid id, ResolveRequest? request, IGameRepository games)
    {
        return Play(id, games, game => game.Resolve(request?.Choice ?? SpaceChoice.None));
    }

    private static IResult EndTurn(Guid id, IGameRepository games)
    {
        return Play(id, games, game => game.EndTurn());
    }

    private static IResult Play(Guid id, IGameRepository games, Func<Game, Game> action)
    {
        Game? game = games.Find(id);
        if (game is null)
        {
            return Results.NotFound();
        }

        try
        {
            Game updated = action(game);
            games.Save(updated);
            return Results.Ok(GameStateResponse.From(updated));
        }
        catch (TurnException exception)
        {
            return Results.Conflict(new GameRequestError(exception.Errors));
        }
    }

    private static IResult GetGame(Guid id, IGameRepository games)
    {
        Game? game = games.Find(id);
        if (game is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(GameStateResponse.From(game));
    }

    private static IResult StartGame(StartGameRequest? request, IGameRepository games, IShuffle shuffle)
    {
        if (request is null)
        {
            return Results.BadRequest(new GameRequestError(["Request body is required."]));
        }

        IReadOnlyList<StartPlayerRequest?> players = request.Players ?? [];
        List<string> errors = new();
        List<PlayerSetup> setups = new(players.Count);

        foreach (StartPlayerRequest? player in players)
        {
            if (player is null)
            {
                errors.Add("A player entry is required.");
                continue;
            }

            if (player.TokenColour is null)
            {
                errors.Add("Token colour is required.");
                continue;
            }

            setups.Add(new PlayerSetup(player.Name!, player.ProfessionId!, player.TokenColour.Value));
        }

        if (errors.Count > 0)
        {
            return Results.BadRequest(new GameRequestError(errors));
        }

        try
        {
            Game game = Game.Create(setups, shuffle);
            games.Add(game);
            return Results.Created($"/games/{game.Id}", GameStateResponse.From(game));
        }
        catch (GameValidationException exception)
        {
            return Results.BadRequest(new GameRequestError(exception.Errors));
        }
    }
}

public sealed record GameRequestError(IReadOnlyList<string> Errors);

public sealed record RollRequest(int? DieCount);

public sealed record ResolveRequest(SpaceChoice? Choice);

public sealed record BuyRequest(int? Shares);

public sealed record LoanRequest(int? Increments);
