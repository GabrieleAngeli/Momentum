using Dapr;
using Microsoft.AspNetCore.Mvc;
using Notifier.Application.Dispatching;

namespace Notifier.Api.Controllers;

[ApiController]
[Route("dapr/subscribe")] // Dapr discovery endpoint
public sealed class SubscriptionsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetSubscriptions() => Ok(new[]
    {
        new { pubsubname = "kafka", topic = "telemetry.ingested", route = "events/telemetry-ingested" }
    });
}

[ApiController]
[Route("events")] 
public sealed class EventsController : ControllerBase
{
    private readonly DispatchNotificationHandler _handler;

    public EventsController(DispatchNotificationHandler handler)
    {
        _handler = handler;
    }

    [HttpPost("telemetry-ingested")]
    [Topic("kafka", "telemetry.ingested")]
    public async Task<IActionResult> HandleTelemetry([FromBody] TelemetryNotification payload, CancellationToken cancellationToken)
    {
        var command = new DispatchNotificationCommand(payload.Id, "signalr", "broadcast", payload.Message ?? $"{payload.Type}={payload.Value}", DateTimeOffset.UtcNow);
        await _handler.HandleAsync(command, cancellationToken);
        return Ok();
    }
}

public sealed record TelemetryNotification(Guid Id, string Source, string Type, double Value, string? Message);
