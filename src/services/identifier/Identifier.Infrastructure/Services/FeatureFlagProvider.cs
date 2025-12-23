using Identifier.Application.Abstractions;
using Identifier.Application.Caching;
using Identifier.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identifier.Infrastructure.Services;

public class FeatureFlagProvider : IFeatureFlagProvider
{
    private readonly IdentifierDbContext _dbContext;
    private readonly IIdentifierCache _cache;

    public FeatureFlagProvider(IdentifierDbContext dbContext, IIdentifierCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<string> EvaluateAsync(Guid featureFlagId, Guid? organizationId, Guid? userId, IEnumerable<Guid> groupIds, CancellationToken cancellationToken = default)
    {
        var flag = await GetFlagAsync(featureFlagId, cancellationToken);
        if (flag is null)
        {
            return string.Empty;
        }

        var groupIdList = groupIds?.Where(id => id != Guid.Empty).Distinct().ToList() ?? new List<Guid>();

        if (userId.HasValue)
        {
            var userOverride = await _dbContext.UserFlags
                .AsNoTracking()
                .Where(f => f.FeatureFlagId == featureFlagId && f.UserId == userId)
                .Select(f => f.Variation)
                .FirstOrDefaultAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(userOverride))
            {
                return userOverride;
            }
        }

        if (groupIdList.Count > 0)
        {
            var groupOverride = await _dbContext.GroupFlags
                .AsNoTracking()
                .Where(f => f.FeatureFlagId == featureFlagId && groupIdList.Contains(f.GroupId))
                .OrderBy(f => f.Id)
                .Select(f => f.Variation)
                .FirstOrDefaultAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(groupOverride))
            {
                return groupOverride;
            }
        }

        if (organizationId.HasValue)
        {
            var orgOverride = await _dbContext.OrgFlags
                .AsNoTracking()
                .Where(f => f.FeatureFlagId == featureFlagId && f.OrganizationId == organizationId)
                .Select(f => f.Variation)
                .FirstOrDefaultAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(orgOverride))
            {
                return orgOverride;
            }
        }

        return flag.DefaultVariation;
    }

    public async Task<bool> IsEnabledAsync(string flagKey, Guid? organizationId, Guid? userId, IEnumerable<Guid> groupIds, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(flagKey))
        {
            return true;
        }

        var flag = await GetFlagByKeyAsync(flagKey, cancellationToken);
        if (flag is null)
        {
            return false;
        }

        var variation = await EvaluateAsync(flag.Id, organizationId, userId, groupIds, cancellationToken);
        return IsVariationEnabled(variation);
    }

    private async Task<FlagSnapshot?> GetFlagAsync(Guid flagId, CancellationToken cancellationToken)
        => await _cache.GetOrCreateAsync($"identifier:flag:{flagId}", async () =>
        {
            return await _dbContext.FeatureFlags
                .AsNoTracking()
                .Where(f => f.Id == flagId)
                .Select(f => new FlagSnapshot(f.Id, f.Key, f.DefaultVariation))
                .FirstOrDefaultAsync(cancellationToken);
        });

    private async Task<FlagSnapshot?> GetFlagByKeyAsync(string flagKey, CancellationToken cancellationToken)
        => await _cache.GetOrCreateAsync($"identifier:flag-key:{flagKey.ToLowerInvariant()}", async () =>
        {
            return await _dbContext.FeatureFlags
                .AsNoTracking()
                .Where(f => f.Key == flagKey)
                .Select(f => new FlagSnapshot(f.Id, f.Key, f.DefaultVariation))
                .FirstOrDefaultAsync(cancellationToken);
        });

    private static bool IsVariationEnabled(string? variation)
    {
        if (string.IsNullOrWhiteSpace(variation))
        {
            return false;
        }

        return variation.Trim().ToLowerInvariant() switch
        {
            "on" or "true" or "1" or "enabled" or "yes" => true,
            _ => false
        };
    }

    private sealed record FlagSnapshot(Guid Id, string Key, string DefaultVariation);
}
