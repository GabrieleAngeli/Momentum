using Core.Types.Dtos;
using CoreWeb.Api.Auth;
using CoreWeb.Api.Features.Flags;
using CoreWeb.Api.Hubs;
using CoreWeb.Api.I18n;
using CoreWeb.Api.Menu;
using CoreWeb.Api.Manifest;
using Dapr.Client;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
var configuration = builder.Configuration;

services.AddControllers();
services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
services.AddSignalR();
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();

services.AddHttpContextAccessor();
services.AddCors(options =>
{
    options.AddPolicy("shell", policy =>
    {
        var allowedOrigin = configuration["Shell:Origin"] ?? "http://localhost:4200";
        policy.WithOrigins(allowedOrigin)
            .AllowAnyHeader()
            .AllowCredentials()
            .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS");
    });
});

services.AddSingleton<DaprClient>(_ => new DaprClientBuilder().Build());

services.AddSingleton<IFeatureFlagStore, InMemoryFeatureFlagStore>();
services.AddSingleton<IFeatureFlagService, FeatureFlagService>();
services.AddSingleton<IFlagChangeNotifier, SignalRFlagChangeNotifier>();

services.AddSingleton<IManifestProvider, StaticManifestProvider>();
services.AddSingleton<IMenuProvider, DefaultMenuProvider>();
services.AddSingleton<II18nResourceProvider, JsonFileI18nResourceProvider>();
services.AddSingleton<IClock, SystemClock>();
services.AddSingleton<IJwtIssuer, DefaultJwtIssuer>();
services.AddSingleton<ISessionStore, InMemorySessionStore>();
services.AddScoped<IPasswordSignInManager, DefaultPasswordSignInManager>();
services.AddSingleton<IMfaProvider, StubMfaProvider>();
services.AddSingleton<IExternalIdentityProvider, StubOidcIdentityProvider>();
services.AddScoped<AuthContextBuilder>();
services.AddHostedService<FeatureFlagSeeder>();

services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("core-web"))
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddHttpClientInstrumentation();
        tracing.AddSource("CoreWeb");
    })
    .StartWithHost();

var signingKey = configuration["Auth:Jwt:SigningKey"] ?? "development-signing-key-please-change";
var signingKeyBytes = Encoding.UTF8.GetBytes(signingKey);

services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        IssuerSigningKey = new SymmetricSecurityKey(signingKeyBytes),
        ValidateIssuerSigningKey = true,
    };
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.Cookie.Name = "coreweb.session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.SlidingExpiration = true;
    options.Events.OnValidatePrincipal = async ctx =>
    {
        var sessionStore = ctx.HttpContext.RequestServices.GetRequiredService<ISessionStore>();
        if (!await sessionStore.ExistsAsync(ctx.Principal))
        {
            ctx.RejectPrincipal();
        }
    };
});

services.AddAuthorization(options =>
{
    options.AddPolicy("flags:write", policy =>
    {
        policy.RequireAssertion(ctx => ctx.User.HasClaim("permission", "flags:write"));
    });
});

services.AddHealthChecks();

var app = builder.Build();

app.UseCors("shell");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<UiHub>("/hubs/ui");
app.MapHealthChecks("/health");
app.MapGet("/api/ping", () => Results.Ok(new { status = "ok" }));
app.MapGet("/api/csrf", (HttpContext context, IAntiforgery antiforgery) =>
{
    var tokens = antiforgery.GetAndStoreTokens(context);
    context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!, new CookieOptions
    {
        HttpOnly = false,
        SameSite = SameSiteMode.Strict,
        Secure = context.Request.IsHttps
    });
    return Results.NoContent();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();

public sealed class AuthContextBuilder
{
    private readonly IHttpContextAccessor _accessor;

    public AuthContextBuilder(IHttpContextAccessor accessor) => _accessor = accessor;

    public EvaluationContext BuildContext()
    {
        var httpContext = _accessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            return new EvaluationContext();
        }

        var tenant = httpContext.User.FindFirstValue("tenant") ?? httpContext.User.FindFirstValue("tenant_id");
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? httpContext.User.FindFirstValue("sub");
        var roles = httpContext.User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToArray();

        return new EvaluationContext
        {
            TenantId = tenant,
            UserId = userId,
            Roles = roles
        };
    }
}

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
