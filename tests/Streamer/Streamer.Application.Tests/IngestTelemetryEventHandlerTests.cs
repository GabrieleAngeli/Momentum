using Moq;
using Streamer.Application.Ingestion;
using Streamer.Domain.Entities;
using Xunit;

namespace Streamer.Application.Tests;

public class IngestTelemetryEventHandlerTests
{
    [Fact]
    public async Task HandleAsync_PersistsAndNotifies()
    {
        var repository = new Mock<ITelemetryRepository>();
        var notifier = new Mock<IEventNotifier>();
        var handler = new IngestTelemetryEventHandler(repository.Object, notifier.Object);
        var command = new IngestTelemetryEventCommand("sensor", "temperature", DateTimeOffset.UtcNow, 42.0, new Dictionary<string, string>());

        await handler.HandleAsync(command, CancellationToken.None);

        repository.Verify(r => r.PersistAsync(It.IsAny<TelemetryEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        notifier.Verify(n => n.NotifyAsync(It.IsAny<TelemetryEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
