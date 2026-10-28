using System.Net;
using System.Text;
using System.Text.Json;

namespace RatRaceEscape.Api.Tests;

public sealed class DayFiveApiTests : IClassFixture<RatRaceEscapeApiFactory>
{
    private readonly RatRaceEscapeApiFactory _factory;
    private readonly HttpClient _client;

    public DayFiveApiTests(RatRaceEscapeApiFactory factory)
    {
        _factory = factory;
        _factory.Dice.Clear();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task A_new_game_is_in_the_rat_race_with_no_loans_and_no_winner()
    {
        JsonElement root = await StartAsync(OnePlayer());
        JsonElement player = root.GetProperty("players")[0];

        Assert.Equal("RatRace", root.GetProperty("phase").GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("winnerIndex").ValueKind);
        Assert.Equal("RatRace", player.GetProperty("track").GetString());
        Assert.False(player.GetProperty("isBankrupt").GetBoolean());
        Assert.Equal(0, player.GetProperty("loans").GetArrayLength());
    }

    [Fact]
    public async Task Borrow_and_repay_stack_one_bank_loan()
    {
        Guid id = Id(await StartAsync(OnePlayer()));

        JsonElement borrowed = await PostJsonAsync($"/games/{id}/borrow", """{ "increments": 2 }""");
        JsonElement player = borrowed.GetProperty("players")[0];
        JsonElement loan = player.GetProperty("loans")[0];

        Assert.Equal(290_000, player.GetProperty("cashCents").GetInt64());
        Assert.Equal(43_000, player.GetProperty("monthlyCashFlowCents").GetInt64());
        Assert.Equal("Bank", loan.GetProperty("name").GetString());
        Assert.Equal(200_000, loan.GetProperty("balanceCents").GetInt64());
        Assert.Equal(20_000, loan.GetProperty("monthlyPaymentCents").GetInt64());
        Assert.Equal("AwaitingRoll", borrowed.GetProperty("turnPhase").GetString());

        JsonElement repaid = await PostJsonAsync($"/games/{id}/repay", """{ "increments": 1 }""");
        JsonElement left = repaid.GetProperty("players")[0];
        Assert.Equal(190_000, left.GetProperty("cashCents").GetInt64());
        Assert.Equal(100_000, left.GetProperty("loans")[0].GetProperty("balanceCents").GetInt64());
        Assert.Equal(10_000, left.GetProperty("loans")[0].GetProperty("monthlyPaymentCents").GetInt64());

        JsonElement cleared = await PostJsonAsync($"/games/{id}/repay", """{ "increments": 1 }""");
        Assert.Equal(90_000, cleared.GetProperty("players")[0].GetProperty("cashCents").GetInt64());
        Assert.Equal(0, cleared.GetProperty("players")[0].GetProperty("loans").GetArrayLength());
        Assert.Equal(63_000, cleared.GetProperty("players")[0].GetProperty("monthlyCashFlowCents").GetInt64());

        await AssertConflict($"/games/{id}/repay", """{ "increments": 1 }""", "There is no bank loan to repay.");
        await AssertConflict($"/games/{id}/borrow", """{ "increments": 0 }""", "Loan increments must be greater than zero.");
    }

    [Fact]
    public async Task Sell_shares_into_the_pier_bid()
    {
        Guid id = Id(await StartAsync(OnePlayer()));
        _factory.Dice.Enqueue(2, 2, 1);

        await PostEmptyAsync($"/games/{id}/roll");
        await PostJsonAsync($"/games/{id}/resolve", """{ "choice": "SmallDeal" }""");
        await PostEmptyAsync($"/games/{id}/pass");
        await PostEmptyAsync($"/games/{id}/end-turn");

        await PostEmptyAsync($"/games/{id}/roll");
        JsonElement offered = await PostJsonAsync($"/games/{id}/resolve", """{ "choice": "SmallDeal" }""");
        Assert.Equal("north-pier", offered.GetProperty("drawn").GetProperty("id").GetString());
        await AssertConflict($"/games/{id}/sell-shares", """{ "shares": 1 }""", "There is no share bid to sell against.");

        JsonElement bought = await PostJsonAsync($"/games/{id}/buy", """{ "shares": 4 }""");
        Assert.Equal(80_000, bought.GetProperty("players")[0].GetProperty("cashCents").GetInt64());
        await PostEmptyAsync($"/games/{id}/end-turn");

        await PostEmptyAsync($"/games/{id}/roll");
        JsonElement bid = await PostEmptyAsync($"/games/{id}/resolve");
        Assert.Equal("pier-bid", bid.GetProperty("drawn").GetProperty("id").GetString());
        Assert.Equal(4_000, bid.GetProperty("drawn").GetProperty("priceCents").GetInt64());

        JsonElement sold = await PostJsonAsync($"/games/{id}/sell-shares", """{ "shares": 4 }""");
        JsonElement player = sold.GetProperty("players")[0];
        Assert.Equal(96_000, player.GetProperty("cashCents").GetInt64());
        Assert.Equal(0, player.GetProperty("stocks").GetArrayLength());
        Assert.Equal(0, player.GetProperty("passiveIncomeCents").GetInt64());
        Assert.Equal(JsonValueKind.Null, sold.GetProperty("drawn").ValueKind);
        await AssertConflict($"/games/{id}/sell-shares", """{ "shares": 1 }""", "There is no share bid to sell against.");
    }

    [Fact]
    public async Task Settle_bankrupts_a_player_who_cannot_pay_hours_cut()
    {
        Guid id = Id(await StartAsync(OnePlayer()));
        _factory.Dice.Enqueue(6, 6, 1);

        await PostEmptyAsync($"/games/{id}/roll");
        await PostEmptyAsync($"/games/{id}/resolve");
        await PostEmptyAsync($"/games/{id}/end-turn");
        await PostEmptyAsync($"/games/{id}/roll");
        await PostEmptyAsync($"/games/{id}/resolve");
        await PostEmptyAsync($"/games/{id}/end-turn");

        JsonElement landed = await PostEmptyAsync($"/games/{id}/roll");
        Assert.Equal("Downsized", landed.GetProperty("players")[0].GetProperty("spaceKind").GetString());
        Assert.Equal(216_000, landed.GetProperty("players")[0].GetProperty("cashCents").GetInt64());
        await AssertConflict($"/games/{id}/resolve", null, "Cash cannot cover the downsized expenses.");

        JsonElement settled = await PostEmptyAsync($"/games/{id}/settle");
        JsonElement player = settled.GetProperty("players")[0];
        Assert.True(player.GetProperty("isBankrupt").GetBoolean());
        Assert.Equal(0, player.GetProperty("cashCents").GetInt64());
        Assert.Equal(0, player.GetProperty("skipTurnsRemaining").GetInt32());
        Assert.Equal("RatRace", settled.GetProperty("phase").GetString());
        Assert.Equal("AwaitingEnd", settled.GetProperty("turnPhase").GetString());

        JsonElement over = await PostEmptyAsync($"/games/{id}/end-turn");
        Assert.Equal("Over", over.GetProperty("phase").GetString());
        Assert.Equal(JsonValueKind.Null, over.GetProperty("winnerIndex").ValueKind);
        await AssertConflict($"/games/{id}/roll", null, "The game is already over.");
        await AssertConflict($"/games/{id}/borrow", """{ "increments": 1 }""", "The game is already over.");
    }

    [Fact]
    public async Task Leaving_before_passive_income_covers_the_bills_is_a_conflict()
    {
        Guid id = Id(await StartAsync(OnePlayer()));

        await AssertConflict($"/games/{id}/leave", null, "Passive income does not yet cover the bills.");
        await AssertConflict($"/games/{id}/sell-asset", null, "Roll the dice before selling.");
        await AssertConflict($"/games/{id}/settle", null, "Roll the dice before settling.");
        JsonElement game = await GetAsync(id);
        Assert.Equal("RatRace", game.GetProperty("players")[0].GetProperty("track").GetString());
        Assert.Equal("RatRace", game.GetProperty("phase").GetString());
    }

    private async Task AssertConflict(string path, string? json, string expectedError)
    {
        HttpResponseMessage response = await PostAsync(path, json);
        string body = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.Conflict, body);
        using JsonDocument document = JsonDocument.Parse(body);
        string[] errors = document.RootElement.GetProperty("errors").EnumerateArray().Select(error => error.GetString()!).ToArray();
        Assert.Contains(expectedError, errors);
    }

    private async Task<JsonElement> StartAsync(string json)
    {
        HttpResponseMessage response = await PostAsync("/games", json);
        string body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, body);
        return Read(body);
    }

    private async Task<JsonElement> GetAsync(Guid id)
    {
        HttpResponseMessage response = await _client.GetAsync(new Uri($"/games/{id}", UriKind.Relative));
        string body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        return Read(body);
    }

    private async Task<JsonElement> PostJsonAsync(string path, string json)
    {
        HttpResponseMessage response = await PostAsync(path, json);
        string body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        return Read(body);
    }

    private async Task<JsonElement> PostEmptyAsync(string path)
    {
        HttpResponseMessage response = await _client.PostAsync(new Uri(path, UriKind.Relative), content: null);
        string body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        return Read(body);
    }

    private static JsonElement Read(string body)
    {
        using JsonDocument document = JsonDocument.Parse(body);
        return document.RootElement.Clone();
    }

    private async Task<HttpResponseMessage> PostAsync(string path, string? json)
    {
        using StringContent? content = json is null ? null : new StringContent(json, Encoding.UTF8, "application/json");
        return await _client.PostAsync(new Uri(path, UriKind.Relative), content);
    }

    private static Guid Id(JsonElement game)
    {
        return game.GetProperty("id").GetGuid();
    }

    private static string OnePlayer()
    {
        return """
            { "players": [ { "name": "Ava", "professionId": "night-baker", "tokenColour": "Amber" } ] }
            """;
    }
}
