using Identifier.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identifier.Infrastructure.Persistence.Configurations;

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.ToTable("Groups");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Name)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasMany(g => g.UserGroups)
            .WithOne(ug => ug.Group)
            .HasForeignKey(ug => ug.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(g => g.GroupRoles)
            .WithOne(gr => gr.Group)
            .HasForeignKey(gr => gr.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(g => g.Flags)
            .WithOne(f => f.Group)
            .HasForeignKey(f => f.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
