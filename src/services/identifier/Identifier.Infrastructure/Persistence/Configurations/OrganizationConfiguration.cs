using Identifier.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identifier.Infrastructure.Persistence.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("Organizations");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasMany(o => o.Users)
            .WithOne(u => u.Organization)
            .HasForeignKey(u => u.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.Groups)
            .WithOne(g => g.Organization)
            .HasForeignKey(g => g.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.Licenses)
            .WithOne(l => l.Organization)
            .HasForeignKey(l => l.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
