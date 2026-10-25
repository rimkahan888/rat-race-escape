using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using RatRaceEscape.Engine;

namespace RatRaceEscape.Api.Tests;

public sealed class StartGameTests : IClassFixture<RatRaceEscapeApiFactory>
{
    private readonly RatRaceEscapeApiFactory _factory;
    private readonly HttpClient _client;

    public StartGameTests(RatRaceEscapeApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Start_returns_the_seated_game_and_stores_it()
    {
        HttpResponseMessage response = await PostAsync(
            """
            {
              "players": [
                { "name": "  Ava Stone  ", "professionId": "night-baker", "tokenColour": "Amber" },
                { "name": "Bo", "professionId": "municipal-archivist", "tokenColour": "Cobalt" }
              ]
            }
            """);

        string body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, body);

        using JsonDocument document = JsonDocument.Parse(body);
        JsonElement root = document.RootElement;
        Guid id = root.GetProperty("id").GetGuid();
        Assert.Equal($"/games/{id}", response.Headers.Location?.OriginalString);
        Assert.Equal("RatRace", root.GetProperty("phase").GetString());
        Assert.Equal(0, root.GetProperty("currentTurnIndex").GetInt32());

        JsonElement ava = root.GetProperty("players")[0];
        Assert.Equal("Ava Stone", ava.GetProperty("name").GetString());
        Assert.Equal("night-baker", ava.GetProperty("professionId").GetString());
        Assert.Equal("Night-Shift Baker", ava.GetProperty("professionTitle").GetString());
        Assert.Equal("Amber", ava.GetProperty("tokenColour").GetString());
        Assert.Equal(JsonValueKind.Number, ava.GetProperty("cashCents").ValueKind);
        Assert.Equal(90_000, ava.GetProperty("cashCents").GetInt64());
        Assert.Equal(0, ava.GetProperty("childCount").GetInt32());
        Assert.Equal(90_000, ava.GetProperty("totalAssetsCents").GetInt64());
        Assert.Equal(8_520_000, ava.GetProperty("totalLiabilitiesCents").GetInt64());
        Assert.Equal(280_000, ava.GetProperty("salaryCents").GetInt64());
        Assert.Equal(0, ava.GetProperty("passiveIncomeCents").GetInt64());
        Assert.Equal(63_000, ava.GetProperty("monthlyCashFlowCents").GetInt64());
        Assert.False(ava.GetProperty("passiveIncomeExceedsExpenses").GetBoolean());
        Assert.DoesNotContain(".", ava.GetProperty("cashCents").GetRawText());

        JsonElement bo = root.GetProperty("players")[1];
        Assert.Equal("Municipal Archivist", bo.GetProperty("professionTitle").GetString());
        Assert.Equal("Cobalt", bo.GetProperty("tokenColour").GetString());
        Assert.Equal(140_000, bo.GetProperty("cashCents").GetInt64());

        IGameRepository repository = _factory.Services.GetRequiredService<IGameRepository>();
        Game? stored = repository.Find(id);
        Assert.NotNull(stored);
        Assert.Equal("Ava Stone", stored.Players[0].Name);
        Assert.Equal(Money.FromDollars(900), stored.Players[0].Cash);
        Assert.Equal(GamePhase.RatRace, stored.Phase);
    }

    [Fact]
    public async Task Start_stores_each_game_under_its_own_id()
    {
        HttpResponseMessage first = await PostAsync(OnePlayer("Ava", "night-baker", "Amber"));
        HttpResponseMessage second = await PostAsync(OnePlayer("Bo", "municipal-archivist", "Cobalt"));
        string firstBody = await first.Content.ReadAsStringAsync();
        string secondBody = await second.Content.ReadAsStringAsync();
        Assert.True(first.StatusCode == HttpStatusCode.Created, firstBody);
        Assert.True(second.StatusCode == HttpStatusCode.Created, secondBody);

        using JsonDocument firstDocument = JsonDocument.Parse(firstBody);
        using JsonDocument secondDocument = JsonDocument.Parse(secondBody);
        Guid firstId = firstDocument.RootElement.GetProperty("id").GetGuid();
        Guid secondId = secondDocument.RootElement.GetProperty("id").GetGuid();
        Assert.NotEqual(firstId, secondId);

        IGameRepository repository = _factory.Services.GetRequiredService<IGameRepository>();
        Assert.Equal("Ava", repository.Find(firstId)?.Players[0].Name);
        Assert.Equal("Bo", repository.Find(secondId)?.Players[0].Name);
    }

    [Fact]
    public async Task Start_rejects_an_unknown_profession()
    {
        await AssertBadRequest(
            OnePlayer("Ava", "Night-Baker", "Amber"),
            "No profession with id 'Night-Baker'.");
    }

    [Fact]
    public async Task Start_rejects_a_duplicated_token_colour()
    {
        await AssertBadRequest(
            """
            {
              "players": [
                { "name": "Ava", "professionId": "night-baker", "tokenColour": "Amber" },
                { "name": "Bo", "professionId": "municipal-archivist", "tokenColour": "Amber" }
              ]
            }
            """,
            "Token colour Amber is used more than once.");
    }

    [Fact]
    public async Task Start_rejects_a_name_that_is_too_short()
    {
        await AssertBadRequest(
            OnePlayer("A", "night-baker", "Fern"),
            "Name must be between 2 and 40 characters.");
    }

    [Fact]
    public async Task Start_rejects_an_empty_player_list()
    {
        await AssertBadRequest(
            """
            { "players": [] }
            """,
            "A game needs at least one player.");
    }

    [Fact]
    public async Task Start_rejects_a_missing_body()
    {
        await AssertBadRequest("null", "Request body is required.");
    }

    [Fact]
    public async Task Start_rejects_a_missing_token_colour()
    {
        await AssertBadRequest(
            """
            { "players": [ { "name": "Ava", "professionId": "night-baker" } ] }
            """,
            "Token colour is required.");
    }

    [Fact]
    public async Task Start_rejects_an_unknown_token_colour()
    {
        HttpResponseMessage response = await PostAsync(OnePlayer("Ava", "night-baker", "Chartreuse"));
        string body = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.BadRequest, body);
        Assert.DoesNotContain("RatRace", body);
    }

    [Fact]
    public async Task Start_rejects_more_than_six_players()
    {
        string[] colours = ["Amber", "Cobalt", "Fern", "Coral", "Violet", "Slate", "Amber"];
        string players = string.Join(
            ",\n",
            colours.Select((colour, index) =>
                $$"""{ "name": "Player {{index + 1}}", "professionId": "night-baker", "tokenColour": "{{colour}}" }"""));

        HttpResponseMessage response = await PostAsync("{ \"players\": [" + players + "] }");
        string body = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.BadRequest, body);
        Assert.Contains("A game cannot have more than 6 players.", body);
        Assert.Contains("Token colour Amber is used more than once.", body);
    }

    private async Task AssertBadRequest(string json, string expectedError)
    {
        HttpResponseMessage response = await PostAsync(json);
        string body = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.BadRequest, body);
        using JsonDocument document = JsonDocument.Parse(body);
        string[] errors = document.RootElement.GetProperty("errors").EnumerateArray().Select(error => error.GetString()!).ToArray();
        Assert.Contains(expectedError, errors);
    }

    private async Task<HttpResponseMessage> PostAsync(string json)
    {
        using StringContent content = new(json, Encoding.UTF8, "application/json");
        return await _client.PostAsync(new Uri("/games", UriKind.Relative), content);
    }

    private static string OnePlayer(string name, string professionId, string tokenColour)
    {
        return $$"""
            { "players": [ { "name": "{{name}}", "professionId": "{{professionId}}", "tokenColour": "{{tokenColour}}" } ] }
            """;
    }
}
