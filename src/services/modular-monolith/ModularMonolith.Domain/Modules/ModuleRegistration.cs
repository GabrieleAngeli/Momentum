namespace ModularMonolith.Domain.Modules;

public sealed record ModuleRegistration(
    string Name,
    string Description,
    string AppId,
    string HealthEndpoint);
