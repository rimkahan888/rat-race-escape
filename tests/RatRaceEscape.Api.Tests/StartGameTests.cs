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
    public async Task Start_seats_a_clone_with_the_same_profession()
    {
        HttpResponseMessage response = await PostAsync(
            """
            {
              "clone": true,
              "players": [
                { "name": "Ava", "professionId": "night-baker", "tokenColour": "Amber" }
              ]
            }
            """);

        string body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using JsonDocument document = JsonDocument.Parse(body);
        JsonElement players = document.RootElement.GetProperty("players");
        Assert.Equal(2, players.GetArrayLength());
        Assert.False(players[0].GetProperty("isClone").GetBoolean());
        Assert.True(players[1].GetProperty("isClone").GetBoolean());
        Assert.Equal("night-baker", players[1].GetProperty("professionId").GetString());
        Assert.Equal(players[0].GetProperty("cashCents").GetInt64(), players[1].GetProperty("cashCents").GetInt64());
        Assert.NotEqual(players[0].GetProperty("tokenColour").GetString(), players[1].GetProperty("tokenColour").GetString());

        Guid id = document.RootElement.GetProperty("id").GetGuid();
        HttpResponseMessage step = await _client.PostAsync(new Uri($"/games/{id}/clone-step", UriKind.Relative), null);
        string stepBody = await step.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Conflict, step.StatusCode);
        Assert.Contains("It is not the clone's turn.", stepBody, StringComparison.Ordinal);
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

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("at ", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Exception", body, StringComparison.Ordinal);
        using JsonDocument document = JsonDocument.Parse(body);
        Assert.Equal(400, document.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Token colour is not a playing colour.", document.RootElement.GetProperty("title").GetString());
        Assert.Contains("Chartreuse", document.RootElement.GetProperty("detail").GetString(), StringComparison.Ordinal);
        Assert.Contains("Amber", document.RootElement.GetProperty("detail").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_same_seed_repeats_the_first_roll()
    {
        const string Body = """
            {
              "seed": 42,
              "players": [
                { "name": "Ava", "professionId": "night-baker", "tokenColour": "Amber" }
              ]
            }
            """;

        HttpResponseMessage first = await PostAsync(Body);
        HttpResponseMessage second = await PostAsync(Body);
        string firstText = await first.Content.ReadAsStringAsync();
        string secondText = await second.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);

        using JsonDocument firstDocument = JsonDocument.Parse(firstText);
        using JsonDocument secondDocument = JsonDocument.Parse(secondText);
        Guid firstId = firstDocument.RootElement.GetProperty("id").GetGuid();
        Guid secondId = secondDocument.RootElement.GetProperty("id").GetGuid();
        Assert.Equal(42, firstDocument.RootElement.GetProperty("seed").GetInt32());
        Assert.NotEqual(firstId, secondId);

        HttpResponseMessage firstRoll = await _client.PostAsync(new Uri($"/games/{firstId}/roll", UriKind.Relative), null);
        HttpResponseMessage secondRoll = await _client.PostAsync(new Uri($"/games/{secondId}/roll", UriKind.Relative), null);
        string firstRollText = await firstRoll.Content.ReadAsStringAsync();
        string secondRollText = await secondRoll.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, firstRoll.StatusCode);
        using JsonDocument rolled = JsonDocument.Parse(firstRollText);
        using JsonDocument again = JsonDocument.Parse(secondRollText);
        Assert.Equal(
            rolled.RootElement.GetProperty("lastRoll").GetProperty("faces").ToString(),
            again.RootElement.GetProperty("lastRoll").GetProperty("faces").ToString());
        Assert.Equal(
            rolled.RootElement.GetProperty("players")[0].GetProperty("boardIndex").GetInt32(),
            again.RootElement.GetProperty("players")[0].GetProperty("boardIndex").GetInt32());
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
