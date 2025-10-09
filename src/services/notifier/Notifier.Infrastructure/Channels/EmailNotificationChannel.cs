using Azure.Communication.Email;
using Notifier.Application.Dispatching;
using Notifier.Domain.Entities;

namespace Notifier.Infrastructure.Channels;

public sealed class EmailNotificationChannel : INotificationChannel
{
    private readonly EmailClient _client;
    private readonly ILogger<EmailNotificationChannel> _logger;

    public EmailNotificationChannel(IConfiguration configuration, ILogger<EmailNotificationChannel> logger)
    {
        var connectionString = configuration.GetValue<string>("Notifications:Email:ConnectionString") ?? "endpoint=https://local";
        _client = new EmailClient(connectionString);
        _logger = logger;
    }

    public string Name => "email";

    public async Task DispatchAsync(Notification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sending email notification for event {EventId}", notification.EventId);
        await _client.SendAsync(
            WaitUntil.Started,
            senderAddress: "noreply@momentum.dev",
            recipientAddress: notification.Recipient,
            subject: $"Telemetry alert from {notification.Channel}",
            htmlContent: notification.Message,
            cancellationToken: cancellationToken);
    }
}
