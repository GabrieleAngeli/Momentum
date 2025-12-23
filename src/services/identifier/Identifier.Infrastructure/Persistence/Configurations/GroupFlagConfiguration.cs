using Identifier.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identifier.Infrastructure.Persistence.Configurations;

public class GroupFlagConfiguration : IEntityTypeConfiguration<GroupFlag>
{
    public void Configure(EntityTypeBuilder<GroupFlag> builder)
    {
        builder.ToTable("GroupFlags");
        builder.HasKey(gf => gf.Id);
        builder.Property(gf => gf.Variation)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(gf => new { gf.GroupId, gf.FeatureFlagId })
            .IsUnique();
    }
}
