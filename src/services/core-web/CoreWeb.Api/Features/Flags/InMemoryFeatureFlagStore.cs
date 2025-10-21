using System.Collections.Concurrent;
using Core.Types.Dtos;
using System.Linq;

namespace CoreWeb.Api.Features.Flags;

public sealed class InMemoryFeatureFlagStore : IFeatureFlagStore
{
    private readonly ConcurrentDictionary<string, FlagValue> _flags = new(StringComparer.OrdinalIgnoreCase);

    public Task<FlagValue?> GetAsync(string key, FlagScope scope, string? scopeReference, CancellationToken cancellationToken)
    {
        var normalizedKey = BuildKey(key, scope, scopeReference);
        _flags.TryGetValue(normalizedKey, out var value);
        return Task.FromResult(value);
    }

    public Task<IDictionary<string, FlagValue>> ListAsync(FlagScope scope, string? scopeReference, CancellationToken cancellationToken)
    {
        var prefix = BuildKeyPrefix(scope, scopeReference);
        var values = _flags
            .Where(kv => kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(kv => kv.Key[(prefix.Length)..], kv => kv.Value, StringComparer.OrdinalIgnoreCase);
        return Task.FromResult<IDictionary<string, FlagValue>>(values);
    }

    public Task SetAsync(string key, FlagValue flag, CancellationToken cancellationToken)
    {
        var normalizedKey = BuildKey(key, Enum.Parse<FlagScope>(flag.Scope, true), flag.ScopeReference);
        _flags[normalizedKey] = flag with { Key = key };
        return Task.CompletedTask;
    }

    private static string BuildKey(string key, FlagScope scope, string? scopeReference)
        => $"{scope}:{scopeReference ?? "_"}:{key}";

    private static string BuildKeyPrefix(FlagScope scope, string? scopeReference)
        => $"{scope}:{scopeReference ?? "_"}:";
}
