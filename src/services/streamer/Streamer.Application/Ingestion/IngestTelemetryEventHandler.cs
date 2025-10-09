using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Streamer.Domain.Entities;

namespace Streamer.Application.Ingestion;

public sealed record IngestTelemetryEventCommand(string Source, string Type, DateTimeOffset Timestamp, double Value, IDictionary<string, string> Metadata);

public interface ITelemetryRepository
{
    Task PersistAsync(TelemetryEvent telemetryEvent, CancellationToken cancellationToken);
}

public interface IEventNotifier
{
    Task NotifyAsync(TelemetryEvent telemetryEvent, CancellationToken cancellationToken);
}

public sealed class IngestTelemetryEventHandler
{
    private readonly ITelemetryRepository _repository;
    private readonly IEventNotifier _notifier;

    public IngestTelemetryEventHandler(ITelemetryRepository repository, IEventNotifier notifier)
    {
        _repository = repository;
        _notifier = notifier;
    }

    public async Task HandleAsync(IngestTelemetryEventCommand command, CancellationToken cancellationToken)
    {
        var telemetryEvent = new TelemetryEvent(Guid.NewGuid(), command.Source, command.Type, command.Timestamp, command.Value, command.Metadata);
        await _repository.PersistAsync(telemetryEvent, cancellationToken);
        await _notifier.NotifyAsync(telemetryEvent, cancellationToken);
    }
}
