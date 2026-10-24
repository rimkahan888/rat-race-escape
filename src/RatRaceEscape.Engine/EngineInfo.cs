namespace RatRaceEscape.Engine;

/// <summary>
/// Identifies the rules engine. Money, professions, players, the board, and
/// turn resolution all live in this library so the HTTP host stays thin.
/// </summary>
public static class EngineInfo
{
    public const string ProductName = "Rat Race Escape";

    public static string Version =>
        typeof(EngineInfo).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
}
