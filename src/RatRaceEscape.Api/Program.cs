using System.Text.Json.Serialization;
using RatRaceEscape.Api.Games;
using RatRaceEscape.Api.Health;
using RatRaceEscape.Engine;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
});
builder.Services.AddSingleton<IGameRepository, InMemoryGameRepository>();
builder.Services.AddSingleton<IDieRoller, RandomDieRoller>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.MapHealthEndpoints();
app.MapGameEndpoints();

app.Run();

/// <summary>
/// Exposed so the API integration tests can host the application.
/// </summary>
public partial class Program
{
}
