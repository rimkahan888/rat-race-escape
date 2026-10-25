using RatRaceEscape.Engine;

namespace RatRaceEscape.Api.Games;

public sealed record StartGameRequest(IReadOnlyList<StartPlayerRequest?>? Players);

public sealed record StartPlayerRequest(string? Name, string? ProfessionId, TokenColour? TokenColour);
