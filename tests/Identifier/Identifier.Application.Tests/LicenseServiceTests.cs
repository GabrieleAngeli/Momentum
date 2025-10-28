using FluentAssertions;
using Identifier.Domain.Entities;
using Identifier.Infrastructure.Persistence;
using Identifier.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Identifier.Application.Tests;

public class LicenseServiceTests
{
    [Fact]
    public async Task Returns_NoActiveLicense_When_NoneValid()
    {
        await using var context = CreateContext();
        var service = new LicenseService(context);

        var evaluation = await service.EvaluateAsync(Guid.NewGuid(), "devices.core");

        evaluation.HasLicense.Should().BeFalse();
        evaluation.Reason.Should().Be("no-active-license");
    }

    [Fact]
    public async Task Returns_FeatureIncluded_When_EntitlementExists()
    {
        await using var context = CreateContext();
        var orgId = Guid.NewGuid();
        var feature = new Feature { Id = Guid.NewGuid(), Key = "devices.core" };
        var license = new License
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Tier = "pro",
            ValidFrom = DateTimeOffset.UtcNow.AddDays(-1),
            ValidTo = DateTimeOffset.UtcNow.AddDays(30)
        };
        context.Features.Add(feature);
        context.Licenses.Add(license);
        context.Entitlements.Add(new Entitlement
        {
            Id = Guid.NewGuid(),
            LicenseId = license.Id,
            FeatureId = feature.Id,
            Quota = 10
        });
        await context.SaveChangesAsync();

        var service = new LicenseService(context);
        var evaluation = await service.EvaluateAsync(orgId, feature.Key);

        evaluation.HasLicense.Should().BeTrue();
        evaluation.FeatureIncluded.Should().BeTrue();
        evaluation.WithinQuota.Should().BeTrue();
        evaluation.RemainingQuota.Should().Be(10);
    }

    private static IdentifierDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<IdentifierDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new IdentifierDbContext(options);
    }
}
