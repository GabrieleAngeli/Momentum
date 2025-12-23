using Core.Types.Dtos;
using CoreWeb.Api.Features.Flags;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace CoreWeb.Api.Tests;

public class FeatureFlagServiceTests
{
    private sealed class RecordingNotifier : IFlagChangeNotifier
    {
        public List<FlagsDelta> Deltas { get; } = new();

        public Task NotifyAsync(FlagsDelta delta, CancellationToken cancellationToken = default)
        {
            Deltas.Add(delta);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Resolves_precedence_user_overrides_all()
    {
        var store = new InMemoryFeatureFlagStore();
        var notifier = new RecordingNotifier();
        var sut = new FeatureFlagService(store, notifier);

        await sut.SetAsync("featureA.enabled", new FlagValue
        {
            Key = "featureA.enabled",
            Scope = FlagScope.Global.ToString(),
            ScopeReference = null,
            Type = "boolean",
            Value = false
        }, CancellationToken.None);

        await sut.SetAsync("featureA.enabled", new FlagValue
        {
            Key = "featureA.enabled",
            Scope = FlagScope.Tenant.ToString(),
            ScopeReference = "tenant-1",
            Type = "boolean",
            Value = true
        }, CancellationToken.None);

        await sut.SetAsync("featureA.enabled", new FlagValue
        {
            Key = "featureA.enabled",
            Scope = FlagScope.Role.ToString(),
            ScopeReference = "admin",
            Type = "boolean",
            Value = false
        }, CancellationToken.None);

        await sut.SetAsync("featureA.enabled", new FlagValue
        {
            Key = "featureA.enabled",
            Scope = FlagScope.User.ToString(),
            ScopeReference = "user-42",
            Type = "boolean",
            Value = true
        }, CancellationToken.None);

        var ctx = new EvaluationContext
        {
            TenantId = "tenant-1",
            UserId = "user-42",
            Roles = new[] { "admin" }
        };

        var result = await sut.GetBooleanAsync("featureA.enabled", ctx, false, CancellationToken.None);

        Assert.True(result);
        Assert.NotEmpty(notifier.Deltas);
    }
}
