using System.Threading;
using ModularMonolith.Application.Modules;
using ModularMonolith.Domain.Modules;
using Moq;
using Xunit;

namespace ModularMonolith.Application.Tests;

public sealed class GetModuleStatusQueryTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsStatusesFromProvider()
    {
        var registration = new ModuleRegistration("identifier", "Authentication", "identifier-api", "healthz");
        var expected = new ModuleStatus(registration, true, "Healthy");

        var provider = new Mock<IModuleStatusProvider>();
        provider
            .Setup(p => p.GetStatusesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { expected });

        var query = new GetModuleStatusQuery(provider.Object);
        var result = await query.ExecuteAsync();

        Assert.Single(result);
        Assert.Equal(expected, result.Single());
    }
}
