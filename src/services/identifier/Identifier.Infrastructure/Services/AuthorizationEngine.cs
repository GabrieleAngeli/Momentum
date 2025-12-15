using Identifier.Application;
using Identifier.Application.Abstractions;
using Identifier.Domain.Entities;
using Identifier.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Identifier.Infrastructure.Services;

public class AuthorizationEngine : IAuthorizationEngine
{
    private readonly IdentifierDbContext _dbContext;
    private readonly ILicenseService _licenseService;
    private readonly IFeatureFlagProvider _featureFlagProvider;
    private readonly IdentifierAuthorizationOptions _options;

    public AuthorizationEngine(
        IdentifierDbContext dbContext,
        ILicenseService licenseService,
        IFeatureFlagProvider featureFlagProvider,
        IOptions<IdentifierAuthorizationOptions> options)
    {
        _dbContext = dbContext;
        _licenseService = licenseService;
        _featureFlagProvider = featureFlagProvider;
        _options = options.Value;
    }

    public async Task<AuthorizationDecision> AuthorizeAsync(Guid userId, string resource, string action, CancellationToken cancellationToken = default)
    {
        var userProjection = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.Id,
                u.Active,
                u.OrganizationId,

                GroupIds = u.UserGroups
                    .Select(ug => ug.GroupId)
                    .ToList(),

                Permissions = u.UserGroups
                    .Where(ug => ug.Group != null)
                    .SelectMany(ug => ug.Group!.GroupRoles)
                    .Where(gr => gr.Role != null)
                    .SelectMany(gr => gr.Role!.RolePermissions)
                    .Where(rp => rp.Permission != null)
                    .Select(rp => new
                    {
                        rp.Permission!.Code,
                        rp.Permission!.Resource,
                        rp.Permission!.Action
                    })
                    .Distinct()
                    .Select(p => new PermissionSummary(p.Code, p.Resource, p.Action))
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (userProjection is null)
        {
            return AuthorizationDecision.Denied("user-not-found");
        }

        if (!userProjection.Active)
        {
            return AuthorizationDecision.Denied("user-inactive");
        }

        var orgId = userProjection.OrganizationId;
        var compositeKey = GetCompositeKey(resource, action);

        if (_options.FeatureMap.TryGetValue(compositeKey, out var featureKey))
        {
            var licenseEval = await _licenseService.EvaluateAsync(orgId, featureKey, cancellationToken);
            if (!licenseEval.HasLicense)
            {
                return AuthorizationDecision.Denied("license-not-found");
            }

            if (!licenseEval.FeatureIncluded)
            {
                return AuthorizationDecision.Denied("feature-not-included");
            }

            if (!licenseEval.WithinQuota)
            {
                return AuthorizationDecision.Denied("license-quota-exceeded");
            }
        }

        if (_options.FlagMap.TryGetValue(compositeKey, out var flagKey))
        {
            var enabled = await _featureFlagProvider.IsEnabledAsync(flagKey, orgId, userId, userProjection.GroupIds, cancellationToken);
            if (!enabled)
            {
                return AuthorizationDecision.Denied("feature-flag-disabled");
            }
        }

        var hasPermission = userProjection.Permissions.Any(p =>
            string.Equals(p.Code, compositeKey, StringComparison.OrdinalIgnoreCase) ||
            (string.Equals(p.Resource, resource, StringComparison.OrdinalIgnoreCase) &&
             string.Equals(p.Action, action, StringComparison.OrdinalIgnoreCase)));

        return hasPermission
            ? AuthorizationDecision.Success()
            : AuthorizationDecision.Denied("permission-denied");
    }

    private static string GetCompositeKey(string resource, string action)
        => $"{resource}:{action}".ToLowerInvariant();

    private record PermissionSummary(string Code, string Resource, string Action);
}
