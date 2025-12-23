using FluentAssertions;
using Identifier.Application.Caching;
using Identifier.Domain.Entities;
using Identifier.Infrastructure.Caching;
using Identifier.Infrastructure.Persistence;
using Identifier.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Identifier.Application.Tests;

public class FeatureFlagProviderTests
{
    [Fact]
    public async Task Uses_User_Override_When_Present()
    {
        await using var context = CreateContext();
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var flag = new FeatureFlag { Id = Guid.NewGuid(), Key = "ui.newDashboard", DefaultVariation = "off" };
        var org = new Organization { Id = orgId, Name = "Org" };
        var group = new Group { Id = groupId, OrganizationId = orgId, Name = "Team" };
        var user = new User { Id = userId, OrganizationId = orgId, Email = "user@test", Active = true };

        context.AddRange(flag, org, group, user);
        context.UserGroups.Add(new UserGroup { UserId = userId, GroupId = groupId });
        context.GroupFlags.Add(new GroupFlag { Id = Guid.NewGuid(), GroupId = groupId, FeatureFlagId = flag.Id, Variation = "off" });
        context.OrgFlags.Add(new OrgFlag { Id = Guid.NewGuid(), OrganizationId = orgId, FeatureFlagId = flag.Id, Variation = "on" });
        context.UserFlags.Add(new UserFlag { Id = Guid.NewGuid(), UserId = userId, FeatureFlagId = flag.Id, Variation = "on" });
        await context.SaveChangesAsync();

        var provider = CreateProvider(context);

        var variation = await provider.EvaluateAsync(flag.Id, orgId, userId, new[] { groupId }, CancellationToken.None);

        variation.Should().Be("on");
    }

    [Fact]
    public async Task Falls_Back_To_Default_When_No_Override()
    {
        await using var context = CreateContext();
        var flag = new FeatureFlag { Id = Guid.NewGuid(), Key = "devices.beta", DefaultVariation = "off" };
        context.FeatureFlags.Add(flag);
        await context.SaveChangesAsync();

        var provider = CreateProvider(context);

        var enabled = await provider.IsEnabledAsync(flag.Key, null, null, Array.Empty<Guid>(), CancellationToken.None);

        enabled.Should().BeFalse();
    }

    private static FeatureFlagProvider CreateProvider(IdentifierDbContext context)
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        IIdentifierCache cache = new MemoryIdentifierCache(memoryCache);
        return new FeatureFlagProvider(context, cache);
    }

    private static IdentifierDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<IdentifierDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new IdentifierDbContext(options);
    }
}
