using Microsoft.Extensions.Configuration;

namespace Identifier.Api.Seed;

public static class SeedExecution
{
    private const string SeedConfigKey = "Identifier:Seed:Enabled";
    private const string SeedEnvVariable = "IDENTIFIER__SEED_ENABLED";

    public static bool IsSeedEnabled(IConfiguration configuration)
    {
        if (configuration.GetValue<bool?>(SeedConfigKey) is { } configured)
        {
            return configured;
        }

        var envValue = Environment.GetEnvironmentVariable(SeedEnvVariable);
        return string.Equals(envValue, "true", StringComparison.OrdinalIgnoreCase);
    }
}
