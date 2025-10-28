using Identifier.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identifier.Infrastructure.Persistence.Configurations;

public class LicenseModuleConfiguration : IEntityTypeConfiguration<LicenseModule>
{
    public void Configure(EntityTypeBuilder<LicenseModule> builder)
    {
        builder.ToTable("LicenseModules");
        builder.HasKey(lm => new { lm.LicenseId, lm.ModuleId });
    }
}
