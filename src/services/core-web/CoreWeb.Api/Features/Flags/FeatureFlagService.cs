using System.Text.Json;
using System.Linq;
using Core.Types.Dtos;

namespace CoreWeb.Api.Features.Flags;

public sealed class FeatureFlagService : IFeatureFlagService
{
    private readonly IFeatureFlagStore _store;
    private readonly IFlagChangeNotifier _notifier;

    public FeatureFlagService(IFeatureFlagStore store, IFlagChangeNotifier notifier)
    {
        _store = store;
        _notifier = notifier;
    }

    public async Task<IDictionary<string, FlagValue>> GetSnapshotAsync(EvaluationContext ctx, CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, FlagValue>(StringComparer.OrdinalIgnoreCase);
        var precedence = await ResolvePrecedenceAsync(ctx, cancellationToken);
        foreach (var flag in precedence)
        {
            result[flag.Key] = flag.Value;
        }
        return result;
    }

    public async Task<bool> GetBooleanAsync(string key, EvaluationContext ctx, bool @default, CancellationToken cancellationToken)
    {
        var value = await ResolveFlagAsync(key, ctx, cancellationToken);
        if (value is null)
        {
            return @default;
        }

        return value.Value switch
        {
            bool b => b,
            string s when bool.TryParse(s, out var parsed) => parsed,
            JsonElement json when json.ValueKind == JsonValueKind.True || json.ValueKind == JsonValueKind.False => json.GetBoolean(),
            _ => @default
        };
    }

    public async Task<string?> GetStringAsync(string key, EvaluationContext ctx, string? @default, CancellationToken cancellationToken)
    {
        var value = await ResolveFlagAsync(key, ctx, cancellationToken);
        return value?.Value?.ToString() ?? @default;
    }

    public async Task<double> GetNumberAsync(string key, EvaluationContext ctx, double @default, CancellationToken cancellationToken)
    {
        var value = await ResolveFlagAsync(key, ctx, cancellationToken);
        if (value is null)
        {
            return @default;
        }

        return value.Value switch
        {
            int i => i,
            long l => l,
            double d => d,
            JsonElement json when json.TryGetDouble(out var parsed) => parsed,
            string s when double.TryParse(s, out var parsed) => parsed,
            _ => @default
        };
    }

    public async Task<T?> GetObjectAsync<T>(string key, EvaluationContext ctx, T? @default, CancellationToken cancellationToken)
    {
        var value = await ResolveFlagAsync(key, ctx, cancellationToken);
        if (value is null)
        {
            return @default;
        }

        if (value.Value is JsonElement json && json.ValueKind != JsonValueKind.Null)
        {
            return json.Deserialize<T>();
        }

        if (value.Value is T typed)
        {
            return typed;
        }

        return @default;
    }

    public async Task SetAsync(string key, FlagValue value, CancellationToken cancellationToken)
    {
        await _store.SetAsync(key, value, cancellationToken);
        await _notifier.NotifyAsync(new FlagsDelta
        {
            Updated = new Dictionary<string, FlagValue>
            {
                [key] = value
            }
        }, cancellationToken);
    }

    private async Task<FlagValue?> ResolveFlagAsync(string key, EvaluationContext ctx, CancellationToken cancellationToken)
    {
        var precedence = await ResolvePrecedenceAsync(ctx, cancellationToken);
        foreach (var flag in precedence)
        {
            if (flag.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                return flag.Value;
            }
        }
        return null;
    }

    private async Task<IEnumerable<KeyValuePair<string, FlagValue>>> ResolvePrecedenceAsync(EvaluationContext ctx, CancellationToken cancellationToken)
    {
        var values = new Dictionary<string, FlagValue>(StringComparer.OrdinalIgnoreCase);

        var globalFlags = await _store.ListAsync(FlagScope.Global, null, cancellationToken);
        Merge(values, globalFlags);

        if (!string.IsNullOrWhiteSpace(ctx.TenantId))
        {
            var tenantFlags = await _store.ListAsync(FlagScope.Tenant, ctx.TenantId, cancellationToken);
            Merge(values, tenantFlags);
        }

        foreach (var role in ctx.Roles)
        {
            var roleFlags = await _store.ListAsync(FlagScope.Role, role, cancellationToken);
            Merge(values, roleFlags);
        }

        if (!string.IsNullOrWhiteSpace(ctx.UserId))
        {
            var userFlags = await _store.ListAsync(FlagScope.User, ctx.UserId, cancellationToken);
            Merge(values, userFlags);
        }

        return values.OrderBy(kv => kv.Key);

        static void Merge(IDictionary<string, FlagValue> destination, IDictionary<string, FlagValue> source)
        {
            foreach (var pair in source)
            {
                destination[pair.Key] = pair.Value;
            }
        }
    }
}
