using System.Linq;
using Identifier.Api.Services;
using Identifier.Application;
using Identifier.Application.Abstractions;
using Identifier.Application.Caching;
using Identifier.Infrastructure.Caching;
using Identifier.Infrastructure.Persistence;
using Identifier.Infrastructure.Seeding;
using Identifier.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
var configuration = builder.Configuration;

services.AddHttpContextAccessor();
services.AddScoped<IIdentityProvider, HttpContextIdentityProvider>();
services.Configure<IdentifierAuthorizationOptions>(configuration.GetSection("Identifier:Authorization"));

services.AddDbContext<IdentifierDbContext>(options =>
{
    var connectionString = configuration.GetConnectionString("Identifier");
    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        options.UseSqlServer(connectionString);
    }
    else
    {
        options.UseInMemoryDatabase("identifier");
    }
});

services.AddMemoryCache();
services.AddScoped<IIdentifierCache, MemoryIdentifierCache>();
services.AddScoped<IFeatureFlagProvider, FeatureFlagProvider>();
services.AddScoped<ILicenseService, LicenseService>();
services.AddScoped<IAuthorizationEngine, AuthorizationEngine>();
services.AddScoped<IdentifierSeeder>();

services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
services.AddAuthorization();
services.AddEndpointsApiExplorer();
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Identifier API",
        Version = "v1",
        Description = "RBAC, licensing and feature flag service for Momentum"
    });
});

services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/healthz");

app.MapGet("/api/identifier/authorize", async (
    [FromQuery] Guid userId,
    [FromQuery] string resource,
    [FromQuery] string action,
    IAuthorizationEngine authorizationEngine,
    CancellationToken cancellationToken) =>
{
    var decision = await authorizationEngine.AuthorizeAsync(userId, resource, action, cancellationToken);
    return Results.Json(new { decision.Allowed, decision.Reason });
})
.WithName("Authorize")
.WithOpenApi(operation =>
{
    operation.Summary = "Evaluates whether a user is authorized for a resource/action";
    if (operation.Parameters is { Count: > 0 })
    {
        var userParam = operation.Parameters.FirstOrDefault(p => p.Name == "userId");
        if (userParam is not null)
        {
            userParam.Description = "User identifier";
        }

        var resourceParam = operation.Parameters.FirstOrDefault(p => p.Name == "resource");
        if (resourceParam is not null)
        {
            resourceParam.Description = "Resource identifier";
        }

        var actionParam = operation.Parameters.FirstOrDefault(p => p.Name == "action");
        if (actionParam is not null)
        {
            actionParam.Description = "Action identifier";
        }
    }
    return operation;
});

app.MapGet("/api/identifier/flags/{flagKey}/evaluate", async (
    string flagKey,
    [FromQuery] Guid? orgId,
    [FromQuery] Guid? userId,
    [FromQuery(Name = "groupIds")] Guid[]? groupIds,
    IdentifierDbContext dbContext,
    IFeatureFlagProvider featureFlagProvider,
    CancellationToken cancellationToken) =>
{
    var flagId = await dbContext.FeatureFlags.AsNoTracking().Where(f => f.Key == flagKey).Select(f => f.Id).FirstOrDefaultAsync(cancellationToken);
    if (flagId == Guid.Empty)
    {
        return Results.NotFound();
    }

    var evaluation = await featureFlagProvider.EvaluateAsync(
        flagId,
        orgId,
        userId,
        groupIds ?? Array.Empty<Guid>(),
        cancellationToken);

    var enabled = await featureFlagProvider.IsEnabledAsync(flagKey, orgId, userId, groupIds ?? Array.Empty<Guid>(), cancellationToken);
    return Results.Json(new { flagKey, variation = evaluation, enabled });
})
.WithName("EvaluateFlag")
.WithOpenApi(operation =>
{
    operation.Summary = "Evaluates a feature flag considering org/group/user overrides";
    return operation;
});

app.MapGet("/api/identifier/license/has-feature/{featureKey}", async (
    string featureKey,
    [FromQuery] Guid orgId,
    ILicenseService licenseService,
    CancellationToken cancellationToken) =>
{
    var evaluation = await licenseService.EvaluateAsync(orgId, featureKey, cancellationToken);
    return Results.Json(new
    {
        organizationId = orgId,
        featureKey,
        hasLicense = evaluation.HasLicense,
        included = evaluation.FeatureIncluded,
        withinQuota = evaluation.WithinQuota,
        evaluation.Reason,
        evaluation.RemainingQuota
    });
})
.WithName("HasFeature")
.WithOpenApi(operation =>
{
    operation.Summary = "Checks whether an organization can access a feature";
    return operation;
});

app.MapPost("/api/identifier/seed", async (
    IdentifierSeeder seeder,
    IConfiguration config,
    CancellationToken cancellationToken) =>
{
    var enabled = config.GetValue<bool?>("Identifier:Seed:Enabled") ??
                  string.Equals(Environment.GetEnvironmentVariable("IDENTIFIER__SEED_ENABLED"), "true", StringComparison.OrdinalIgnoreCase);
    if (!enabled)
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    await seeder.SeedAsync(cancellationToken);
    return Results.Ok(new { status = "seeded" });
})
.WithName("Seed")
.WithOpenApi(operation =>
{
    operation.Summary = "Seeds baseline data. Protected by configuration flag.";
    return operation;
});

app.MapGet("/api/identifier/flags/{flagKey}", async (
    string flagKey,
    IdentifierDbContext dbContext,
    IFeatureFlagProvider featureFlagProvider,
    CancellationToken cancellationToken) =>
{
    var flagExists = await dbContext.FeatureFlags.AsNoTracking().AnyAsync(f => f.Key == flagKey, cancellationToken);
    if (!flagExists)
    {
        return Results.NotFound();
    }

    var enabled = await featureFlagProvider.IsEnabledAsync(flagKey, null, null, Array.Empty<Guid>(), cancellationToken);
    return Results.Json(new { flagKey, enabled });
})
.WithName("FlagStatus");

app.MapGet("/", () => Results.Ok(new { service = "identifier", status = "ready" }));

app.Run();

public partial class Program;
