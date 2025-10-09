using System;
using System.Collections.Generic;

namespace Streamer.Domain.Entities;

public sealed class TelemetryEvent
{
    public Guid Id { get; }
    public string Source { get; }
    public string Type { get; }
    public DateTimeOffset Timestamp { get; }
    public double Value { get; }
    public IReadOnlyDictionary<string, string> Metadata => _metadata;

    private readonly Dictionary<string, string> _metadata;

    public TelemetryEvent(Guid id, string source, string type, DateTimeOffset timestamp, double value, IDictionary<string, string>? metadata = null)
    {
        Id = id;
        Source = source;
        Type = type;
        Timestamp = timestamp;
        Value = value;
        _metadata = metadata is null ? new Dictionary<string, string>() : new Dictionary<string, string>(metadata);
    }
}
