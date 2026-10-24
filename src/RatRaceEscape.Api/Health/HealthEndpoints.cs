using RatRaceEscape.Engine;

namespace RatRaceEscape.Api.Health;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health", () => Results.Ok(new HealthResponse(
                Status: "ok",
                Product: EngineInfo.ProductName,
                EngineVersion: EngineInfo.Version)))
            .WithName("GetHealth")
            .WithTags("Health");

        return endpoints;
    }
}

public sealed record HealthResponse(string Status, string Product, string EngineVersion);
