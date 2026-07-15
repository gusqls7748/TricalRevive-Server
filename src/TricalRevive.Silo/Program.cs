using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Hosting;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using StackExchange.Redis;

const string connectionString =
    "Host=host.docker.internal;Port=5432;Database=tricalrevive;Username=tricaladmin;Password=tricalpass123";

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Elastic.Apm", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .Enrich.WithProperty("Application", "TricalRevive.Silo")
    .WriteTo.Console()
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://host.docker.internal:9200")) {
        AutoRegisterTemplate = true,
        IndexFormat = "tricalrevive-silo-logs-{0:yyyy.MM.dd}"
    })
    .CreateLogger();

try {
    Log.Information("TricalRevive Silo 시작 준비 중...");

    var builder = Host.CreateApplicationBuilder(args);

    builder.Logging.ClearProviders();
    builder.Services.AddSerilog();

    builder.Services.AddAllElasticApm();

    builder.Services.AddSingleton<IConnectionMultiplexer>(
        ConnectionMultiplexer.Connect("host.docker.internal:6379"));

    builder.UseOrleans(siloBuilder => {
        // UseLocalhostClustering()은 같은 머신·같은 프로세스 그룹 안에서만 동작하는
        // 개발용 설정이라, Silo와 Api가 서로 다른 Kubernetes 파드로 분리된 환경에서는
        // 서로를 찾을 수 없습니다. 이미 프로비저닝해둔 PostgreSQL의
        // OrleansMembershipTable을 이용한 ADO.NET 기반 클러스터링으로 대체합니다.
        siloBuilder.UseAdoNetClustering(options => {
            options.Invariant = "Npgsql";
            options.ConnectionString = connectionString;
        });

        siloBuilder.Configure<ClusterOptions>(options => {
            options.ClusterId = "tricalrevive-cluster";
            options.ServiceId = "tricalrevive";
        });

        siloBuilder.Configure<EndpointOptions>(options => {
            // Kubernetes 파드는 재시작될 때마다 IP가 바뀌므로,
            // Helm 차트에서 Downward API로 주입한 POD_IP 환경변수를
            // 클러스터 멤버십에 광고할 주소로 사용합니다.
            var podIp = Environment.GetEnvironmentVariable("POD_IP");
            if (!string.IsNullOrEmpty(podIp)) {
                options.AdvertisedIPAddress = System.Net.IPAddress.Parse(podIp);
            }
            options.SiloPort = 11111;
            options.GatewayPort = 30000;
        });

        siloBuilder.AddAdoNetGrainStorage("PlayerStore", options => {
            options.Invariant = "Npgsql";
            options.ConnectionString = connectionString;
        });
    });

    var host = builder.Build();

    Log.Information("TricalRevive Silo 시작됨. API 서버로부터의 연결을 기다립니다.");

    await host.RunAsync();
} catch (Exception ex) {
    Log.Fatal(ex, "Silo가 예기치 않게 종료되었습니다.");
} finally {
    Log.CloseAndFlush();
}