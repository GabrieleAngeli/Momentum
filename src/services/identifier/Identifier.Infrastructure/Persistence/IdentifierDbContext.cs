using Identifier.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identifier.Infrastructure.Persistence;

public class IdentifierDbContext : DbContext
{
    public IdentifierDbContext(DbContextOptions<IdentifierDbContext> options) : base(options)
    {
    }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<License> Licenses => Set<License>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<Feature> Features => Set<Feature>();
    public DbSet<Entitlement> Entitlements => Set<Entitlement>();
    public DbSet<LicenseModule> LicenseModules => Set<LicenseModule>();
    public DbSet<ModuleFeature> ModuleFeatures => Set<ModuleFeature>();
    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();
    public DbSet<OrgFlag> OrgFlags => Set<OrgFlag>();
    public DbSet<GroupFlag> GroupFlags => Set<GroupFlag>();
    public DbSet<UserFlag> UserFlags => Set<UserFlag>();
    public DbSet<UserGroup> UserGroups => Set<UserGroup>();
    public DbSet<GroupRole> GroupRoles => Set<GroupRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentifierDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
