using Core.Types.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CoreWeb.Api.Features.Flags;

public sealed class FeatureFlagSeeder : IHostedService
{
    private readonly IServiceProvider _services;

    public FeatureFlagSeeder(IServiceProvider services)
    {
        _services = services;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var flags = scope.ServiceProvider.GetRequiredService<IFeatureFlagService>();
        await flags.SetAsync("featureA.enabled", new FlagValue
        {
            Key = "featureA.enabled",
            Scope = FlagScope.Global.ToString(),
            Type = "boolean",
            Value = true
        }, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
