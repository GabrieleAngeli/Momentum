using Identifier.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identifier.Infrastructure.Persistence.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Code)
            .HasMaxLength(128)
            .IsRequired();
        builder.Property(p => p.Resource)
            .HasMaxLength(128)
            .IsRequired();
        builder.Property(p => p.Action)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(p => p.Code)
            .IsUnique();
    }
}
