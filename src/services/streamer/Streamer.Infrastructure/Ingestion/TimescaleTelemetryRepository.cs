using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Streamer.Application.Ingestion;
using Streamer.Domain.Entities;

namespace Streamer.Infrastructure.Ingestion;

public sealed class TimescaleTelemetryRepository : ITelemetryRepository
{
    private readonly string _connectionString;

    public TimescaleTelemetryRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Timescale") ?? throw new InvalidOperationException("Timescale connection string missing");
    }

    public async Task PersistAsync(TelemetryEvent telemetryEvent, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var command = new NpgsqlCommand("INSERT INTO telemetry_events(id, source, type, ts, value, metadata) VALUES (@id, @source, @type, @ts, @value, @metadata)", connection);
        command.Parameters.AddWithValue("@id", telemetryEvent.Id);
        command.Parameters.AddWithValue("@source", telemetryEvent.Source);
        command.Parameters.AddWithValue("@type", telemetryEvent.Type);
        command.Parameters.AddWithValue("@ts", telemetryEvent.Timestamp);
        command.Parameters.AddWithValue("@value", telemetryEvent.Value);
        command.Parameters.AddWithValue("@metadata", System.Text.Json.JsonSerializer.Serialize(telemetryEvent.Metadata));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
