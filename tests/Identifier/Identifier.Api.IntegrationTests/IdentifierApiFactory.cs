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
            .WithDatabase("momentum_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            //TODO fix on configuration
            .WithImage("timescale/timescaledb:2.22.1-pg16")
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
                    npgsql.MigrationsAssembly(typeof(Identifier.Infrastructure.Migrations.InitialIdentifierSchema).Assembly.GetName().Name);
                });
            });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentifierDbContext>();

        Console.WriteLine("IDENTIFIER_CS: " + db.Database.GetConnectionString());

        db.Database.Migrate();

        // DEBUG: ti dice subito se ha migrato davvero
        var pending = db.Database.GetPendingMigrations().ToList();
        Console.WriteLine("PENDING: " + string.Join(", ", pending));

        var applied = db.Database.GetAppliedMigrations().ToList();
        Console.WriteLine("APPLIED: " + string.Join(", ", applied));

        var seeder = scope.ServiceProvider.GetRequiredService<IdentifierSeeder>();
        seeder.SeedAsync(CancellationToken.None).GetAwaiter().GetResult();

        return host;
    }


}
