using Identifier.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identifier.Infrastructure.Persistence.Configurations;

public class GroupRoleConfiguration : IEntityTypeConfiguration<GroupRole>
{
    public void Configure(EntityTypeBuilder<GroupRole> builder)
    {
        builder.ToTable("GroupRoles");
        builder.HasKey(gr => new { gr.GroupId, gr.RoleId });
    }
}
