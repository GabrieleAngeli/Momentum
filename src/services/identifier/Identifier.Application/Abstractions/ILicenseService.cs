namespace Identifier.Application.Abstractions;

public interface ILicenseService
{
    Task<bool> HasFeatureAsync(Guid organizationId, string featureKey, CancellationToken cancellationToken = default);
    Task<LicenseEvaluation> EvaluateAsync(Guid organizationId, string featureKey, CancellationToken cancellationToken = default);
}

public record LicenseEvaluation(bool HasLicense, bool FeatureIncluded, bool WithinQuota, string? Reason, int? RemainingQuota);
