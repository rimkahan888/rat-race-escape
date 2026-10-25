using System.Net;
using System.Text;
using System.Text.Json;

namespace RatRaceEscape.Api.Tests;

public sealed class GetGameTests : IClassFixture<RatRaceEscapeApiFactory>
{
    private readonly HttpClient _client;

    public GetGameTests(RatRaceEscapeApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_returns_the_game_that_was_started()
    {
        HttpResponseMessage created = await PostAsync(OnePlayer("  Ava Stone  ", "night-baker", "Amber"));
        string createdBody = await created.Content.ReadAsStringAsync();
        Assert.True(created.StatusCode == HttpStatusCode.Created, createdBody);

        using JsonDocument createdDocument = JsonDocument.Parse(createdBody);
        string location = created.Headers.Location!.OriginalString;
        Guid id = createdDocument.RootElement.GetProperty("id").GetGuid();

        HttpResponseMessage response = await _client.GetAsync(new Uri(location, UriKind.Relative));
        string body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        using JsonDocument document = JsonDocument.Parse(body);
        JsonElement root = document.RootElement;
        JsonElement player = root.GetProperty("players")[0];
        Assert.Equal(id, root.GetProperty("id").GetGuid());
        Assert.Equal("RatRace", root.GetProperty("phase").GetString());
        Assert.Equal(0, root.GetProperty("currentTurnIndex").GetInt32());
        Assert.Equal("Ava Stone", player.GetProperty("name").GetString());
        Assert.Equal("night-baker", player.GetProperty("professionId").GetString());
        Assert.Equal("Night-Shift Baker", player.GetProperty("professionTitle").GetString());
        Assert.Equal("Amber", player.GetProperty("tokenColour").GetString());
        Assert.Equal(90_000, player.GetProperty("cashCents").GetInt64());
        Assert.Equal(63_000, player.GetProperty("monthlyCashFlowCents").GetInt64());
        Assert.False(player.GetProperty("passiveIncomeExceedsExpenses").GetBoolean());
        Assert.DoesNotContain(".", player.GetProperty("cashCents").GetRawText());

        HttpResponseMessage upper = await _client.GetAsync(new Uri($"/games/{id.ToString().ToUpperInvariant()}", UriKind.Relative));
        string upperBody = await upper.Content.ReadAsStringAsync();
        Assert.True(upper.StatusCode == HttpStatusCode.OK, upperBody);
        using JsonDocument upperDocument = JsonDocument.Parse(upperBody);
        Assert.Equal(id, upperDocument.RootElement.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Get_returns_each_game_on_its_own()
    {
        Guid ava = await StartAsync(OnePlayer("Ava", "night-baker", "Amber"));
        Guid bo = await StartAsync(OnePlayer("Bo", "municipal-archivist", "Cobalt"));

        using JsonDocument avaDocument = await GetJsonAsync(ava);
        using JsonDocument boDocument = await GetJsonAsync(bo);

        Assert.Equal("Ava", avaDocument.RootElement.GetProperty("players")[0].GetProperty("name").GetString());
        Assert.Equal(90_000, avaDocument.RootElement.GetProperty("players")[0].GetProperty("cashCents").GetInt64());
        Assert.Equal("Bo", boDocument.RootElement.GetProperty("players")[0].GetProperty("name").GetString());
        Assert.Equal(140_000, boDocument.RootElement.GetProperty("players")[0].GetProperty("cashCents").GetInt64());
        Assert.Equal("Cobalt", boDocument.RootElement.GetProperty("players")[0].GetProperty("tokenColour").GetString());
    }

    [Fact]
    public async Task Get_returns_404_when_the_id_is_unknown()
    {
        await AssertNotFound($"/games/{Guid.NewGuid()}");
    }

    [Fact]
    public async Task Get_returns_404_for_an_empty_id()
    {
        await AssertNotFound($"/games/{Guid.Empty}");
    }

    [Theory]
    [InlineData("/games/not-a-guid")]
    [InlineData("/games/123")]
    [InlineData("/games/night-baker")]
    public async Task Get_returns_404_when_the_id_is_not_a_guid(string path)
    {
        await AssertNotFound(path);
    }

    private async Task AssertNotFound(string path)
    {
        HttpResponseMessage response = await _client.GetAsync(new Uri(path, UriKind.Relative));
        string body = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.NotFound, body);
        Assert.DoesNotContain("players", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Night-Shift Baker", body, StringComparison.Ordinal);
    }

    private async Task<Guid> StartAsync(string json)
    {
        HttpResponseMessage response = await PostAsync(json);
        string body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, body);
        using JsonDocument document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("id").GetGuid();
    }

    private async Task<JsonDocument> GetJsonAsync(Guid id)
    {
        HttpResponseMessage response = await _client.GetAsync(new Uri($"/games/{id}", UriKind.Relative));
        string body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        return JsonDocument.Parse(body);
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
