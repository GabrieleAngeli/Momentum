using Microsoft.AspNetCore.SignalR;
using Notifier.Application.Dispatching;
using Notifier.Infrastructure.Channels;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDaprClient();
builder.Services.AddSingleton<INotificationChannel, EmailNotificationChannel>();
builder.Services.AddSingleton<INotificationChannel, SignalRNotificationChannel>();
builder.Services.AddSingleton<DispatchNotificationHandler>();

builder.Services.AddSignalR();
builder.Services.AddControllers();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("notifier-api"))
    .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation().AddPrometheusExporter())
    .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation());

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapControllers();
app.MapHub<NotificationsHub>("/hub/notifications");
app.MapSubscribeHandler();
app.MapGet("/", () => "Notifier service ready");
app.MapHealthChecks("/healthz");
app.MapGet("/metrics", () => Results.Ok("Prometheus metrics"));

app.Run();

public sealed class NotificationsHub : Microsoft.AspNetCore.SignalR.Hub
{
    private readonly ILogger<NotificationsHub> _logger;

    public NotificationsHub(ILogger<NotificationsHub> logger)
    {
        _logger = logger;
    }

    public async Task BroadcastNotification(object payload)
    {
        _logger.LogInformation("Broadcasting notification {Payload}", payload);
        await Clients.All.SendAsync("telemetryNotification", payload);
    }
}

public partial class Program { }
