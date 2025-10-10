using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Notifier.Application.Dispatching;
using Xunit;

namespace Notifier.Api.Integration.Tests;

public class NotificationsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public NotificationsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(_ => { });
    }

    [Fact]
    public async Task Publish_ReturnsAccepted()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/notifications", new DispatchNotificationCommand(Guid.NewGuid(), "signalr", "broadcast", "demo", DateTimeOffset.UtcNow));
        Assert.Equal(System.Net.HttpStatusCode.Accepted, response.StatusCode);
    }
}
