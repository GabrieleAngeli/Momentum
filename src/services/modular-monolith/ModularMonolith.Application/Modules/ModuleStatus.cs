using ModularMonolith.Domain.Modules;

namespace ModularMonolith.Application.Modules;

public sealed record ModuleStatus(ModuleRegistration Registration, bool Healthy, string Details);
