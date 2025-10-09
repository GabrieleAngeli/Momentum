using Dapr.Client;
using Streamer.Application.Ingestion;
using Streamer.Domain.Entities;

namespace Streamer.Infrastructure.Ingestion;

public sealed class DaprEventNotifier : IEventNotifier
{
    private readonly DaprClient _daprClient;
    private readonly ILogger<DaprEventNotifier> _logger;

    public DaprEventNotifier(DaprClient daprClient, ILogger<DaprEventNotifier> logger)
    {
        _daprClient = daprClient;
        _logger = logger;
    }

    public async Task NotifyAsync(TelemetryEvent telemetryEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Publishing telemetry event {Id} to notifier", telemetryEvent.Id);
        await _daprClient.PublishEventAsync("kafka", "telemetry.ingested", new
        {
            telemetryEvent.Id,
            telemetryEvent.Source,
            telemetryEvent.Type,
            telemetryEvent.Timestamp,
            telemetryEvent.Value,
            telemetryEvent.Metadata
        }, cancellationToken);
    }
}
