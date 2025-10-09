using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Streamer.Application.Ingestion;

namespace Streamer.Infrastructure.Ingestion;

public sealed class KafkaTelemetryConsumer : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KafkaTelemetryConsumer> _logger;

    public KafkaTelemetryConsumer(IOptions<KafkaOptions> options, IServiceProvider serviceProvider, ILogger<KafkaTelemetryConsumer> logger)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            GroupId = options.Value.ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };
        _consumer = new ConsumerBuilder<string, string>(config).Build();
        _consumer.Subscribe(options.Value.Topic);
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = _consumer.Consume(stoppingToken);
                if (result is null)
                {
                    continue;
                }

                using var scope = _serviceProvider.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<IngestTelemetryEventHandler>();
                var payload = System.Text.Json.JsonDocument.Parse(result.Message.Value);
                var root = payload.RootElement;
                var metadata = root.GetProperty("metadata").EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString() ?? string.Empty);
                var command = new IngestTelemetryEventCommand(
                    root.GetProperty("source").GetString() ?? "unknown",
                    root.GetProperty("type").GetString() ?? "unknown",
                    root.GetProperty("timestamp").GetDateTimeOffset(),
                    root.GetProperty("value").GetDouble(),
                    metadata);
                await handler.HandleAsync(command, stoppingToken);
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Kafka consume error");
            }
        }
    }

    public override void Dispose()
    {
        _consumer.Close();
        _consumer.Dispose();
        base.Dispose();
    }
}

public sealed class KafkaOptions
{
    public string BootstrapServers { get; set; } = "kafka:9092";
    public string Topic { get; set; } = "telemetry.input";
    public string ConsumerGroup { get; set; } = "streamer";
}
