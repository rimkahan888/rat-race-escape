namespace RatRaceEscape.Api.Games;

public sealed record StartGameRequest(IReadOnlyList<StartPlayerRequest?>? Players, int? Seed);

public sealed record StartPlayerRequest(string? Name, string? ProfessionId, string? TokenColour);
