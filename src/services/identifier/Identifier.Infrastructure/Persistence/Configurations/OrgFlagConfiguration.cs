using Identifier.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identifier.Infrastructure.Persistence.Configurations;

public class OrgFlagConfiguration : IEntityTypeConfiguration<OrgFlag>
{
    public void Configure(EntityTypeBuilder<OrgFlag> builder)
    {
        builder.ToTable("OrgFlags");
        builder.HasKey(of => of.Id);
        builder.Property(of => of.Variation)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(of => new { of.OrganizationId, of.FeatureFlagId })
            .IsUnique();
    }
}
