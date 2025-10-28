using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identifier.Infrastructure.Migrations;

public partial class Add_Identifier_Schema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "FeatureFlags",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                DefaultVariation = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Key = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                DefaultVariation = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FeatureFlags", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Features",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Key = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Features", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Modules",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Key = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Modules", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Organizations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Organizations", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Permissions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                Resource = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                Action = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Permissions", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Roles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Roles", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Licenses",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                ValidFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ValidTo = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Tier = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Licenses", x => x.Id);
                table.ForeignKey(
                    name: "FK_Licenses_Organizations_OrganizationId",
                    column: x => x.OrganizationId,
                    principalTable: "Organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Groups",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Groups", x => x.Id);
                table.ForeignKey(
                    name: "FK_Groups_Organizations_OrganizationId",
                    column: x => x.OrganizationId,
                    principalTable: "Organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                Active = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
                table.ForeignKey(
                    name: "FK_Users_Organizations_OrganizationId",
                    column: x => x.OrganizationId,
                    principalTable: "Organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ModuleFeatures",
            columns: table => new
            {
                ModuleId = table.Column<Guid>(type: "uuid", nullable: false),
                FeatureId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ModuleFeatures", x => new { x.ModuleId, x.FeatureId });
                table.ForeignKey(
                    name: "FK_ModuleFeatures_Features_FeatureId",
                    column: x => x.FeatureId,
                    principalTable: "Features",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ModuleFeatures_Modules_ModuleId",
                    column: x => x.ModuleId,
                    principalTable: "Modules",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Entitlements",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                LicenseId = table.Column<Guid>(type: "uuid", nullable: false),
                FeatureId = table.Column<Guid>(type: "uuid", nullable: false),
                Quota = table.Column<int>(type: "integer", nullable: true),
                ConstraintsJson = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Entitlements", x => x.Id);
                table.ForeignKey(
                    name: "FK_Entitlements_Features_FeatureId",
                    column: x => x.FeatureId,
                    principalTable: "Features",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Entitlements_Licenses_LicenseId",
                    column: x => x.LicenseId,
                    principalTable: "Licenses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "LicenseModules",
            columns: table => new
            {
                LicenseId = table.Column<Guid>(type: "uuid", nullable: false),
                ModuleId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LicenseModules", x => new { x.LicenseId, x.ModuleId });
                table.ForeignKey(
                    name: "FK_LicenseModules_Licenses_LicenseId",
                    column: x => x.LicenseId,
                    principalTable: "Licenses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_LicenseModules_Modules_ModuleId",
                    column: x => x.ModuleId,
                    principalTable: "Modules",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "GroupFlags",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                FeatureFlagId = table.Column<Guid>(type: "uuid", nullable: false),
                Variation = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GroupFlags", x => x.Id);
                table.ForeignKey(
                    name: "FK_GroupFlags_FeatureFlags_FeatureFlagId",
                    column: x => x.FeatureFlagId,
                    principalTable: "FeatureFlags",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_GroupFlags_Groups_GroupId",
                    column: x => x.GroupId,
                    principalTable: "Groups",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "GroupRoles",
            columns: table => new
            {
                GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                RoleId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GroupRoles", x => new { x.GroupId, x.RoleId });
                table.ForeignKey(
                    name: "FK_GroupRoles_Groups_GroupId",
                    column: x => x.GroupId,
                    principalTable: "Groups",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_GroupRoles_Roles_RoleId",
                    column: x => x.RoleId,
                    principalTable: "Roles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "OrgFlags",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                FeatureFlagId = table.Column<Guid>(type: "uuid", nullable: false),
                Variation = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                RuleJson = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OrgFlags", x => x.Id);
                table.ForeignKey(
                    name: "FK_OrgFlags_FeatureFlags_FeatureFlagId",
                    column: x => x.FeatureFlagId,
                    principalTable: "FeatureFlags",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_OrgFlags_Organizations_OrganizationId",
                    column: x => x.OrganizationId,
                    principalTable: "Organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "RolePermissions",
            columns: table => new
            {
                RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                PermissionId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                table.ForeignKey(
                    name: "FK_RolePermissions_Permissions_PermissionId",
                    column: x => x.PermissionId,
                    principalTable: "Permissions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_RolePermissions_Roles_RoleId",
                    column: x => x.RoleId,
                    principalTable: "Roles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserFlags",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                FeatureFlagId = table.Column<Guid>(type: "uuid", nullable: false),
                Variation = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserFlags", x => x.Id);
                table.ForeignKey(
                    name: "FK_UserFlags_FeatureFlags_FeatureFlagId",
                    column: x => x.FeatureFlagId,
                    principalTable: "FeatureFlags",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_UserFlags_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserGroups",
            columns: table => new
            {
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                GroupId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserGroups", x => new { x.UserId, x.GroupId });
                table.ForeignKey(
                    name: "FK_UserGroups_Groups_GroupId",
                    column: x => x.GroupId,
                    principalTable: "Groups",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_UserGroups_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Entitlements_FeatureId",
            table: "Entitlements",
            column: "FeatureId");

        migrationBuilder.CreateIndex(
            name: "IX_Entitlements_LicenseId",
            table: "Entitlements",
            column: "LicenseId");

        migrationBuilder.CreateIndex(
            name: "IX_FeatureFlags_Key",
            table: "FeatureFlags",
            column: "Key",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Features_Key",
            table: "Features",
            column: "Key",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_GroupFlags_FeatureFlagId",
            table: "GroupFlags",
            column: "FeatureFlagId");

        migrationBuilder.CreateIndex(
            name: "IX_GroupFlags_GroupId_FeatureFlagId",
            table: "GroupFlags",
            columns: new[] { "GroupId", "FeatureFlagId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_GroupRoles_RoleId",
            table: "GroupRoles",
            column: "RoleId");

        migrationBuilder.CreateIndex(
            name: "IX_Groups_OrganizationId",
            table: "Groups",
            column: "OrganizationId");

        migrationBuilder.CreateIndex(
            name: "IX_LicenseModules_ModuleId",
            table: "LicenseModules",
            column: "ModuleId");

        migrationBuilder.CreateIndex(
            name: "IX_Licenses_OrganizationId",
            table: "Licenses",
            column: "OrganizationId");

        migrationBuilder.CreateIndex(
            name: "IX_Modules_Key",
            table: "Modules",
            column: "Key",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ModuleFeatures_FeatureId",
            table: "ModuleFeatures",
            column: "FeatureId");

        migrationBuilder.CreateIndex(
            name: "IX_OrgFlags_FeatureFlagId",
            table: "OrgFlags",
            column: "FeatureFlagId");

        migrationBuilder.CreateIndex(
            name: "IX_OrgFlags_OrganizationId_FeatureFlagId",
            table: "OrgFlags",
            columns: new[] { "OrganizationId", "FeatureFlagId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Permissions_Code",
            table: "Permissions",
            column: "Code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_RolePermissions_PermissionId",
            table: "RolePermissions",
            column: "PermissionId");

        migrationBuilder.CreateIndex(
            name: "IX_UserFlags_FeatureFlagId",
            table: "UserFlags",
            column: "FeatureFlagId");

        migrationBuilder.CreateIndex(
            name: "IX_UserFlags_UserId_FeatureFlagId",
            table: "UserFlags",
            columns: new[] { "UserId", "FeatureFlagId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Users_OrganizationId",
            table: "Users",
            column: "OrganizationId");

        migrationBuilder.CreateIndex(
            name: "IX_Users_OrganizationId_Email",
            table: "Users",
            columns: new[] { "OrganizationId", "Email" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Entitlements");

        migrationBuilder.DropTable(
            name: "GroupFlags");

        migrationBuilder.DropTable(
            name: "GroupRoles");

        migrationBuilder.DropTable(
            name: "LicenseModules");

        migrationBuilder.DropTable(
            name: "ModuleFeatures");

        migrationBuilder.DropTable(
            name: "OrgFlags");

        migrationBuilder.DropTable(
            name: "RolePermissions");

        migrationBuilder.DropTable(
            name: "UserFlags");

        migrationBuilder.DropTable(
            name: "UserGroups");

        migrationBuilder.DropTable(
            name: "Licenses");

        migrationBuilder.DropTable(
            name: "Permissions");

        migrationBuilder.DropTable(
            name: "Roles");

        migrationBuilder.DropTable(
            name: "FeatureFlags");

        migrationBuilder.DropTable(
            name: "Users");

        migrationBuilder.DropTable(
            name: "Groups");

        migrationBuilder.DropTable(
            name: "Features");

        migrationBuilder.DropTable(
            name: "Modules");

        migrationBuilder.DropTable(
            name: "Organizations");
    }
}
