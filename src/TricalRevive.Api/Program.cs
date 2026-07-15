using Elastic.Apm.NetCoreAll;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using StackExchange.Redis;
using TricalRevive.GrainInterfaces;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Elastic.Apm", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .Enrich.WithProperty("Application", "TricalRevive.Silo") // Api는 "TricalRevive.Api"
    .WriteTo.Console()
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://host.docker.internal:9200")) {
        AutoRegisterTemplate = true,
        IndexFormat = "tricalrevive-silo-logs-{0:yyyy.MM.dd}" // Api는 "tricalrevive-api-logs-{0:yyyy.MM.dd}"
    })
    .CreateLogger();


var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

builder.Host.UseOrleansClient(clientBuilder => {
    clientBuilder.UseLocalhostClustering();
});

builder.Services.AddOpenApi();

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect("host.docker.internal:6379"));

// 이 API 프로세스는 Orleans "클라이언트"로 동작합니다.
// Silo(서버)와는 별도의 프로세스이며, TCP로 게이트웨이 포트(기본 30000)를 통해 연결합니다.
builder.Host.UseOrleansClient(clientBuilder => {
    clientBuilder.UseAdoNetClustering(options =>
    {
        options.Invariant = "Npgsql";
        options.ConnectionString = "Host=host.docker.internal;Port=5432;Database=tricalrevive;Username=tricaladmin;Password=tricalpass123";
    });

    clientBuilder.Configure<ClusterOptions>(options =>
    {
        options.ClusterId = "tricalrevive-cluster";
        options.ServiceId = "tricalrevive";
    });
});

var app = builder.Build();

// Elastic APM 미들웨어 - HTTP 요청 하나하나를 트랜잭션으로 기록하고,
// 그 안에서 발생하는 Redis 호출 등을 자동으로 스팬(span)으로 잡아줍니다.
app.UseAllElasticApm(builder.Configuration);


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

app.MapGet("/leaderboard/gold", async (IConnectionMultiplexer redis) => {
    var db = redis.GetDatabase();
    var entries = await db.SortedSetRangeByScoreWithScoresAsync(
        "leaderboard:gold", order: Order.Descending, take: 10);

    var result = entries.Select(e => new { playerId = e.Element.ToString(), gold = (int)e.Score });
    return Results.Ok(result);
});

app.MapGet("/leaderboard/ssr", async (IConnectionMultiplexer redis) => {
    var db = redis.GetDatabase();
    var entries = await db.SortedSetRangeByScoreWithScoresAsync(
        "leaderboard:ssr", order: Order.Descending, take: 10);

    var result = entries.Select(e => new { playerId = e.Element.ToString(), ssrCount = (int)e.Score });
    return Results.Ok(result);
});

app.Run();