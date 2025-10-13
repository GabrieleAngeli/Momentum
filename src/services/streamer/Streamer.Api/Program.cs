using System;
using Dapr.Client;
using Google.Protobuf.WellKnownTypes;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Streamer.Application.Ingestion;
using Streamer.Infrastructure.Ingestion;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDaprClient();

builder.Services.AddGrpc();
builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection("Kafka"));
builder.Services.AddSingleton<ITelemetryRepository, TimescaleTelemetryRepository>();
builder.Services.AddSingleton<IEventNotifier, DaprEventNotifier>();
builder.Services.AddSingleton<IngestTelemetryEventHandler>();
builder.Services.AddHostedService<KafkaTelemetryConsumer>();
builder.Services.AddDaprClient();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("streamer-api"))
    .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation().AddPrometheusExporter())
    .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation());

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapGrpcService<StreamerGrpcService>();
app.MapGet("/", () => "Streamer service ready");
app.MapHealthChecks("/healthz");
app.MapGet("/metrics", () => Results.Ok("Prometheus metrics"));

app.Run();

public sealed class StreamerGrpcService : Streamer.Ingestion.V1.StreamIngestor.StreamIngestorBase
{
    private readonly IngestTelemetryEventHandler _handler;

    public StreamerGrpcService(IngestTelemetryEventHandler handler)
    {
        _handler = handler;
    }

    public override async Task<Streamer.Ingestion.V1.IngestReply> Ingest(Streamer.Ingestion.V1.IngestRequest request, Grpc.Core.ServerCallContext context)
    {
        await _handler.HandleAsync(new IngestTelemetryEventCommand(request.Source, request.Type, request.Timestamp.ToDateTimeOffset(), request.Value, request.Metadata), context.CancellationToken);
        return new Streamer.Ingestion.V1.IngestReply { Accepted = true };
    }
}

public static class TimestampExtensions
{
    public static DateTimeOffset ToDateTimeOffset(this Google.Protobuf.WellKnownTypes.Timestamp timestamp) => DateTimeOffset.FromUnixTimeSeconds(timestamp.Seconds).AddTicks(timestamp.Nanos / 100);
}

public partial class Program { }
