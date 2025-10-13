

using Aspire.Hosting;
using Microsoft.Extensions.Hosting;

// Test di integrazione e verifica funzionamento CI/CD

var builder = DistributedApplication.CreateBuilder(args);


var kafka = builder.AddKafka("kafka-momentum")
    .WithContainerName("kafka-momentum")
    .WithKafkaUI();

var pgUser = builder.AddParameter("pg-user", builder.Configuration["Pg:User"] ?? "postgres");
var pgPass = builder.AddParameter("pg-pass", builder.Configuration["Pg:User"] ?? "postgres", secret: true);
var pgDb   = builder.AddParameter("pg-db", builder.Configuration["Pg:Database"] ?? "momentum");

var timescale = builder.AddPostgres("timescale-momentum")
    .WithContainerName("timescale-momentum")
    .WithDataVolume()
    .WithImage("timescale/timescaledb:2.22.1-pg16")
    //.WithBindMount("./mounts/time-scale/data", "/var/lib/postgresql/data")
    .WithPgWeb(pg => pg.WithHostPort(5050))
    .WithUserName(pgUser)          // <-- accetta IResourceBuilder<ParameterResource>
    .WithPassword(pgPass);

// Aggiungi il database (consigliato con AddDatabase)
var momentumDb = timescale.AddDatabase("momentum");

var redis = builder.AddRedis("redis-momentum")
            .WithContainerName("redis-momentum")
            .WithRedisInsight();

var pubSub = builder.AddDaprPubSub("pubsub")
                    .WithMetadata("redisHost", "localhost:6379")
                    .WaitFor(redis);

var ignite = builder.AddContainer("ignite-momentum", "apacheignite/ignite", "2.16.0")
    .WithContainerName("ignite-momentum")
    .WithEndpoint(11211, 11211, name: "client")
    .WithEndpoint(8080, 8080, "rest");

var grafana = builder.AddContainer("grafana-momentum", "grafana/grafana", "12.2")
    .WithContainerName("grafana-momentum")
    .WithEndpoint(3000, 3000, "http");

var prometheus = builder.AddContainer("prometheus-momentum", "prom/prometheus", "v3.6.0")
    .WithContainerName("prometheus-momentum")
    .WithEndpoint(9090, 9090, "http")
    .WithBindMount("./mounts/prometheus/prometheus.yml", "/etc/prometheus/prometheus.yml");

var loki = builder.AddContainer("loki-momentum", "grafana/loki", "3.5")
    .WithContainerName("loki-momentum")
    .WithEndpoint(3100, 3100, "http");

var tempo = builder.AddContainer("tempo-momentum", "grafana/tempo", "main-ed2862d")
    .WithContainerName("tempo-momentum")
    .WithEndpoint(name: "http", port: 3200, targetPort: 3200)
    .WithEndpoint(name: "otlp-grpc", port: 4317, targetPort: 4317)
    .WithEndpoint(name: "otlp-http", port: 4318, targetPort: 4318)
    .WithBindMount("./mounts/tempo/tempo.yaml", "/etc/tempo/tempo.yaml", isReadOnly: true)
    .WithArgs("--config.file=/etc/tempo/tempo.yaml")
    .WithBindMount("./mounts/tempo/data", "/var/tempo");

var identifier = builder.AddProject<Projects.Identifier_Api>("identifier-api")
    .WithDaprSidecar()
    .WithReference(kafka);

var streamer = builder.AddProject<Projects.Streamer_Api>("streamer-api")
    .WithDaprSidecar()
    .WithReference(kafka)
    .WithReference(timescale)
    .WithEnvironment("ConnectionStrings__Timescale", "Host=timescale;Username=postgres;Password=postgres;Database=momentum");

var notifier = builder.AddProject<Projects.Notifier_Api>("notifier-api")
    .WithDaprSidecar()
    .WithReference(kafka);

var backend = builder.AddProject<Projects.WebBackendCore_Api>("web-backend-core")
    .WithDaprSidecar();

var angular = builder.AddExecutable("web-core", "npm", "run", "start")
    .WithWorkingDirectory("../src/web-core")
    .WithHttpEndpoint(port: 4200, targetPort: 4200, isProxied: false);
    //.WithExternalHttpEndpoints()
    //.PublishAsDockerFile();

// var frontend = builder.AddNpmApp("frontend", "../NodeFrontend", "watch")
//     .WithReference(weatherapi)
//     .WaitFor(weatherapi)
//     .WithReference(cache)
//     .WaitFor(cache)
//     .WithHttpEndpoint(env: "PORT")
//     .WithExternalHttpEndpoints()
//     .PublishAsDockerFile();

var launchProfile = builder.Configuration["DOTNET_LAUNCH_PROFILE"];

if (builder.Environment.IsDevelopment() && launchProfile == "https")
{
    angular.RunWithHttpsDevCertificate("HTTPS_CERT_FILE", "HTTPS_CERT_KEY_FILE");
}

builder.Build().Run();
