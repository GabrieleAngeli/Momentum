using Notifier.Domain.Entities;

namespace Notifier.Application.Dispatching;

public sealed record DispatchNotificationCommand(Guid EventId, string Channel, string Recipient, string Message, DateTimeOffset CreatedAt);

public interface INotificationChannel
{
    string Name { get; }
    Task DispatchAsync(Notification notification, CancellationToken cancellationToken);
}

public sealed class DispatchNotificationHandler
{
    private readonly IEnumerable<INotificationChannel> _channels;

    public DispatchNotificationHandler(IEnumerable<INotificationChannel> channels)
    {
        _channels = channels;
    }

    public async Task HandleAsync(DispatchNotificationCommand command, CancellationToken cancellationToken)
    {
        var notification = new Notification(command.EventId, command.Channel, command.Recipient, command.Message, command.CreatedAt);
        var channel = _channels.FirstOrDefault(ch => string.Equals(ch.Name, command.Channel, StringComparison.OrdinalIgnoreCase));
        if (channel is null)
        {
            throw new InvalidOperationException($"Channel {command.Channel} not configured");
        }

        await channel.DispatchAsync(notification, cancellationToken);
    }
}
