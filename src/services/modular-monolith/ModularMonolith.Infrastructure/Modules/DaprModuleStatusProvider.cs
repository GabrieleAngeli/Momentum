using System.Net.Http;
using Dapr.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModularMonolith.Application.Modules;
using ModularMonolith.Infrastructure.Options;

namespace ModularMonolith.Infrastructure.Modules;

public sealed class DaprModuleStatusProvider : IModuleStatusProvider
{
    private readonly DaprClient _daprClient;
    private readonly MonolithOptions _options;
    private readonly ILogger<DaprModuleStatusProvider> _logger;

    public DaprModuleStatusProvider(
        DaprClient daprClient,
        IOptions<MonolithOptions> options,
        ILogger<DaprModuleStatusProvider> logger)
    {
        _daprClient = daprClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<ModuleStatus>> GetStatusesAsync(CancellationToken cancellationToken)
    {
        var registrations = _options.ToRegistrations();
        var statuses = new List<ModuleStatus>(registrations.Count);

        foreach (var registration in registrations)
        {
            var healthy = false;
            var details = "Module unreachable";

            try
            {
                await _daprClient.InvokeMethodAsync(HttpMethod.Get, registration.AppId, registration.HealthEndpoint, cancellationToken);
                healthy = true;
                details = "Healthy";
            }
            catch (InvocationException ex)
            {
                _logger.LogWarning(ex, "Failed to reach module {ModuleName} via Dapr", registration.Name);
                details = ex.InnerException?.Message ?? ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while invoking module {ModuleName}", registration.Name);
                details = ex.Message;
            }

            statuses.Add(new ModuleStatus(registration, healthy, details));
        }

        return statuses;
    }
}
