using ModularMonolith.Application.Modules;
using ModularMonolith.Infrastructure.Modules;
using ModularMonolith.Infrastructure.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDaprClient();

builder.Services
    .AddOptions<MonolithOptions>()
    .Bind(builder.Configuration.GetSection("Monolith"))
    .ValidateDataAnnotations()
    .Validate(options => options.Modules.Count > 0, "At least one module must be configured")
    .ValidateOnStart();

builder.Services.AddSingleton<IModuleStatusProvider, DaprModuleStatusProvider>();
builder.Services.AddSingleton<GetModuleStatusQuery>();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("modular-monolith-api"))
    .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation().AddPrometheusExporter())
    .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation());

builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHealthChecks("/healthz");
app.MapGet("/", () => "Modular monolith ready");
app.MapGet("/metrics", () => Results.Ok("Prometheus metrics"));

app.Run();

public partial class Program
{
    public static string Name => "ModularMonolith";
}
