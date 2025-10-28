using System.Linq;
using Identifier.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Identifier.Api.IntegrationTests;

public class IdentifierApiFactory : WebApplicationFactory<Program>
{
    private readonly PostgreSqlTestcontainer _timescaleContainer;
    private bool _initialized;

    public IdentifierApiFactory()
    {
        var configuration = new PostgreSqlTestcontainerConfiguration
        {
            Database = "identifier",
            Username = "postgres",
            Password = "postgres",
            Image = "timescale/timescaledb-ha:pg15.5-ts2.13.1"
        };

        _timescaleContainer = new PostgreSqlTestcontainer(configuration);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        EnsureContainerStarted();

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<IdentifierDbContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<IdentifierDbContext>(options =>
            {
                options.UseNpgsql(_timescaleContainer.ConnectionString);
            });

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IdentifierDbContext>();
            db.Database.Migrate();
        });
    }

    private void EnsureContainerStarted()
    {
        if (_initialized)
        {
            return;
        }

        _timescaleContainer.StartAsync().GetAwaiter().GetResult();
        _initialized = true;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (_initialized)
        {
            _timescaleContainer.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }
}
