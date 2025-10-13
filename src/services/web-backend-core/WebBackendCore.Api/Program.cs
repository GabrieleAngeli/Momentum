using System.Collections.Generic;
using System.Text.Json.Serialization;
using OpenFeature;
using OpenFeature.DependencyInjection.Providers.Memory;
using OpenFeature.Hooks;
using OpenFeature.Model;
using OpenFeature.Providers.Memory;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using WebBackendCore.Application.Notifications;
using WebBackendCore.Domain.Aggregates;
using WebBackendCore.Infrastructure.Notifications;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddDaprClient();
builder.Services.AddOpenFeature(featureBuilder =>
{
    var metricsHookOptions = MetricsHookOptions.CreateBuilder()
        .WithCustomDimension("custom_dimension_key", "custom_dimension_value")
        .WithFlagEvaluationMetadata("boolean", s => s.GetBool("boolean"))
        .Build();

    featureBuilder
        //.AddHook(sp => new LoggingHook(sp.GetRequiredService<ILogger<LoggingHook>>()))
        //.AddHook(_ => new MetricsHook(metricsHookOptions))
        //.AddHook<TraceEnricherHook>()
        .AddInMemoryProvider("InMemory", _ => new Dictionary<string, Flag>()
        {
            {
                "welcome-message", new Flag<bool>(
                    new Dictionary<string, bool> { { "show", true }, { "hide", false } }, "show")
            },
            {
                "test-config", new Flag<Value>(new Dictionary<string, Value>()
                {
                    { "enable", new Value(Structure.Builder().Set(nameof(TestConfig.Threshold), 100).Build()) },
                    { "half", new Value(Structure.Builder().Set(nameof(TestConfig.Threshold), 50).Build()) },
                    { "disable", new Value(Structure.Builder().Set(nameof(TestConfig.Threshold), 0).Build()) }
                }, "disable")
            }
        });
});

builder.Services.AddSingleton<NotificationStream>();
builder.Services.AddSingleton<INotificationBroadcaster, SignalRNotificationBroadcaster>();
builder.Services.AddSingleton<NotificationOrchestrator>();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("web-backend-core-api"))
    .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation().AddPrometheusExporter())
    .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation());

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapControllers();
app.MapHub<NotificationHub>("/notifications");
app.MapHealthChecks("/healthz");
app.MapGet("/", () => "Web backend ready");
app.MapGet("/metrics", () => Results.Ok("Prometheus metrics"));

app.Run();

public class TestConfig
{
    public int Threshold { get; set; } = 10;
}

[JsonSerializable(typeof(TestConfig))]
[JsonSerializable(typeof(Value))]
public partial class AppJsonSerializerContext : JsonSerializerContext;

public partial class Program { }
