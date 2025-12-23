using Identifier.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identifier.Infrastructure.Persistence.Configurations;

public class ModuleFeatureConfiguration : IEntityTypeConfiguration<ModuleFeature>
{
    public void Configure(EntityTypeBuilder<ModuleFeature> builder)
    {
        builder.ToTable("ModuleFeatures");
        builder.HasKey(mf => new { mf.ModuleId, mf.FeatureId });
    }
}
