using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RatRaceEscape.Engine;

namespace RatRaceEscape.Api.Tests;

public sealed class RatRaceEscapeApiFactory : WebApplicationFactory<Program>
{
    public QueueDieRoller Dice { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDieRoller>();
            services.AddSingleton<IDieRoller>(Dice);
            services.RemoveAll<IShuffle>();
            services.AddSingleton<IShuffle, IdentityShuffle>();
        });
    }
}
