using System.ComponentModel.DataAnnotations;
using ModularMonolith.Domain.Modules;

namespace ModularMonolith.Infrastructure.Options;

public sealed class MonolithOptions
{
    [Required]
    public ICollection<ModuleOption> Modules { get; init; } = new List<ModuleOption>();

    public IReadOnlyCollection<ModuleRegistration> ToRegistrations()
        => Modules
            .Select(module => new ModuleRegistration(
                module.Name!,
                module.Description!,
                module.AppId!,
                module.HealthEndpoint ?? "healthz"))
            .ToArray();
}

public sealed class ModuleOption
{
    [Required]
    public string? Name { get; init; }

    [Required]
    public string? Description { get; init; }

    [Required]
    public string? AppId { get; init; }

    public string? HealthEndpoint { get; init; }
}
