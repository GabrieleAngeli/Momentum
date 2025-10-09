using Aspire.Hosting;
using Aspire.Hosting.Dapr;
using Aspire.Hosting.Kafka;
using Aspire.Hosting.PostgreSQL;

var builder = DistributedApplication.CreateBuilder(args);

var kafka = builder.AddKafka("kafka")
    .WithBrokerSettings(b => b.WithNumPartitions(3))
    .WithTopic("telemetry.input")
    .WithTopic("telemetry.ingested");

var timescale = builder.AddPostgres("timescale", password: "postgres")
    .WithDataVolume()
    .WithEnvironment("POSTGRES_DB", "momentum")
    .WithImage("timescale/timescaledb:2.14.0-pg16");

var ignite = builder.AddContainer("ignite", "apacheignite/ignite", "2.16.0")
    .WithEndpoint(11211, name: "client");

var grafana = builder.AddContainer("grafana", "grafana/grafana", "10.4.0")
    .WithEndpoint(3000, 3000, "http");

var prometheus = builder.AddContainer("prometheus", "prom/prometheus", "v2.49.0")
    .WithEndpoint(9090, 9090, "http")
    .WithBindMount("./observability/prometheus.yml", "/etc/prometheus/prometheus.yml");

var loki = builder.AddContainer("loki", "grafana/loki", "3.0.0")
    .WithEndpoint(3100, 3100, "http");

var tempo = builder.AddContainer("tempo", "grafana/tempo", "2.4.0")
    .WithEndpoint(3200, 3200, "http");

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

var angular = builder.AddNpmApp("web-core", "src/web-core")
    .WithEnvironment("NG_FORCE_TTY", "0")
    .WithHttpEndpoint(port: 4200, targetPort: 4200);

builder.Build().Run();
