using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Identifier.Infrastructure.Persistence; // metti il namespace giusto

public sealed class IdentifierDbContextFactory : IDesignTimeDbContextFactory<IdentifierDbContext>
{
    public IdentifierDbContext CreateDbContext(string[] args)
    {
        // usa env var (perfetto per CI/dev), fallback locale
        var cs =
            Environment.GetEnvironmentVariable("ConnectionStrings__Identifier")
            ?? "Host=localhost;Port=5432;Database=identifier_dev;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<IdentifierDbContext>()
            .UseNpgsql(cs, npgsql =>
            {
                npgsql.MigrationsAssembly("Identifier.Infrastructure"); // ✅ dove stanno le migrations
            })
            .Options;

        return new IdentifierDbContext(options);
    }
}
