using Identifier.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identifier.Infrastructure.Persistence.Configurations;

public class LicenseConfiguration : IEntityTypeConfiguration<License>
{
    public void Configure(EntityTypeBuilder<License> builder)
    {
        builder.ToTable("Licenses");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Tier)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasMany(l => l.Entitlements)
            .WithOne(e => e.License)
            .HasForeignKey(e => e.LicenseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(l => l.Modules)
            .WithOne(lm => lm.License)
            .HasForeignKey(lm => lm.LicenseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
