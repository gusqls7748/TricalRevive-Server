using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Hosting;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect("localhost:6379"));

const string connectionString =
    "Host=localhost;Port=5432;Database=tricalrevive;Username=tricaladmin;Password=tricalpass123";

builder.UseOrleans(siloBuilder => {
    siloBuilder.UseLocalhostClustering();

    siloBuilder.AddAdoNetGrainStorage("PlayerStore", options => {
        options.Invariant = "Npgsql";
        options.ConnectionString = connectionString;
    });
});

var host = builder.Build();

Console.WriteLine("=== TricalRevive Silo가 시작되었습니다 ===");
Console.WriteLine("API 서버(TricalRevive.Api)로부터의 연결을 기다립니다...\n");

await host.RunAsync();