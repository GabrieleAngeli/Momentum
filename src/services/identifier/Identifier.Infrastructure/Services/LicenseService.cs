using Identifier.Application.Abstractions;
using Identifier.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identifier.Infrastructure.Services;

public class LicenseService : ILicenseService
{
    private readonly IdentifierDbContext _dbContext;

    public LicenseService(IdentifierDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> HasFeatureAsync(Guid organizationId, string featureKey, CancellationToken cancellationToken = default)
    {
        var evaluation = await EvaluateAsync(organizationId, featureKey, cancellationToken);
        return evaluation.HasLicense && evaluation.FeatureIncluded && evaluation.WithinQuota;
    }

    public async Task<LicenseEvaluation> EvaluateAsync(Guid organizationId, string featureKey, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var licenses = await _dbContext.Licenses
            .AsNoTracking()
            .Where(l => l.OrganizationId == organizationId && l.ValidFrom <= now && l.ValidTo >= now)
            .Select(l => new
            {
                LicenseId = l.Id,
                Entitlements = l.Entitlements.Select(e => new { e.Quota, e.ConstraintsJson, FeatureKey = e.Feature.Key }).ToList(),
                Modules = l.Modules.Select(lm => lm.Module).Select(m => new
                {
                    ModuleKey = m.Key,
                    FeatureKeys = m.Features.Select(mf => mf.Feature.Key).ToList()
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        if (licenses.Count == 0)
        {
            return new LicenseEvaluation(false, false, false, "no-active-license", null);
        }

        var featureIncluded = false;
        int? quota = null;

        foreach (var license in licenses)
        {
            var entitlement = license.Entitlements.FirstOrDefault(e => string.Equals(e.FeatureKey, featureKey, StringComparison.OrdinalIgnoreCase));
            if (entitlement is not null)
            {
                featureIncluded = true;
                quota = entitlement.Quota;
                break;
            }

            if (license.Modules.Any(m => m.FeatureKeys.Any(fk => string.Equals(fk, featureKey, StringComparison.OrdinalIgnoreCase))))
            {
                featureIncluded = true;
                break;
            }
        }

        if (!featureIncluded)
        {
            return new LicenseEvaluation(true, false, false, "feature-not-in-license", null);
        }

        var withinQuota = !quota.HasValue || quota.Value > 0;
        return new LicenseEvaluation(true, true, withinQuota, withinQuota ? null : "quota-exhausted", quota);
    }
}
