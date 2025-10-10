using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Notifier.Application.Dispatching;
using Notifier.Domain.Entities;

namespace Notifier.Infrastructure.Channels;

public sealed class SignalRNotificationChannel : INotificationChannel, IAsyncDisposable
{
    private readonly HubConnection _connection;
    private readonly ILogger<SignalRNotificationChannel> _logger;

    public SignalRNotificationChannel(IConfiguration configuration, ILogger<SignalRNotificationChannel> logger)
    {
        var hubUrl = configuration.GetValue<string>("Notifications:SignalR:HubUrl") ?? "http://web-backend-core:8080/notifications";
        _connection = new HubConnectionBuilder().WithUrl(hubUrl).WithAutomaticReconnect().Build();
        _logger = logger;
    }

    public string Name => "signalr";

    public async Task DispatchAsync(Notification notification, CancellationToken cancellationToken)
    {
        if (_connection.State != HubConnectionState.Connected)
        {
            await _connection.StartAsync(cancellationToken);
        }

        _logger.LogInformation("Sending SignalR notification for event {EventId}", notification.EventId);
        await _connection.InvokeAsync("BroadcastNotification", new
        {
            notification.EventId,
            notification.Channel,
            notification.Message,
            notification.CreatedAt
        }, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}
