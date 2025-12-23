using System.Collections.Immutable;
using Aspire.Hosting;
using CommunityToolkit.Aspire.Hosting.Dapr;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// ------------------------------------------------------------
// Paths (stabili)
// ------------------------------------------------------------
// Assumo che l'AppHost viva in: <repo>/src/AppHost
// e che lo lanci da lì (o che la WorkingDirectory sia quella).
var appHostRoot = Directory.GetCurrentDirectory();
var mountsRoot = Path.GetFullPath(Path.Combine(appHostRoot, "mounts"));

Directory.CreateDirectory(mountsRoot);

// Dapr components nel repo (evita C:\Users\...\ .dapr\components)
var daprComponentsDir = Path.Combine(mountsRoot, "dapr", "components");
Directory.CreateDirectory(daprComponentsDir);

var daprSidecarOptions = new DaprSidecarOptions
{
    // Dapr CLI accetta più path; qui ne passiamo uno (repo-based)
    ResourcesPaths = ImmutableHashSet.Create(daprComponentsDir)
};

// ------------------------------------------------------------
// Infra
// ------------------------------------------------------------
var kafka = builder.AddKafka("kafka-momentum")
    .WithContainerName("kafka-momentum")
    .WithKafkaUI();

var pgDbName = builder.Configuration["Pg:Database"] ?? "momentum";

var pgUser = builder.AddParameter("pg-user", builder.Configuration["Pg:User"] ?? "postgres");
var pgPass = builder.AddParameter("pg-pass", builder.Configuration["Pg:Password"] ?? "postgres", secret: true);
var pgDb   = builder.AddParameter("pg-db", pgDbName);

var timescale = builder.AddPostgres("timescale-momentum")
    .WithContainerName("timescale-momentum")
    .WithDataVolume()
    .WithImage("timescale/timescaledb:2.22.1-pg16")
    .WithPgWeb(pg => pg.WithHostPort(5050))
    .WithUserName(pgUser)
    .WithPassword(pgPass);

var momentumDb = timescale.AddDatabase(pgDbName);

var redis = builder.AddRedis("redis-momentum")
    .WithContainerName("redis-momentum")
    .WithRedisInsight();

// Se vuoi PubSub Dapr su Redis, occhio che "localhost:6379" è del container Dapr,
// non del tuo host. Qui di solito va usato il nome risorsa/host corretto.
// Per ora lo lascio commentato.
// var pubSub = builder.AddDaprPubSub("pubsub")
//     .WithMetadata("redisHost", "redis-momentum:6379")
//     .WaitFor(redis);

// ------------------------------------------------------------
// Apps
// ------------------------------------------------------------
var coreWeb = builder.AddProject<Projects.CoreWeb_Api>("core-web-api")
    .WithDaprSidecar(daprSidecarOptions)
    .WithReference(redis)
    .WithHttpEndpoint(port: 5080, targetPort: 8080, name: "http");

var ignite = builder.AddContainer("ignite-momentum", "apacheignite/ignite", "2.16.0")
    .WithContainerName("ignite-momentum")
    .WithEndpoint(11211, 11211, name: "client")
    .WithEndpoint(8088, 8080, name: "rest");

// Grafana: host 13000 -> container 3000
var grafana = builder.AddContainer("grafana-momentum", "grafana/grafana", "12.2")
    .WithContainerName("grafana-momentum")
    .WithEndpoint(13000, 3000, "http");

// Prometheus
var prometheusConfigDir = Path.Combine(mountsRoot, "prometheus");
Directory.CreateDirectory(prometheusConfigDir);

var prometheus = builder.AddContainer("prometheus-momentum", "prom/prometheus", "v3.6.0")
    .WithContainerName("prometheus-momentum")
    .WithEndpoint(9090, 9090, "http")
    .WithBindMount(prometheusConfigDir, "/etc/prometheus", isReadOnly: true)
    .WithArgs("--config.file=/etc/prometheus/prometheus.yml");

// Loki: host 13100 -> container 3100 (così non confligge con roba locale)
var loki = builder.AddContainer("loki-momentum", "grafana/loki", "3.5")
    .WithContainerName("loki-momentum")
    .WithEndpoint(13100, 3100, "http");

// Tempo
var tempoConfigDir = Path.Combine(mountsRoot, "tempo");
Directory.CreateDirectory(tempoConfigDir);

var tempo = builder.AddContainer("tempo-momentum", "grafana/tempo", "main-ed2862d")
    .WithContainerName("tempo-momentum")
    .WithEndpoint(name: "http", port: 13200, targetPort: 3200)
    .WithEndpoint(name: "otlp-grpc", port: 4317, targetPort: 4317)
    .WithEndpoint(name: "otlp-http", port: 4318, targetPort: 4318)
    .WithBindMount(tempoConfigDir, "/etc/tempo", isReadOnly: true)
    .WithVolume("tempo-data", "/var/tempo")
    .WithArgs("--config.file=/etc/tempo/tempo.yaml");

// Se Tempo ha problemi permessi sul volume, questo workaround funziona:
tempo.WithContainerRuntimeArgs("--user=root");

var identifier = builder.AddProject<Projects.Identifier_Api>("identifier-api")
    .WithDaprSidecar(daprSidecarOptions)
    .WithReference(kafka);

var streamer = builder.AddProject<Projects.Streamer_Api>("streamer-api")
    .WithDaprSidecar(daprSidecarOptions)
    .WithReference(kafka)
    .WithReference(timescale);

var notifier = builder.AddProject<Projects.Notifier_Api>("notifier-api")
    .WithDaprSidecar(daprSidecarOptions)
    .WithReference(kafka);

var backend = builder.AddProject<Projects.WebBackendCore_Api>("web-backend-core")
    .WithDaprSidecar(daprSidecarOptions);

var modularMonolith = builder.AddProject<Projects.ModularMonolith_Api>("modular-monolith")
    .WithDaprSidecar(daprSidecarOptions)
    .WithReference(identifier)
    .WithReference(streamer)
    .WithReference(notifier)
    .WithReference(backend);

// ------------------------------------------------------------
// Frontend (containerizzato come docker-compose)
// docker-compose:
//   context: .
//   dockerfile: src/web-core/Dockerfile
//   ports: 4200:80
// ------------------------------------------------------------
var repoRoot = Path.GetFullPath(Path.Combine(appHostRoot, "../..")); // da src/AppHost -> repo root

var webCore = builder.AddContainer("web-core", "web-core-dev", "local")
    .WithContainerName("web-core")
    // context repo root, dockerfile relativo al context
    .WithDockerfile(repoRoot, "src/web-core/Dockerfile")
    .WithHttpEndpoint(port: 4200, targetPort: 80, isProxied: false)
    .WithReference(coreWeb)
    .WaitFor(coreWeb);

builder.Build().Run();
