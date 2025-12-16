using System.Linq;
using System.Threading.Tasks;
using Identifier.Infrastructure.Persistence;
using Identifier.Infrastructure.Seeding;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using Xunit;

namespace Identifier.Api.IntegrationTests;

public class IdentifierApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _timescaleContainer;

    public IdentifierApiFactory()
    {
        _timescaleContainer = new PostgreSqlBuilder()
            .WithDatabase("identifier")
            .WithUsername("postgres")
            .WithPassword("postgres")
            //TODO fix on configuration
            .WithImage("timescale/timescaledb-ha:pg15.5-ts2.13.1")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _timescaleContainer.StartAsync();
    }

    // xUnit: DEVE essere Task, quindi implementazione esplicita
    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();

    // WebApplicationFactory: override "vero" (ValueTask)
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _timescaleContainer.DisposeAsync();
    }


    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<IdentifierDbContext>>();

            services.AddDbContext<IdentifierDbContext>(options =>
            {
                options.UseNpgsql(_timescaleContainer.GetConnectionString(), npgsql =>
                {
                    npgsql.MigrationsAssembly("Identifier.Infrastructure"); // ✅ fondamentale
                });
            });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentifierDbContext>();

        db.Database.Migrate();

        var seeder = scope.ServiceProvider.GetRequiredService<IdentifierSeeder>();
        seeder.SeedAsync(CancellationToken.None).GetAwaiter().GetResult();

        return host;
    }


}
