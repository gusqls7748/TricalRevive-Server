using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using TricalRevive.GrainInterfaces;

var builder = WebApplication.CreateBuilder(args);

// 이 API 프로세스는 Orleans "클라이언트"로 동작합니다.
// Silo(서버)와는 별도의 프로세스이며, TCP로 게이트웨이 포트(기본 30000)를 통해 연결합니다.
builder.Host.UseOrleansClient(clientBuilder => {
    clientBuilder.UseLocalhostClustering();
});

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi();

// ── PlayerGrain 관련 엔드포인트 ──────────────────────────

app.MapGet("/players/{playerId}/gold", async (string playerId, IClusterClient client) => {
    var player = client.GetGrain<IPlayerGrain>(playerId);
    var gold = await player.GetGoldAsync();
    return Results.Ok(new { playerId, gold });
});

app.MapPost("/players/{playerId}/gold", async (string playerId, int amount, IClusterClient client) => {
    var player = client.GetGrain<IPlayerGrain>(playerId);
    var newGold = await player.AddGoldAsync(amount);
    return Results.Ok(new { playerId, gold = newGold });
});

app.MapGet("/players/{playerId}/characters", async (string playerId, IClusterClient client) => {
    var player = client.GetGrain<IPlayerGrain>(playerId);
    var characters = await player.GetOwnedCharactersAsync();
    return Results.Ok(new { playerId, characters });
});

// ── GachaGrain 관련 엔드포인트 ───────────────────────────

app.MapPost("/gacha/{playerId}/single", async (string playerId, IClusterClient client) => {
    var gacha = client.GetGrain<IGachaGrain>(playerId);
    try {
        var result = await gacha.PullSingleAsync();
        return Results.Ok(result);
    } catch (InvalidOperationException ex) {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/gacha/{playerId}/ten", async (string playerId, IClusterClient client) => {
    var gacha = client.GetGrain<IGachaGrain>(playerId);
    try {
        var result = await gacha.PullTenAsync();
        return Results.Ok(result);
    } catch (InvalidOperationException ex) {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/gacha/{playerId}/pity", async (string playerId, IClusterClient client) => {
    var gacha = client.GetGrain<IGachaGrain>(playerId);
    var pity = await gacha.GetPityCountAsync();
    return Results.Ok(new { playerId, pityCount = pity });
});

app.Run();