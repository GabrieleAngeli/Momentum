using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using WebBackendCore.Application.Notifications;

namespace WebBackendCore.Infrastructure.Notifications;

public sealed class SignalRNotificationBroadcaster : INotificationBroadcaster
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<SignalRNotificationBroadcaster> _logger;

    public SignalRNotificationBroadcaster(IHubContext<NotificationHub> hubContext, ILogger<SignalRNotificationBroadcaster> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task BroadcastAsync(object payload, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Broadcasting payload {Payload}", payload);
        await _hubContext.Clients.All.SendAsync("telemetryNotification", payload, cancellationToken);
    }
}

public sealed class NotificationHub : Hub
{
}
