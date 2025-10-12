using Dapr.Client;
using Identifier.Application.Authentication;
using Identifier.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddDaprClient();
builder.Services.AddSingleton<IUserAuthenticator, InMemoryUserAuthenticator>();
builder.Services.AddSingleton<AuthenticateUserHandler>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddAuthorization();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("identifier-api"))
    .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation().AddPrometheusExporter())
    .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation());

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapGrpcService<IdentifierGrpcService>();
app.MapHealthChecks("/healthz");
app.MapGet("/metrics", () => Results.Ok("Prometheus metrics exposed"));
app.MapGet("/", () => "Identifier service ready");

app.Run();

public sealed class IdentifierGrpcService : Identifier.Authentication.V1.Authenticator.AuthenticatorBase
{
    private readonly AuthenticateUserHandler _handler;

    public IdentifierGrpcService(AuthenticateUserHandler handler)
    {
        _handler = handler;
    }

    public override async Task<Identifier.Authentication.V1.AuthenticateReply> Authenticate(Identifier.Authentication.V1.AuthenticateRequest request, Grpc.Core.ServerCallContext context)
    {

        Console.WriteLine($"Received authentication request for {request.Email}"); // TODO da eliminare
        var result = await _handler.HandleAsync(new AuthenticateUserCommand(request.Email, request.Password), context.CancellationToken);
        if (result is null)
        {
            throw new Grpc.Core.RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.Unauthenticated, "Invalid credentials"));
        }

        var reply = new Identifier.Authentication.V1.AuthenticateReply
        {
            UserId = result.UserId.ToString(),
            DisplayName = result.DisplayName,
            Jwt = result.Token
        };
        reply.Roles.AddRange(result.Roles);
        return reply;
    }
}

public partial class Program { }
