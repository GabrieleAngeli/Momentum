using Identifier.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identifier.Infrastructure.Persistence.Configurations;

public class UserFlagConfiguration : IEntityTypeConfiguration<UserFlag>
{
    public void Configure(EntityTypeBuilder<UserFlag> builder)
    {
        builder.ToTable("UserFlags");
        builder.HasKey(uf => uf.Id);
        builder.Property(uf => uf.Variation)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(uf => new { uf.UserId, uf.FeatureFlagId })
            .IsUnique();
    }
}
