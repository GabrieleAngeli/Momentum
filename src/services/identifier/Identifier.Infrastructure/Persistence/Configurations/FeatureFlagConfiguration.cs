using Identifier.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identifier.Infrastructure.Persistence.Configurations;

public class FeatureFlagConfiguration : IEntityTypeConfiguration<FeatureFlag>
{
    public void Configure(EntityTypeBuilder<FeatureFlag> builder)
    {
        builder.ToTable("FeatureFlags");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Key)
            .HasMaxLength(128)
            .IsRequired();
        builder.Property(f => f.DefaultVariation)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(f => f.Key)
            .IsUnique();
    }
}
