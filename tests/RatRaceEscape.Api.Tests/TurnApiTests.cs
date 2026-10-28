using System.Net;
using System.Text;
using System.Text.Json;

namespace RatRaceEscape.Api.Tests;

public sealed class TurnApiTests : IClassFixture<RatRaceEscapeApiFactory>
{
    private readonly RatRaceEscapeApiFactory _factory;
    private readonly HttpClient _client;

    public TurnApiTests(RatRaceEscapeApiFactory factory)
    {
        _factory = factory;
        _factory.Dice.Clear();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task A_new_game_is_waiting_on_the_offer_table()
    {
        JsonElement root = await StartAsync(OnePlayer("Ava", "night-baker", "Amber"));

        Assert.Equal("RatRace", root.GetProperty("phase").GetString());
        Assert.Equal("AwaitingRoll", root.GetProperty("turnPhase").GetString());
        Assert.Equal(0, root.GetProperty("currentTurnIndex").GetInt32());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("lastRoll").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("pendingDeck").ValueKind);

        JsonElement player = root.GetProperty("players")[0];
        Assert.Equal(0, player.GetProperty("boardIndex").GetInt32());
        Assert.Equal("Deal", player.GetProperty("spaceKind").GetString());
        Assert.Equal("Offer Table", player.GetProperty("spaceTitle").GetString());
        Assert.Equal(0, player.GetProperty("skipTurnsRemaining").GetInt32());
        Assert.Equal(0, player.GetProperty("extraDieTurnsRemaining").GetInt32());
        Assert.Equal(90_000, player.GetProperty("cashCents").GetInt64());
    }

    [Fact]
    public async Task Roll_resolve_and_end_turn_play_a_lap_through_the_api()
    {
        Guid id = Id(await StartAsync(TwoPlayers()));
        _factory.Dice.Enqueue(3);

        JsonElement charity = await PostJsonAsync($"/games/{id}/roll", """{ "dieCount": 1 }""");
        Assert.Equal("AwaitingResolve", charity.GetProperty("turnPhase").GetString());
        Assert.Equal(0, charity.GetProperty("currentTurnIndex").GetInt32());
        Assert.Equal(3, charity.GetProperty("players")[0].GetProperty("boardIndex").GetInt32());
        Assert.Equal("Charity", charity.GetProperty("players")[0].GetProperty("spaceKind").GetString());
        Assert.Equal("Mutual Aid", charity.GetProperty("players")[0].GetProperty("spaceTitle").GetString());
        Assert.Equal(90_000, charity.GetProperty("players")[0].GetProperty("cashCents").GetInt64());
        Assert.Equal(0, charity.GetProperty("lastRoll").GetProperty("paydayCashCents").GetInt64());
        Assert.Equal(3, charity.GetProperty("lastRoll").GetProperty("total").GetInt32());
        Assert.Equal(0, charity.GetProperty("lastRoll").GetProperty("fromIndex").GetInt32());
        Assert.Equal(3, charity.GetProperty("lastRoll").GetProperty("toIndex").GetInt32());
        Assert.Equal(3, charity.GetProperty("lastRoll").GetProperty("faces")[0].GetInt32());

        await AssertConflict($"/games/{id}/resolve", """{ "choice": "None" }""", "Charity needs a donate or decline choice.");
        JsonElement stillOpen = await GetAsync(id);
        Assert.Equal("AwaitingResolve", stillOpen.GetProperty("turnPhase").GetString());
        Assert.Equal(90_000, stillOpen.GetProperty("players")[0].GetProperty("cashCents").GetInt64());

        JsonElement donated = await PostJsonAsync($"/games/{id}/resolve", """{ "choice": "Donate" }""");
        Assert.Equal("AwaitingEnd", donated.GetProperty("turnPhase").GetString());
        Assert.Equal(62_000, donated.GetProperty("players")[0].GetProperty("cashCents").GetInt64());
        Assert.Equal(3, donated.GetProperty("players")[0].GetProperty("extraDieTurnsRemaining").GetInt32());
        Assert.Equal(0, donated.GetProperty("players")[1].GetProperty("extraDieTurnsRemaining").GetInt32());
        Assert.Equal(JsonValueKind.Null, donated.GetProperty("pendingDeck").ValueKind);

        JsonElement boWaiting = await PostEmptyAsync($"/games/{id}/end-turn");
        Assert.Equal("AwaitingRoll", boWaiting.GetProperty("turnPhase").GetString());
        Assert.Equal(1, boWaiting.GetProperty("currentTurnIndex").GetInt32());
        Assert.Equal(JsonValueKind.Null, boWaiting.GetProperty("lastRoll").ValueKind);
        Assert.Equal("Bo", boWaiting.GetProperty("players")[1].GetProperty("name").GetString());

        _factory.Dice.Enqueue(1);
        JsonElement doodad = await PostJsonAsync($"/games/{id}/roll", "{}");
        Assert.Equal(1, doodad.GetProperty("currentTurnIndex").GetInt32());
        Assert.Equal(1, doodad.GetProperty("players")[1].GetProperty("boardIndex").GetInt32());
        Assert.Equal("Doodad", doodad.GetProperty("players")[1].GetProperty("spaceKind").GetString());
        Assert.Equal("Odd Bill", doodad.GetProperty("players")[1].GetProperty("spaceTitle").GetString());
        Assert.Equal(0, doodad.GetProperty("players")[1].GetProperty("extraDieTurnsRemaining").GetInt32());

        JsonElement hooked = await PostJsonAsync($"/games/{id}/resolve", """{ "choice": "None" }""");
        Assert.Equal("Doodad", hooked.GetProperty("pendingDeck").GetString());
        Assert.Equal("cracked-oven-door", hooked.GetProperty("drawn").GetProperty("id").GetString());
        Assert.Equal(18_000, hooked.GetProperty("drawn").GetProperty("costCents").GetInt64());
        Assert.Equal(122_000, hooked.GetProperty("players")[1].GetProperty("cashCents").GetInt64());
        Assert.Equal("AwaitingEnd", hooked.GetProperty("turnPhase").GetString());

        JsonElement avaAgain = await PostEmptyAsync($"/games/{id}/end-turn");
        Assert.Equal(0, avaAgain.GetProperty("currentTurnIndex").GetInt32());
        Assert.Equal(JsonValueKind.Null, avaAgain.GetProperty("pendingDeck").ValueKind);
        Assert.Equal("AwaitingRoll", avaAgain.GetProperty("turnPhase").GetString());

        _factory.Dice.Enqueue(2, 5);
        JsonElement passedPay = await PostJsonAsync($"/games/{id}/roll", """{ "dieCount": 2 }""");
        JsonElement ava = passedPay.GetProperty("players")[0];
        Assert.Equal(10, ava.GetProperty("boardIndex").GetInt32());
        Assert.Equal("Doodad", ava.GetProperty("spaceKind").GetString());
        Assert.Equal(63_000, passedPay.GetProperty("lastRoll").GetProperty("paydayCashCents").GetInt64());
        Assert.Equal(7, passedPay.GetProperty("lastRoll").GetProperty("total").GetInt32());
        Assert.Equal(125_000, ava.GetProperty("cashCents").GetInt64());
        Assert.Equal(2, ava.GetProperty("extraDieTurnsRemaining").GetInt32());
        Assert.Equal(122_000, passedPay.GetProperty("players")[1].GetProperty("cashCents").GetInt64());

        JsonElement resolved = await PostEmptyAsync($"/games/{id}/resolve");
        Assert.Equal("Doodad", resolved.GetProperty("pendingDeck").GetString());
        JsonElement closed = await PostEmptyAsync($"/games/{id}/end-turn");
        Assert.Equal(1, closed.GetProperty("currentTurnIndex").GetInt32());
        Assert.Equal(JsonValueKind.Null, closed.GetProperty("pendingDeck").ValueKind);

        JsonElement fetched = await GetAsync(id);
        Assert.Equal(121_000, fetched.GetProperty("players")[0].GetProperty("cashCents").GetInt64());
        Assert.Equal(10, fetched.GetProperty("players")[0].GetProperty("boardIndex").GetInt32());
        Assert.Equal(2, fetched.GetProperty("players")[0].GetProperty("extraDieTurnsRemaining").GetInt32());
        Assert.Equal("AwaitingRoll", fetched.GetProperty("turnPhase").GetString());
    }

    [Fact]
    public async Task A_baby_space_adds_a_child_after_payday_is_collected()
    {
        Guid id = Id(await StartAsync(OnePlayer("Ava", "night-baker", "Amber")));
        _factory.Dice.Enqueue(5, 3);

        JsonElement market = await PostEmptyAsync($"/games/{id}/roll");
        Assert.Equal(5, market.GetProperty("players")[0].GetProperty("boardIndex").GetInt32());
        Assert.Equal("Market", market.GetProperty("players")[0].GetProperty("spaceKind").GetString());
        Assert.Equal("Corner Market", market.GetProperty("players")[0].GetProperty("spaceTitle").GetString());
        Assert.Equal(90_000, market.GetProperty("players")[0].GetProperty("cashCents").GetInt64());
        Assert.Equal(0, market.GetProperty("lastRoll").GetProperty("paydayCashCents").GetInt64());

        JsonElement offered = await PostEmptyAsync($"/games/{id}/resolve");
        Assert.Equal("Market", offered.GetProperty("pendingDeck").GetString());
        await PostEmptyAsync($"/games/{id}/end-turn");

        JsonElement arrival = await PostEmptyAsync($"/games/{id}/roll");
        JsonElement before = arrival.GetProperty("players")[0];
        Assert.Equal(8, before.GetProperty("boardIndex").GetInt32());
        Assert.Equal("Baby", before.GetProperty("spaceKind").GetString());
        Assert.Equal("New Arrival", before.GetProperty("spaceTitle").GetString());
        Assert.Equal(0, before.GetProperty("childCount").GetInt32());
        Assert.Equal(63_000, before.GetProperty("monthlyCashFlowCents").GetInt64());
        Assert.Equal(63_000, arrival.GetProperty("lastRoll").GetProperty("paydayCashCents").GetInt64());
        Assert.Equal(153_000, before.GetProperty("cashCents").GetInt64());

        JsonElement parent = (await PostEmptyAsync($"/games/{id}/resolve")).GetProperty("players")[0];
        Assert.Equal(1, parent.GetProperty("childCount").GetInt32());
        Assert.Equal(35_000, parent.GetProperty("monthlyCashFlowCents").GetInt64());
        Assert.Equal(153_000, parent.GetProperty("cashCents").GetInt64());
        Assert.Equal(JsonValueKind.Null, (await GetAsync(id)).GetProperty("pendingDeck").ValueKind);
    }

    [Fact]
    public async Task Downsized_collects_expenses_and_a_solo_player_skips_both_turns()
    {
        Guid id = Id(await StartAsync(OnePlayer("Ava", "night-baker", "Amber")));
        _factory.Dice.Enqueue(6, 6, 6, 6, 6, 6, 1);

        for (int turn = 0; turn < 6; turn++)
        {
            await PostEmptyAsync($"/games/{id}/roll");
            if (turn == 3)
            {
                await PostJsonAsync($"/games/{id}/resolve", """{ "choice": "SmallDeal" }""");
            }
            else
            {
                await PostEmptyAsync($"/games/{id}/resolve");
            }

            await PostEmptyAsync($"/games/{id}/end-turn");
        }

        JsonElement landed = await PostEmptyAsync($"/games/{id}/roll");
        Assert.Equal(13, landed.GetProperty("players")[0].GetProperty("boardIndex").GetInt32());
        Assert.Equal("Downsized", landed.GetProperty("players")[0].GetProperty("spaceKind").GetString());
        Assert.Equal("Hours Cut", landed.GetProperty("players")[0].GetProperty("spaceTitle").GetString());
        Assert.Equal(405_000, landed.GetProperty("players")[0].GetProperty("cashCents").GetInt64());
        Assert.Equal(0, landed.GetProperty("players")[0].GetProperty("skipTurnsRemaining").GetInt32());

        JsonElement benched = await PostEmptyAsync($"/games/{id}/resolve");
        Assert.Equal("AwaitingEnd", benched.GetProperty("turnPhase").GetString());
        Assert.Equal(188_000, benched.GetProperty("players")[0].GetProperty("cashCents").GetInt64());
        Assert.Equal(2, benched.GetProperty("players")[0].GetProperty("skipTurnsRemaining").GetInt32());

        JsonElement ready = await PostEmptyAsync($"/games/{id}/end-turn");
        Assert.Equal("AwaitingRoll", ready.GetProperty("turnPhase").GetString());
        Assert.Equal(0, ready.GetProperty("currentTurnIndex").GetInt32());
        Assert.Equal(0, ready.GetProperty("players")[0].GetProperty("skipTurnsRemaining").GetInt32());
        Assert.Equal(13, ready.GetProperty("players")[0].GetProperty("boardIndex").GetInt32());

        _factory.Dice.Enqueue(1);
        JsonElement next = await PostEmptyAsync($"/games/{id}/roll");
        Assert.Equal(14, next.GetProperty("players")[0].GetProperty("boardIndex").GetInt32());
        Assert.Equal("Deal", next.GetProperty("players")[0].GetProperty("spaceKind").GetString());
    }

    [Fact]
    public async Task Downsized_without_enough_cash_stays_on_the_space()
    {
        Guid id = Id(await StartAsync(OnePlayer("Ava", "night-baker", "Amber")));
        _factory.Dice.Enqueue(6, 6, 1);

        await PostEmptyAsync($"/games/{id}/roll");
        await PostEmptyAsync($"/games/{id}/resolve");
        await PostEmptyAsync($"/games/{id}/end-turn");
        await PostEmptyAsync($"/games/{id}/roll");
        await PostEmptyAsync($"/games/{id}/resolve");
        await PostEmptyAsync($"/games/{id}/end-turn");

        JsonElement landed = await PostEmptyAsync($"/games/{id}/roll");
        Assert.Equal(216_000, landed.GetProperty("players")[0].GetProperty("cashCents").GetInt64());

        await AssertConflict($"/games/{id}/resolve", null, "Cash cannot cover the downsized expenses.");

        JsonElement stuck = await GetAsync(id);
        Assert.Equal("AwaitingResolve", stuck.GetProperty("turnPhase").GetString());
        Assert.Equal(216_000, stuck.GetProperty("players")[0].GetProperty("cashCents").GetInt64());
        Assert.Equal(0, stuck.GetProperty("players")[0].GetProperty("skipTurnsRemaining").GetInt32());
        Assert.Equal(13, stuck.GetProperty("players")[0].GetProperty("boardIndex").GetInt32());
    }

    [Fact]
    public async Task Two_dice_before_charity_do_not_spend_the_queued_face()
    {
        Guid id = Id(await StartAsync(OnePlayer("Ava", "night-baker", "Amber")));
        _factory.Dice.Enqueue(4);

        await AssertConflict($"/games/{id}/roll", """{ "dieCount": 2 }""", "Two dice are available only while charity dice remain.");

        JsonElement rolled = await PostEmptyAsync($"/games/{id}/roll");
        Assert.Equal(4, rolled.GetProperty("players")[0].GetProperty("boardIndex").GetInt32());
        Assert.Equal(4, rolled.GetProperty("lastRoll").GetProperty("total").GetInt32());
        Assert.Equal("Deal", rolled.GetProperty("players")[0].GetProperty("spaceKind").GetString());
    }

    [Fact]
    public async Task A_second_roll_before_resolve_does_not_spend_the_next_face()
    {
        Guid id = Id(await StartAsync(TwoPlayers()));
        _factory.Dice.Enqueue(3, 4);

        await PostEmptyAsync($"/games/{id}/roll");
        await AssertConflict($"/games/{id}/roll", """{ "dieCount": 1 }""", "Resolve the space before rolling again.");

        await PostJsonAsync($"/games/{id}/resolve", """{ "choice": "Decline" }""");
        await PostEmptyAsync($"/games/{id}/end-turn");

        JsonElement bo = await PostEmptyAsync($"/games/{id}/roll");
        Assert.Equal(1, bo.GetProperty("currentTurnIndex").GetInt32());
        Assert.Equal(4, bo.GetProperty("players")[1].GetProperty("boardIndex").GetInt32());
        Assert.Equal("Deal", bo.GetProperty("players")[1].GetProperty("spaceKind").GetString());
        Assert.Equal(90_000, bo.GetProperty("players")[0].GetProperty("cashCents").GetInt64());
        Assert.Equal(0, bo.GetProperty("players")[0].GetProperty("extraDieTurnsRemaining").GetInt32());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public async Task Roll_rejects_a_die_count_other_than_one_or_two(int dieCount)
    {
        Guid id = Id(await StartAsync(OnePlayer("Ava", "night-baker", "Amber")));

        await AssertConflict($"/games/{id}/roll", $$"""{ "dieCount": {{dieCount}} }""", "Roll one or two dice.");

        JsonElement waiting = await GetAsync(id);
        Assert.Equal("AwaitingRoll", waiting.GetProperty("turnPhase").GetString());
        Assert.Equal(0, waiting.GetProperty("players")[0].GetProperty("boardIndex").GetInt32());
    }

    [Fact]
    public async Task End_and_resolve_before_a_roll_are_conflicts()
    {
        Guid id = Id(await StartAsync(OnePlayer("Ava", "night-baker", "Amber")));

        await AssertConflict($"/games/{id}/end-turn", null, "Roll the dice before ending the turn.");
        await AssertConflict($"/games/{id}/resolve", null, "Roll the dice before resolving the space.");
        Assert.Equal("AwaitingRoll", (await GetAsync(id)).GetProperty("turnPhase").GetString());
    }

    [Fact]
    public async Task An_unknown_choice_is_rejected_before_the_turn_changes()
    {
        Guid id = Id(await StartAsync(OnePlayer("Ava", "night-baker", "Amber")));
        _factory.Dice.Enqueue(1);
        await PostEmptyAsync($"/games/{id}/roll");

        HttpResponseMessage response = await PostAsync($"/games/{id}/resolve", """{ "choice": "Chartreuse" }""");
        string body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest, body);

        JsonElement game = await GetAsync(id);
        Assert.Equal("AwaitingResolve", game.GetProperty("turnPhase").GetString());
        Assert.Equal(1, game.GetProperty("players")[0].GetProperty("boardIndex").GetInt32());
    }

    [Fact]
    public async Task Buying_two_shares_records_the_lot_and_discards_the_stock()
    {
        Guid id = Id(await StartAsync(OnePlayer("Ava", "night-baker", "Amber")));
        _factory.Dice.Enqueue(2);

        await PostEmptyAsync($"/games/{id}/roll");
        JsonElement offered = await PostJsonAsync($"/games/{id}/resolve", """{ "choice": "SmallDeal" }""");
        Assert.Equal("lumen-glass", offered.GetProperty("drawn").GetProperty("id").GetString());
        Assert.Equal(0, offered.GetProperty("players")[0].GetProperty("stocks").GetArrayLength());

        await AssertConflict($"/games/{id}/buy", "{}", "Stock purchases need a share count.");
        JsonElement still = await GetAsync(id);
        Assert.Equal("lumen-glass", still.GetProperty("drawn").GetProperty("id").GetString());
        Assert.Equal(90_000, still.GetProperty("players")[0].GetProperty("cashCents").GetInt64());

        JsonElement bought = await PostJsonAsync($"/games/{id}/buy", """{ "shares": 2 }""");
        JsonElement player = bought.GetProperty("players")[0];
        JsonElement lot = player.GetProperty("stocks")[0];
        Assert.Equal(JsonValueKind.Null, bought.GetProperty("drawn").ValueKind);
        Assert.Equal(JsonValueKind.Null, bought.GetProperty("pendingDeck").ValueKind);
        Assert.Equal("AwaitingEnd", bought.GetProperty("turnPhase").GetString());
        Assert.Equal(88_000, player.GetProperty("cashCents").GetInt64());
        Assert.Equal(90_000, player.GetProperty("totalAssetsCents").GetInt64());
        Assert.Equal(0, player.GetProperty("passiveIncomeCents").GetInt64());
        Assert.Equal("LUMEN", lot.GetProperty("symbol").GetString());
        Assert.Equal(2, lot.GetProperty("shares").GetInt32());
        Assert.Equal(1_000, lot.GetProperty("costPerShareCents").GetInt64());
        Assert.Equal(0, lot.GetProperty("dividendPerShareCents").GetInt64());
        Assert.Equal(0, player.GetProperty("realEstate").GetArrayLength());
        Assert.Equal(0, player.GetProperty("businesses").GetArrayLength());

        JsonElement closed = await PostEmptyAsync($"/games/{id}/end-turn");
        Assert.Equal(2, closed.GetProperty("players")[0].GetProperty("stocks")[0].GetProperty("shares").GetInt32());
        Assert.Equal(JsonValueKind.Null, closed.GetProperty("drawn").ValueKind);
        await AssertConflict($"/games/{id}/buy", """{ "shares": 1 }""", "Roll the dice before buying.");
    }

    [Fact]
    public async Task Buying_a_big_deal_property_adds_the_mortgage_and_the_cash_flow()
    {
        Guid id = Id(await StartAsync(OnePlayer("Nia", "reef-surveyor", "Violet")));
        _factory.Dice.Enqueue(2);

        await PostEmptyAsync($"/games/{id}/roll");
        JsonElement offered = await PostJsonAsync($"/games/{id}/resolve", """{ "choice": "BigDeal" }""");
        Assert.Equal("brickworks-lofts", offered.GetProperty("drawn").GetProperty("id").GetString());
        long liabilities = offered.GetProperty("players")[0].GetProperty("totalLiabilitiesCents").GetInt64();

        await AssertConflict($"/games/{id}/buy", """{ "shares": 1 }""", "This offer is not sold in shares.");
        JsonElement bought = await PostEmptyAsync($"/games/{id}/buy");
        JsonElement player = bought.GetProperty("players")[0];
        JsonElement property = player.GetProperty("realEstate")[0];

        Assert.Equal(JsonValueKind.Null, bought.GetProperty("drawn").ValueKind);
        Assert.Equal(140_000, player.GetProperty("cashCents").GetInt64());
        Assert.Equal(540_000, player.GetProperty("totalAssetsCents").GetInt64());
        Assert.Equal(liabilities + 2_800_000, player.GetProperty("totalLiabilitiesCents").GetInt64());
        Assert.Equal(22_000, player.GetProperty("passiveIncomeCents").GetInt64());
        Assert.Equal(114_000, player.GetProperty("monthlyCashFlowCents").GetInt64());
        Assert.Equal("Brickworks Lofts", property.GetProperty("name").GetString());
        Assert.Equal(400_000, property.GetProperty("costCents").GetInt64());
        Assert.Equal(2_800_000, property.GetProperty("mortgageCents").GetInt64());
        Assert.Equal(22_000, property.GetProperty("monthlyCashFlowCents").GetInt64());
        Assert.Equal(0, player.GetProperty("businesses").GetArrayLength());
        Assert.Equal(0, player.GetProperty("stocks").GetArrayLength());
    }

    [Fact]
    public async Task A_down_payment_the_player_cannot_cover_leaves_the_offer()
    {
        Guid id = Id(await StartAsync(OnePlayer("Ava", "night-baker", "Amber")));
        _factory.Dice.Enqueue(2);

        await PostEmptyAsync($"/games/{id}/roll");
        await PostJsonAsync($"/games/{id}/resolve", """{ "choice": "BigDeal" }""");
        await AssertConflict($"/games/{id}/buy", null, "Cash cannot cover the down payment.");

        JsonElement stuck = await GetAsync(id);
        Assert.Equal("brickworks-lofts", stuck.GetProperty("drawn").GetProperty("id").GetString());
        Assert.Equal("AwaitingEnd", stuck.GetProperty("turnPhase").GetString());
        Assert.Equal(90_000, stuck.GetProperty("players")[0].GetProperty("cashCents").GetInt64());
        Assert.Equal(0, stuck.GetProperty("players")[0].GetProperty("realEstate").GetArrayLength());
    }

    [Fact]
    public async Task Passing_a_market_card_or_a_paid_bill_clears_it()
    {
        Guid id = Id(await StartAsync(OnePlayer("Ava", "night-baker", "Amber")));
        _factory.Dice.Enqueue(5);

        await PostEmptyAsync($"/games/{id}/roll");
        JsonElement offered = await PostEmptyAsync($"/games/{id}/resolve");
        Assert.Equal("pier-bid", offered.GetProperty("drawn").GetProperty("id").GetString());

        JsonElement passed = await PostEmptyAsync($"/games/{id}/pass");
        Assert.Equal(JsonValueKind.Null, passed.GetProperty("drawn").ValueKind);
        Assert.Equal("AwaitingEnd", passed.GetProperty("turnPhase").GetString());
        Assert.Equal(90_000, passed.GetProperty("players")[0].GetProperty("cashCents").GetInt64());
        await AssertConflict($"/games/{id}/pass", null, "There is no card to pass.");

        await PostEmptyAsync($"/games/{id}/end-turn");
        _factory.Dice.Enqueue(5);
        JsonElement landed = await PostEmptyAsync($"/games/{id}/roll");
        Assert.Equal(10, landed.GetProperty("players")[0].GetProperty("boardIndex").GetInt32());
        Assert.Equal(153_000, landed.GetProperty("players")[0].GetProperty("cashCents").GetInt64());

        JsonElement billed = await PostEmptyAsync($"/games/{id}/resolve");
        Assert.Equal("cracked-oven-door", billed.GetProperty("drawn").GetProperty("id").GetString());
        Assert.Equal(135_000, billed.GetProperty("players")[0].GetProperty("cashCents").GetInt64());

        JsonElement paid = await PostEmptyAsync($"/games/{id}/pass");
        Assert.Equal(JsonValueKind.Null, paid.GetProperty("drawn").ValueKind);
        Assert.Equal(135_000, paid.GetProperty("players")[0].GetProperty("cashCents").GetInt64());
        await AssertConflict($"/games/{id}/buy", null, "There is no offer to buy.");
    }

    [Fact]
    public async Task Buying_a_business_after_passing_the_earlier_big_deals_adds_the_note()
    {
        Guid id = Id(await StartAsync(OnePlayer("Nia", "reef-surveyor", "Violet")));
        _factory.Dice.Enqueue(2, 2, 3, 2, 2, 3);

        for (int deal = 0; deal < 5; deal++)
        {
            await PostEmptyAsync($"/games/{id}/roll");
            await PostJsonAsync($"/games/{id}/resolve", """{ "choice": "BigDeal" }""");
            await PostEmptyAsync($"/games/{id}/pass");
            await PostEmptyAsync($"/games/{id}/end-turn");
        }

        await PostEmptyAsync($"/games/{id}/roll");
        JsonElement offered = await PostJsonAsync($"/games/{id}/resolve", """{ "choice": "BigDeal" }""");
        Assert.Equal("kiln-and-clay", offered.GetProperty("drawn").GetProperty("id").GetString());
        long cash = offered.GetProperty("players")[0].GetProperty("cashCents").GetInt64();
        long liabilities = offered.GetProperty("players")[0].GetProperty("totalLiabilitiesCents").GetInt64();
        long monthly = offered.GetProperty("players")[0].GetProperty("monthlyCashFlowCents").GetInt64();

        JsonElement bought = await PostEmptyAsync($"/games/{id}/buy");
        JsonElement player = bought.GetProperty("players")[0];
        JsonElement business = player.GetProperty("businesses")[0];
        Assert.Equal("Kiln and Clay", business.GetProperty("name").GetString());
        Assert.Equal(300_000, business.GetProperty("costCents").GetInt64());
        Assert.Equal(0, business.GetProperty("liabilityCents").GetInt64());
        Assert.Equal(18_000, business.GetProperty("monthlyCashFlowCents").GetInt64());
        Assert.Equal(cash - 300_000, player.GetProperty("cashCents").GetInt64());
        Assert.Equal(liabilities, player.GetProperty("totalLiabilitiesCents").GetInt64());
        Assert.Equal(18_000, player.GetProperty("passiveIncomeCents").GetInt64());
        Assert.Equal(monthly + 18_000, player.GetProperty("monthlyCashFlowCents").GetInt64());
        Assert.Equal(0, player.GetProperty("realEstate").GetArrayLength());
        Assert.Equal(JsonValueKind.Null, bought.GetProperty("drawn").ValueKind);
    }

    [Fact]
    public async Task Buy_and_pass_before_a_roll_are_conflicts()
    {
        Guid id = Id(await StartAsync(OnePlayer("Ava", "night-baker", "Amber")));

        await AssertConflict($"/games/{id}/buy", null, "Roll the dice before buying.");
        await AssertConflict($"/games/{id}/pass", null, "Roll the dice before passing.");
        Assert.Equal("AwaitingRoll", (await GetAsync(id)).GetProperty("turnPhase").GetString());
    }

    [Theory]
    [InlineData("roll")]
    [InlineData("resolve")]
    [InlineData("end-turn")]
    [InlineData("buy")]
    [InlineData("pass")]
    [InlineData("sell-shares")]
    [InlineData("sell-asset")]
    [InlineData("borrow")]
    [InlineData("repay")]
    [InlineData("settle")]
    [InlineData("leave")]
    public async Task Turn_actions_return_404_when_the_game_is_unknown(string action)
    {
        await AssertNotFound($"/games/{Guid.NewGuid()}/{action}");
        await AssertNotFound($"/games/{Guid.Empty}/{action}");
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

    private async Task AssertNotFound(string path)
    {
        HttpResponseMessage response = await PostAsync(path, "{}");
        string body = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.NotFound, body);
        Assert.DoesNotContain("players", body, StringComparison.OrdinalIgnoreCase);
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

    private static string OnePlayer(string name, string professionId, string tokenColour)
    {
        return $$"""
            { "players": [ { "name": "{{name}}", "professionId": "{{professionId}}", "tokenColour": "{{tokenColour}}" } ] }
            """;
    }

    private static string TwoPlayers()
    {
        return """
            {
              "players": [
                { "name": "Ava", "professionId": "night-baker", "tokenColour": "Amber" },
                { "name": "Bo", "professionId": "municipal-archivist", "tokenColour": "Cobalt" }
              ]
            }
            """;
    }
}
