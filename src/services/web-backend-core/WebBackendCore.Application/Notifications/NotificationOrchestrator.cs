using WebBackendCore.Domain.Aggregates;

namespace WebBackendCore.Application.Notifications;

public interface INotificationBroadcaster
{
    Task BroadcastAsync(object payload, CancellationToken cancellationToken);
}

public sealed class NotificationOrchestrator
{
    private readonly NotificationStream _stream;
    private readonly INotificationBroadcaster _broadcaster;

    public NotificationOrchestrator(NotificationStream stream, INotificationBroadcaster broadcaster)
    {
        _stream = stream;
        _broadcaster = broadcaster;
    }

    public async Task HandleAsync(object payload, CancellationToken cancellationToken)
    {
        _stream.Append(payload);
        await _broadcaster.BroadcastAsync(payload, cancellationToken);
    }
}
