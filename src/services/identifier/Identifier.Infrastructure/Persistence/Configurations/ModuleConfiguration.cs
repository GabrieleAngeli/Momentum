using Identifier.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identifier.Infrastructure.Persistence.Configurations;

public class ModuleConfiguration : IEntityTypeConfiguration<Module>
{
    public void Configure(EntityTypeBuilder<Module> builder)
    {
        builder.ToTable("Modules");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Key)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(m => m.Key)
            .IsUnique();
    }
}
