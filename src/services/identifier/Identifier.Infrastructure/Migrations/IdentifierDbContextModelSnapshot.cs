using System;
using Identifier.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace Identifier.Infrastructure.Migrations;

[DbContext(typeof(IdentifierDbContext))]
partial class IdentifierDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.4")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        modelBuilder.Entity("Identifier.Domain.Entities.Entitlement", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uuid");

            b.Property<string>("ConstraintsJson")
                .HasColumnType("character varying(max)");

            b.Property<Guid>("FeatureId")
                .HasColumnType("uuid");

            b.Property<Guid>("LicenseId")
                .HasColumnType("uuid");

            b.Property<int?>("Quota")
                .HasColumnType("int");

            b.HasKey("Id");

            b.HasIndex("FeatureId");

            b.HasIndex("LicenseId");

            b.ToTable("Entitlements");
        });

        modelBuilder.Entity("Identifier.Domain.Entities.Feature", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uuid");

            b.Property<string>("Key")
                .IsRequired()
                .HasMaxLength(128)
                .HasColumnType("character varying(128)");

            b.HasKey("Id");

            b.HasIndex("Key")
                .IsUnique();

            b.ToTable("Features");
        });

        modelBuilder.Entity("Identifier.Domain.Entities.FeatureFlag", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uuid");

            b.Property<string>("DefaultVariation")
                .IsRequired()
                .HasMaxLength(64)
                .HasColumnType("character varying(64)");

            b.Property<string>("Key")
                .IsRequired()
                .HasMaxLength(128)
                .HasColumnType("character varying(128)");

            b.HasKey("Id");

            b.HasIndex("Key")
                .IsUnique();

            b.ToTable("FeatureFlags");
        });

        modelBuilder.Entity("Identifier.Domain.Entities.Group", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uuid");

            b.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(128)
                .HasColumnType("character varying(128)");

            b.Property<Guid>("OrganizationId")
                .HasColumnType("uuid");

            b.HasKey("Id");

            b.HasIndex("OrganizationId");

            b.ToTable("Groups");
        });

        modelBuilder.Entity("Identifier.Domain.Entities.GroupFlag", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uuid");

            b.Property<Guid>("FeatureFlagId")
                .HasColumnType("uuid");

            b.Property<Guid>("GroupId")
                .HasColumnType("uuid");

            b.Property<string>("Variation")
                .IsRequired()
                .HasMaxLength(64)
                .HasColumnType("character varying(64)");

            b.HasKey("Id");

            b.HasIndex("FeatureFlagId");

            b.HasIndex("GroupId", "FeatureFlagId")
                .IsUnique();

            b.ToTable("GroupFlags");
        });

        modelBuilder.Entity("Identifier.Domain.Entities.GroupRole", b =>
        {
            b.Property<Guid>("GroupId")
                .HasColumnType("uuid");

            b.Property<Guid>("RoleId")
                .HasColumnType("uuid");

            b.HasKey("GroupId", "RoleId");

            b.HasIndex("RoleId");

            b.ToTable("GroupRoles");
        });

        modelBuilder.Entity("Identifier.Domain.Entities.License", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uuid");

            b.Property<Guid>("OrganizationId")
                .HasColumnType("uuid");

            b.Property<string>("Tier")
                .IsRequired()
                .HasMaxLength(64)
                .HasColumnType("character varying(64)");

            b.Property<DateTimeOffset>("ValidFrom")
                .HasColumnType("datetimeoffset");

            b.Property<DateTimeOffset>("ValidTo")
                .HasColumnType("datetimeoffset");

            b.HasKey("Id");

            b.HasIndex("OrganizationId");

            b.ToTable("Licenses");
        });

        modelBuilder.Entity("Identifier.Domain.Entities.LicenseModule", b =>
        {
            b.Property<Guid>("LicenseId")
                .HasColumnType("uuid");

            b.Property<Guid>("ModuleId")
                .HasColumnType("uuid");

            b.HasKey("LicenseId", "ModuleId");

            b.HasIndex("ModuleId");

            b.ToTable("LicenseModules");
        });

        modelBuilder.Entity("Identifier.Domain.Entities.Module", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uuid");

            b.Property<string>("Key")
                .IsRequired()
                .HasMaxLength(128)
                .HasColumnType("character varying(128)");

            b.HasKey("Id");

            b.HasIndex("Key")
                .IsUnique();

            b.ToTable("Modules");
        });

        modelBuilder.Entity("Identifier.Domain.Entities.ModuleFeature", b =>
        {
            b.Property<Guid>("ModuleId")
                .HasColumnType("uuid");

            b.Property<Guid>("FeatureId")
                .HasColumnType("uuid");

            b.HasKey("ModuleId", "FeatureId");

            b.HasIndex("FeatureId");

            b.ToTable("ModuleFeatures");
        });

        modelBuilder.Entity("Identifier.Domain.Entities.OrgFlag", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uuid");

            b.Property<Guid>("FeatureFlagId")
                .HasColumnType("uuid");

            b.Property<Guid>("OrganizationId")
                .HasColumnType("uuid");

            b.Property<string>("RuleJson")
                .HasColumnType("character varying(max)");

            b.Property<string>("Variation")
                .IsRequired()
                .HasMaxLength(64)
                .HasColumnType("character varying(64)");

            b.HasKey("Id");

            b.HasIndex("FeatureFlagId");

            b.HasIndex("OrganizationId", "FeatureFlagId")
                .IsUnique();

            b.ToTable("OrgFlags");
        });

        modelBuilder.Entity("Identifier.Domain.Entities.Organization", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uuid");

            b.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(256)
                .HasColumnType("character varying(256)");

            b.HasKey("Id");

            b.ToTable("Organizations");
        });

        modelBuilder.Entity("Identifier.Domain.Entities.Permission", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uuid");

            b.Property<string>("Action")
                .IsRequired()
                .HasMaxLength(128)
                .HasColumnType("character varying(128)");

            b.Property<string>("Code")
                .IsRequired()
                .HasMaxLength(128)
                .HasColumnType("character varying(128)");

            b.Property<string>("Resource")
                .IsRequired()
                .HasMaxLength(128)
                .HasColumnType("character varying(128)");

            b.HasKey("Id");

            b.HasIndex("Code")
                .IsUnique();

            b.ToTable("Permissions");
        });

        modelBuilder.Entity("Identifier.Domain.Entities.Role", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uuid");

            b.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(128)
                .HasColumnType("character varying(128)");

            b.HasKey("Id");

            b.ToTable("Roles");
        });

        modelBuilder.Entity("Identifier.Domain.Entities.RolePermission", b =>
        {
            b.Property<Guid>("RoleId")
                .HasColumnType("uuid");

            b.Property<Guid>("PermissionId")
                .HasColumnType("uuid");

            b.HasKey("RoleId", "PermissionId");

            b.HasIndex("PermissionId");

            b.ToTable("RolePermissions");
        });

        modelBuilder.Entity("Identifier.Domain.Entities.User", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uuid");

            b.Property<bool>("Active")
                .HasColumnType("bit");

            b.Property<string>("Email")
                .IsRequired()
                .HasMaxLength(256)
                .HasColumnType("character varying(256)");

            b.Property<Guid>("OrganizationId")
                .HasColumnType("uuid");

            b.HasKey("Id");

            b.HasIndex("OrganizationId");

            b.HasIndex("OrganizationId", "Email")
                .IsUnique();

            b.ToTable("Users");
        });

        modelBuilder.Entity("Identifier.Domain.Entities.UserFlag", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uuid");

            b.Property<Guid>("FeatureFlagId")
                .HasColumnType("uuid");

            b.Property<Guid>("UserId")
                .HasColumnType("uuid");

            b.Property<string>("Variation")
                .IsRequired()
                .HasMaxLength(64)
                .HasColumnType("character varying(64)");

            b.HasKey("Id");

            b.HasIndex("FeatureFlagId");

            b.HasIndex("UserId", "FeatureFlagId")
                .IsUnique();

            b.ToTable("UserFlags");
        });

        modelBuilder.Entity("Identifier.Domain.Entities.UserGroup", b =>
        {
            b.Property<Guid>("UserId")
                .HasColumnType("uuid");

            b.Property<Guid>("GroupId")
                .HasColumnType("uuid");

            b.HasKey("UserId", "GroupId");

            b.HasIndex("GroupId");

            b.ToTable("UserGroups");
        });

        modelBuilder.Entity("Identifier.Domain.Entities.Entitlement", b =>
        {
            b.HasOne("Identifier.Domain.Entities.Feature", "Feature")
                .WithMany("Entitlements")
                .HasForeignKey("FeatureId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.HasOne("Identifier.Domain.Entities.License", "License")
                .WithMany("Entitlements")
                .HasForeignKey("LicenseId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("Feature");

            b.Navigation("License");
        });

        modelBuilder.Entity("Identifier.Domain.Entities.Group", b =>
        {
            b.HasOne("Identifier.Domain.Entities.Organization", "Organization")
                .WithMany("Groups")
                .HasForeignKey("OrganizationId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("Organization");
        });

        modelBuilder.Entity("Identifier.Domain.Entities.GroupFlag", b =>
        {
            b.HasOne("Identifier.Domain.Entities.FeatureFlag", "FeatureFlag")
                .WithMany("GroupFlags")
                .HasForeignKey("FeatureFlagId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.HasOne("Identifier.Domain.Entities.Group", "Group")
                .WithMany("Flags")
                .HasForeignKey("GroupId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("FeatureFlag");

            b.Navigation("Group");
        });

        modelBuilder.Entity("Identifier.Domain.Entities.GroupRole", b =>
        {
            b.HasOne("Identifier.Domain.Entities.Group", "Group")
                .WithMany("GroupRoles")
                .HasForeignKey("GroupId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.HasOne("Identifier.Domain.Entities.Role", "Role")
                .WithMany("GroupRoles")
                .HasForeignKey("RoleId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("Group");

            b.Navigation("Role");
        });

        modelBuilder.Entity("Identifier.Domain.Entities.License", b =>
        {
            b.HasOne("Identifier.Domain.Entities.Organization", "Organization")
                .WithMany("Licenses")
                .HasForeignKey("OrganizationId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("Organization");
        });

        modelBuilder.Entity("Identifier.Domain.Entities.LicenseModule", b =>
        {
            b.HasOne("Identifier.Domain.Entities.License", "License")
                .WithMany("Modules")
                .HasForeignKey("LicenseId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.HasOne("Identifier.Domain.Entities.Module", "Module")
                .WithMany("Licenses")
                .HasForeignKey("ModuleId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("License");

            b.Navigation("Module");
        });

        modelBuilder.Entity("Identifier.Domain.Entities.ModuleFeature", b =>
        {
            b.HasOne("Identifier.Domain.Entities.Feature", "Feature")
                .WithMany("Modules")
                .HasForeignKey("FeatureId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.HasOne("Identifier.Domain.Entities.Module", "Module")
                .WithMany("Features")
                .HasForeignKey("ModuleId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("Feature");

            b.Navigation("Module");
        });

        modelBuilder.Entity("Identifier.Domain.Entities.OrgFlag", b =>
        {
            b.HasOne("Identifier.Domain.Entities.FeatureFlag", "FeatureFlag")
                .WithMany("OrgFlags")
                .HasForeignKey("FeatureFlagId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.HasOne("Identifier.Domain.Entities.Organization", "Organization")
                .WithMany("Flags")
                .HasForeignKey("OrganizationId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("FeatureFlag");

            b.Navigation("Organization");
        });

        modelBuilder.Entity("Identifier.Domain.Entities.RolePermission", b =>
        {
            b.HasOne("Identifier.Domain.Entities.Permission", "Permission")
                .WithMany("RolePermissions")
                .HasForeignKey("PermissionId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.HasOne("Identifier.Domain.Entities.Role", "Role")
                .WithMany("RolePermissions")
                .HasForeignKey("RoleId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("Permission");

            b.Navigation("Role");
        });

        modelBuilder.Entity("Identifier.Domain.Entities.User", b =>
        {
            b.HasOne("Identifier.Domain.Entities.Organization", "Organization")
                .WithMany("Users")
                .HasForeignKey("OrganizationId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("Organization");
        });

        modelBuilder.Entity("Identifier.Domain.Entities.UserFlag", b =>
        {
            b.HasOne("Identifier.Domain.Entities.FeatureFlag", "FeatureFlag")
                .WithMany("UserFlags")
                .HasForeignKey("FeatureFlagId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.HasOne("Identifier.Domain.Entities.User", "User")
                .WithMany("Flags")
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("FeatureFlag");

            b.Navigation("User");
        });

        modelBuilder.Entity("Identifier.Domain.Entities.UserGroup", b =>
        {
            b.HasOne("Identifier.Domain.Entities.Group", "Group")
                .WithMany("UserGroups")
                .HasForeignKey("GroupId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.HasOne("Identifier.Domain.Entities.User", "User")
                .WithMany("UserGroups")
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("Group");

            b.Navigation("User");
        });

        modelBuilder.Entity("Identifier.Domain.Entities.Feature", b =>
        {
            b.Navigation("Entitlements");

            b.Navigation("Modules");
        });

        modelBuilder.Entity("Identifier.Domain.Entities.FeatureFlag", b =>
        {
            b.Navigation("GroupFlags");

            b.Navigation("OrgFlags");

            b.Navigation("UserFlags");
        });

        modelBuilder.Entity("Identifier.Domain.Entities.Group", b =>
        {
            b.Navigation("Flags");

            b.Navigation("GroupRoles");

            b.Navigation("UserGroups");
        });

        modelBuilder.Entity("Identifier.Domain.Entities.License", b =>
        {
            b.Navigation("Entitlements");

            b.Navigation("Modules");
        });

        modelBuilder.Entity("Identifier.Domain.Entities.Module", b =>
        {
            b.Navigation("Features");

            b.Navigation("Licenses");
        });

        modelBuilder.Entity("Identifier.Domain.Entities.Organization", b =>
        {
            b.Navigation("Flags");

            b.Navigation("Groups");

            b.Navigation("Licenses");

            b.Navigation("Users");
        });

        modelBuilder.Entity("Identifier.Domain.Entities.Permission", b =>
        {
            b.Navigation("RolePermissions");
        });

        modelBuilder.Entity("Identifier.Domain.Entities.Role", b =>
        {
            b.Navigation("GroupRoles");

            b.Navigation("RolePermissions");
        });

        modelBuilder.Entity("Identifier.Domain.Entities.User", b =>
        {
            b.Navigation("Flags");

            b.Navigation("UserGroups");
        });
#pragma warning restore 612, 618
    }
}
