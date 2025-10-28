using FluentAssertions;
using Identifier.Application;
using Identifier.Application.Abstractions;
using Identifier.Domain.Entities;
using Identifier.Infrastructure.Persistence;
using Identifier.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;

namespace Identifier.Application.Tests;

public class AuthorizationEngineTests
{
    [Fact]
    public async Task Denies_When_User_Not_Found()
    {
        await using var context = CreateContext();
        var engine = new AuthorizationEngine(context, Mock.Of<ILicenseService>(), Mock.Of<IFeatureFlagProvider>(), Options.Create(new IdentifierAuthorizationOptions()));

        var decision = await engine.AuthorizeAsync(Guid.NewGuid(), "devices", "read");

        decision.Allowed.Should().BeFalse();
        decision.Reason.Should().Be("user-not-found");
    }

    [Fact]
    public async Task Denies_When_User_Inactive()
    {
        await using var context = CreateContext();
        var org = new Organization { Id = Guid.NewGuid(), Name = "Org" };
        var user = new User { Id = Guid.NewGuid(), OrganizationId = org.Id, Email = "inactive@test", Active = false };
        context.Organizations.Add(org);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var engine = new AuthorizationEngine(context, Mock.Of<ILicenseService>(), Mock.Of<IFeatureFlagProvider>(), Options.Create(new IdentifierAuthorizationOptions()));

        var decision = await engine.AuthorizeAsync(user.Id, "devices", "read");

        decision.Allowed.Should().BeFalse();
        decision.Reason.Should().Be("user-inactive");
    }

    [Fact]
    public async Task Denies_When_Feature_Not_Included()
    {
        await using var context = CreateContext();
        var org = new Organization { Id = Guid.NewGuid(), Name = "Org" };
        var user = new User { Id = Guid.NewGuid(), OrganizationId = org.Id, Email = "user@test", Active = true };
        context.Organizations.Add(org);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var licenseService = new Mock<ILicenseService>();
        licenseService.Setup(s => s.EvaluateAsync(org.Id, "devices.core", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LicenseEvaluation(true, false, false, "feature-not-included", null));

        var options = Options.Create(new IdentifierAuthorizationOptions
        {
            FeatureMap = new Dictionary<string, string> { ["devices:manage"] = "devices.core" }
        });

        var engine = new AuthorizationEngine(context, licenseService.Object, Mock.Of<IFeatureFlagProvider>(), options);

        var decision = await engine.AuthorizeAsync(user.Id, "devices", "manage");

        decision.Allowed.Should().BeFalse();
        decision.Reason.Should().Be("feature-not-included");
    }

    [Fact]
    public async Task Allows_When_Permission_And_License_Present()
    {
        await using var context = CreateContext();
        var org = new Organization { Id = Guid.NewGuid(), Name = "Org" };
        var user = new User { Id = Guid.NewGuid(), OrganizationId = org.Id, Email = "user@test", Active = true };
        var group = new Group { Id = Guid.NewGuid(), OrganizationId = org.Id, Name = "Admins" };
        var role = new Role { Id = Guid.NewGuid(), Name = "Admin" };
        var permission = new Permission { Id = Guid.NewGuid(), Code = "devices:manage", Resource = "devices", Action = "manage" };
        context.AddRange(org, user, group, role, permission);
        context.UserGroups.Add(new UserGroup { UserId = user.Id, GroupId = group.Id });
        context.GroupRoles.Add(new GroupRole { GroupId = group.Id, RoleId = role.Id });
        context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
        await context.SaveChangesAsync();

        var licenseService = new Mock<ILicenseService>();
        licenseService.Setup(s => s.EvaluateAsync(org.Id, "devices.core", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LicenseEvaluation(true, true, true, null, null));

        var flagProvider = new Mock<IFeatureFlagProvider>();
        flagProvider.Setup(p => p.IsEnabledAsync("ui.newDashboard", org.Id, user.Id, It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var options = Options.Create(new IdentifierAuthorizationOptions
        {
            FeatureMap = new Dictionary<string, string> { ["devices:manage"] = "devices.core" },
            FlagMap = new Dictionary<string, string> { ["devices:manage"] = "ui.newDashboard" }
        });

        var engine = new AuthorizationEngine(context, licenseService.Object, flagProvider.Object, options);

        var decision = await engine.AuthorizeAsync(user.Id, "devices", "manage");

        decision.Allowed.Should().BeTrue();
    }

    private static IdentifierDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<IdentifierDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new IdentifierDbContext(options);
    }
}
