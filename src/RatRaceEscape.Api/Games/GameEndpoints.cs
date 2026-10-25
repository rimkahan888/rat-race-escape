using RatRaceEscape.Engine;

namespace RatRaceEscape.Api.Games;

public static class GameEndpoints
{
    public static IEndpointRouteBuilder MapGameEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/games", StartGame)
            .WithName("StartGame")
            .WithTags("Games");

        return endpoints;
    }

    private static IResult StartGame(StartGameRequest? request, IGameRepository games)
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
            Game game = Game.Create(setups);
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
