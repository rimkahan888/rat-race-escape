namespace RatRaceEscape.Engine.Tests;

public class EngineInfoTests
{
    [Fact]
    public void ProductName_identifies_the_game()
    {
        Assert.Equal("Rat Race Escape", EngineInfo.ProductName);
    }

    [Fact]
    public void Version_reports_the_engine_assembly_version()
    {
        var assemblyVersion = typeof(EngineInfo).Assembly.GetName().Version;

        Assert.NotNull(assemblyVersion);
        Assert.Equal(assemblyVersion.ToString(3), EngineInfo.Version);
    }
}
