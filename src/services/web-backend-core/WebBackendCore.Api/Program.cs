using System.Collections.Generic;
using OpenFeature;
using OpenFeature.Contrib.Providers.Memory;
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
builder.Services.AddOpenFeature();
builder.Services.AddSingleton<IFeatureProvider>(_ => new InMemoryProvider(new Dictionary<string, object>
{
    ["feature:alerting"] = true
}));

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

public partial class Program { }
