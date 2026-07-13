using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;
using Orleans.Configuration;
using TricalRevive.GrainInterfaces;

var builder = Host.CreateApplicationBuilder(args);

const string connectionString =
    "Host=localhost;Port=5432;Database=tricalrevive;Username=tricaladmin;Password=tricalpass123";

builder.UseOrleans(siloBuilder => {
    siloBuilder.UseLocalhostClustering();

    // PostgreSQL을 그레인 상태 저장소로 등록.
    // "PlayerStore"라는 이름으로 등록해두면, 그레인 코드에서 이 이름으로 지정해서 사용합니다.
    siloBuilder.AddAdoNetGrainStorage("PlayerStore", options => {
        options.Invariant = "Npgsql";
        options.ConnectionString = connectionString;
    });
});

var host = builder.Build();
await host.StartAsync();

Console.WriteLine("=== TricalRevive Silo가 시작되었습니다 ===");
Console.WriteLine("PlayerGrain 테스트를 시작합니다...\n");

var client = host.Services.GetRequiredService<IClusterClient>();
var player = client.GetGrain<IPlayerGrain>("player-001");

Console.WriteLine($"초기 골드: {await player.GetGoldAsync()}");

await player.AddGoldAsync(1000);
Console.WriteLine($"1000골드 지급 후: {await player.GetGoldAsync()}");

await player.AddCharacterAsync("셀렌");
await player.AddCharacterAsync("스텔라");

var characters = await player.GetOwnedCharactersAsync();
Console.WriteLine($"보유 캐릭터: {string.Join(", ", characters)}");

Console.WriteLine("\n서버가 계속 실행됩니다. 종료하려면 Ctrl+C를 누르세요.");

await host.WaitForShutdownAsync();