namespace Notifier.Domain.Entities;

public sealed class Notification
{
    public Guid EventId { get; }
    public string Channel { get; }
    public string Recipient { get; }
    public string Message { get; }
    public DateTimeOffset CreatedAt { get; }

    public Notification(Guid eventId, string channel, string recipient, string message, DateTimeOffset createdAt)
    {
        EventId = eventId;
        Channel = channel;
        Recipient = recipient;
        Message = message;
        CreatedAt = createdAt;
    }
}
