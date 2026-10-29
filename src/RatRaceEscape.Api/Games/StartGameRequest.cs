using System.Text.Json.Serialization;

namespace RatRaceEscape.Api.Games;

public sealed record StartGameRequest(
    IReadOnlyList<StartPlayerRequest?>? Players,
    int? Seed,
    [property: JsonPropertyName("clone")] bool? VersusClone);

public sealed record StartPlayerRequest(string? Name, string? ProfessionId, string? TokenColour);
