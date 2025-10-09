namespace WebBackendCore.Domain.Aggregates;

public sealed class NotificationStream
{
    private readonly List<object> _events = new();

    public IReadOnlyCollection<object> Events => _events.AsReadOnly();

    public void Append(object @event) => _events.Add(@event);
}
