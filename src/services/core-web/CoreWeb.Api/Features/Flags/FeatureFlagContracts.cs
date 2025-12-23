using Core.Types.Dtos;

namespace CoreWeb.Api.Features.Flags;

public enum FlagScope
{
    Global,
    Tenant,
    Role,
    User
}

public interface IFeatureFlagStore
{
    Task<FlagValue?> GetAsync(string key, FlagScope scope, string? scopeReference, CancellationToken cancellationToken);
    Task SetAsync(string key, FlagValue flag, CancellationToken cancellationToken);
    Task<IDictionary<string, FlagValue>> ListAsync(FlagScope scope, string? scopeReference, CancellationToken cancellationToken);
}

public interface IFlagChangeNotifier
{
    Task NotifyAsync(FlagsDelta delta, CancellationToken cancellationToken = default);
}

public interface IFeatureFlagService
{
    Task<bool> GetBooleanAsync(string key, EvaluationContext ctx, bool @default, CancellationToken cancellationToken);
    Task<string?> GetStringAsync(string key, EvaluationContext ctx, string? @default, CancellationToken cancellationToken);
    Task<double> GetNumberAsync(string key, EvaluationContext ctx, double @default, CancellationToken cancellationToken);
    Task<T?> GetObjectAsync<T>(string key, EvaluationContext ctx, T? @default, CancellationToken cancellationToken);
    Task<IDictionary<string, FlagValue>> GetSnapshotAsync(EvaluationContext ctx, CancellationToken cancellationToken);
    Task SetAsync(string key, FlagValue value, CancellationToken cancellationToken);
}
