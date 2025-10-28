namespace Identifier.Application.Abstractions;

public interface IFeatureFlagProvider
{
    Task<string> EvaluateAsync(Guid featureFlagId, Guid? organizationId, Guid? userId, IEnumerable<Guid> groupIds, CancellationToken cancellationToken = default);
    Task<bool> IsEnabledAsync(string flagKey, Guid? organizationId, Guid? userId, IEnumerable<Guid> groupIds, CancellationToken cancellationToken = default);
}
