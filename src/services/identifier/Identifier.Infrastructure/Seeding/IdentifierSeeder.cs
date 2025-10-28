using Identifier.Domain.Entities;
using Identifier.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Identifier.Infrastructure.Seeding;

public class IdentifierSeeder
{
    private readonly IdentifierDbContext _dbContext;
    private readonly ILogger<IdentifierSeeder> _logger;

    public IdentifierSeeder(IdentifierDbContext dbContext, ILogger<IdentifierSeeder> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        await SeedPermissionsAsync(cancellationToken);
        await SeedRolesAsync(cancellationToken);
        await SeedFeaturesAsync(cancellationToken);
        await SeedModulesAsync(cancellationToken);
        await SeedFeatureFlagsAsync(cancellationToken);
        await SeedDemoOrganizationAsync(cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task SeedPermissionsAsync(CancellationToken cancellationToken)
    {
        var permissions = new List<Permission>
        {
            new() { Id = Guid.Parse("a5d33914-8e2e-4974-a7f1-040017f3c7cb"), Code = "devices:read", Resource = "devices", Action = "read" },
            new() { Id = Guid.Parse("6740a1a1-0f5d-4270-9d8f-d2f4c1ef3432"), Code = "devices:manage", Resource = "devices", Action = "manage" },
            new() { Id = Guid.Parse("4e596f0e-37d4-4b1f-b6ad-1c6fbce244a5"), Code = "realtime:stream", Resource = "realtime", Action = "stream" }
        };

        foreach (var permission in permissions)
        {
            var existing = await _dbContext.Permissions.FirstOrDefaultAsync(p => p.Code == permission.Code, cancellationToken);
            if (existing is null)
            {
                _dbContext.Permissions.Add(permission);
            }
            else
            {
                existing.Resource = permission.Resource;
                existing.Action = permission.Action;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        var adminRoleId = Guid.Parse("f4b1aa2b-51cc-437e-8aaf-5db9f667f44d");
        var viewerRoleId = Guid.Parse("f266233d-e6c9-4fa9-9fb2-d0f7ed0f3f81");

        var adminRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Id == adminRoleId, cancellationToken);
        if (adminRole is null)
        {
            adminRole = new Role { Id = adminRoleId, Name = "Administrator" };
            _dbContext.Roles.Add(adminRole);
        }
        else
        {
            adminRole.Name = "Administrator";
        }

        var viewerRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Id == viewerRoleId, cancellationToken);
        if (viewerRole is null)
        {
            viewerRole = new Role { Id = viewerRoleId, Name = "Viewer" };
            _dbContext.Roles.Add(viewerRole);
        }
        else
        {
            viewerRole.Name = "Viewer";
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var permissionCodes = new[] { "devices:read", "devices:manage", "realtime:stream" };
        var permissions = await _dbContext.Permissions
            .Where(p => permissionCodes.Contains(p.Code))
            .ToListAsync(cancellationToken);

        foreach (var permission in permissions)
        {
            if (!await _dbContext.RolePermissions.AnyAsync(rp => rp.RoleId == adminRoleId && rp.PermissionId == permission.Id, cancellationToken))
            {
                _dbContext.RolePermissions.Add(new RolePermission { RoleId = adminRoleId, PermissionId = permission.Id });
            }
        }

        var viewerPermission = permissions.FirstOrDefault(p => p.Code == "devices:read");
        if (viewerPermission is not null && !await _dbContext.RolePermissions.AnyAsync(rp => rp.RoleId == viewerRoleId && rp.PermissionId == viewerPermission.Id, cancellationToken))
        {
            _dbContext.RolePermissions.Add(new RolePermission { RoleId = viewerRoleId, PermissionId = viewerPermission.Id });
        }
    }

    private async Task SeedFeaturesAsync(CancellationToken cancellationToken)
    {
        var features = new List<Feature>
        {
            new() { Id = Guid.Parse("c3a62d6b-f2a5-4f8c-9d1f-9e9c345a9a29"), Key = "devices.core" },
            new() { Id = Guid.Parse("2f3d3d4d-3141-4ce3-93fc-1d4d0c1d9f4c"), Key = "realtime.stream" },
            new() { Id = Guid.Parse("f3bcbfe3-88a8-4c65-8139-18b92c7d6e75"), Key = "ui.newDashboard" }
        };

        foreach (var feature in features)
        {
            var existing = await _dbContext.Features.FirstOrDefaultAsync(f => f.Key == feature.Key, cancellationToken);
            if (existing is null)
            {
                _dbContext.Features.Add(feature);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedModulesAsync(CancellationToken cancellationToken)
    {
        var iotModuleId = Guid.Parse("1b6c7c2f-0739-46f8-8d7d-25aa4bf8d2db");
        var realtimeModuleId = Guid.Parse("a1f2478a-c4d1-4fa2-8f74-ec6c0e364e8b");

        var iotModule = await _dbContext.Modules.FirstOrDefaultAsync(m => m.Id == iotModuleId, cancellationToken);
        if (iotModule is null)
        {
            iotModule = new Module { Id = iotModuleId, Key = "iot.core" };
            _dbContext.Modules.Add(iotModule);
        }
        else
        {
            iotModule.Key = "iot.core";
        }

        var realtimeModule = await _dbContext.Modules.FirstOrDefaultAsync(m => m.Id == realtimeModuleId, cancellationToken);
        if (realtimeModule is null)
        {
            realtimeModule = new Module { Id = realtimeModuleId, Key = "realtime" };
            _dbContext.Modules.Add(realtimeModule);
        }
        else
        {
            realtimeModule.Key = "realtime";
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var devicesFeatureId = await _dbContext.Features.Where(f => f.Key == "devices.core").Select(f => f.Id).FirstAsync(cancellationToken);
        var realtimeFeatureId = await _dbContext.Features.Where(f => f.Key == "realtime.stream").Select(f => f.Id).FirstAsync(cancellationToken);

        if (!await _dbContext.ModuleFeatures.AnyAsync(mf => mf.ModuleId == iotModuleId && mf.FeatureId == devicesFeatureId, cancellationToken))
        {
            _dbContext.ModuleFeatures.Add(new ModuleFeature { ModuleId = iotModuleId, FeatureId = devicesFeatureId });
        }

        if (!await _dbContext.ModuleFeatures.AnyAsync(mf => mf.ModuleId == realtimeModuleId && mf.FeatureId == realtimeFeatureId, cancellationToken))
        {
            _dbContext.ModuleFeatures.Add(new ModuleFeature { ModuleId = realtimeModuleId, FeatureId = realtimeFeatureId });
        }
    }

    private async Task SeedFeatureFlagsAsync(CancellationToken cancellationToken)
    {
        var flagId = Guid.Parse("b2b1fd8b-9994-4d17-9348-5a2e7c9b4639");
        var flag = await _dbContext.FeatureFlags.FirstOrDefaultAsync(f => f.Id == flagId, cancellationToken);
        if (flag is null)
        {
            flag = new FeatureFlag { Id = flagId, Key = "ui.newDashboard", DefaultVariation = "off" };
            _dbContext.FeatureFlags.Add(flag);
        }
        else
        {
            flag.Key = "ui.newDashboard";
            flag.DefaultVariation = "off";
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedDemoOrganizationAsync(CancellationToken cancellationToken)
    {
        var orgId = Guid.Parse("3f7b5937-5e63-4d0e-8267-29aef39915af");
        var userId = Guid.Parse("08fb1fb2-541d-4720-9f61-89d33bd44ddc");
        var groupId = Guid.Parse("d43f6bbd-235a-49f1-83f0-4c8c833ca612");
        var licenseId = Guid.Parse("2c8a5f8b-5170-4e2d-8405-99d33721d11d");

        var organization = await _dbContext.Organizations.FirstOrDefaultAsync(o => o.Id == orgId, cancellationToken);
        if (organization is null)
        {
            organization = new Organization { Id = orgId, Name = "Demo Organization" };
            _dbContext.Organizations.Add(organization);
        }
        else
        {
            organization.Name = "Demo Organization";
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            user = new User { Id = userId, OrganizationId = orgId, Email = "admin@demo.local", Active = true };
            _dbContext.Users.Add(user);
        }
        else
        {
            user.OrganizationId = orgId;
            user.Email = "admin@demo.local";
            user.Active = true;
        }

        var group = await _dbContext.Groups.FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);
        if (group is null)
        {
            group = new Group { Id = groupId, OrganizationId = orgId, Name = "Administrators" };
            _dbContext.Groups.Add(group);
        }
        else
        {
            group.OrganizationId = orgId;
            group.Name = "Administrators";
        }

        if (!await _dbContext.UserGroups.AnyAsync(ug => ug.UserId == userId && ug.GroupId == groupId, cancellationToken))
        {
            _dbContext.UserGroups.Add(new UserGroup { UserId = userId, GroupId = groupId });
        }

        var adminRoleId = Guid.Parse("f4b1aa2b-51cc-437e-8aaf-5db9f667f44d");
        if (!await _dbContext.GroupRoles.AnyAsync(gr => gr.GroupId == groupId && gr.RoleId == adminRoleId, cancellationToken))
        {
            _dbContext.GroupRoles.Add(new GroupRole { GroupId = groupId, RoleId = adminRoleId });
        }

        var license = await _dbContext.Licenses.FirstOrDefaultAsync(l => l.Id == licenseId, cancellationToken);
        if (license is null)
        {
            license = new License
            {
                Id = licenseId,
                OrganizationId = orgId,
                ValidFrom = DateTimeOffset.UtcNow.Date.AddDays(-30),
                ValidTo = DateTimeOffset.UtcNow.Date.AddYears(1),
                Tier = "enterprise"
            };
            _dbContext.Licenses.Add(license);
        }
        else
        {
            license.OrganizationId = orgId;
            license.ValidFrom = DateTimeOffset.UtcNow.Date.AddDays(-30);
            license.ValidTo = DateTimeOffset.UtcNow.Date.AddYears(1);
            license.Tier = "enterprise";
        }

        var devicesFeatureId = await _dbContext.Features.Where(f => f.Key == "devices.core").Select(f => f.Id).FirstAsync(cancellationToken);
        var realtimeFeatureId = await _dbContext.Features.Where(f => f.Key == "realtime.stream").Select(f => f.Id).FirstAsync(cancellationToken);

        if (!await _dbContext.Entitlements.AnyAsync(e => e.LicenseId == licenseId && e.FeatureId == devicesFeatureId, cancellationToken))
        {
            _dbContext.Entitlements.Add(new Entitlement
            {
                Id = Guid.Parse("0f6bf5a0-2cdf-4d1a-8b05-8a4d9b1d530d"),
                LicenseId = licenseId,
                FeatureId = devicesFeatureId,
                Quota = null,
                ConstraintsJson = null
            });
        }

        if (!await _dbContext.Entitlements.AnyAsync(e => e.LicenseId == licenseId && e.FeatureId == realtimeFeatureId, cancellationToken))
        {
            _dbContext.Entitlements.Add(new Entitlement
            {
                Id = Guid.Parse("74f59018-5d79-4bd6-84ff-fb19d81e0b10"),
                LicenseId = licenseId,
                FeatureId = realtimeFeatureId,
                Quota = 1000,
                ConstraintsJson = null
            });
        }

        if (!await _dbContext.LicenseModules.AnyAsync(lm => lm.LicenseId == licenseId && lm.ModuleId == Guid.Parse("1b6c7c2f-0739-46f8-8d7d-25aa4bf8d2db"), cancellationToken))
        {
            _dbContext.LicenseModules.Add(new LicenseModule { LicenseId = licenseId, ModuleId = Guid.Parse("1b6c7c2f-0739-46f8-8d7d-25aa4bf8d2db") });
        }

        if (!await _dbContext.LicenseModules.AnyAsync(lm => lm.LicenseId == licenseId && lm.ModuleId == Guid.Parse("a1f2478a-c4d1-4fa2-8f74-ec6c0e364e8b"), cancellationToken))
        {
            _dbContext.LicenseModules.Add(new LicenseModule { LicenseId = licenseId, ModuleId = Guid.Parse("a1f2478a-c4d1-4fa2-8f74-ec6c0e364e8b") });
        }

        var flagId = Guid.Parse("b2b1fd8b-9994-4d17-9348-5a2e7c9b4639");
        if (!await _dbContext.OrgFlags.AnyAsync(of => of.OrganizationId == orgId && of.FeatureFlagId == flagId, cancellationToken))
        {
            _dbContext.OrgFlags.Add(new OrgFlag
            {
                Id = Guid.Parse("2d709533-793c-4d09-ae54-2b7f122c88ea"),
                OrganizationId = orgId,
                FeatureFlagId = flagId,
                Variation = "on",
                RuleJson = null
            });
        }
    }
}
