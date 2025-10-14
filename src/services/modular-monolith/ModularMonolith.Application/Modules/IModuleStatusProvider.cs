using ModularMonolith.Domain.Modules;

namespace ModularMonolith.Application.Modules;

public interface IModuleStatusProvider
{
    Task<IReadOnlyCollection<ModuleStatus>> GetStatusesAsync(CancellationToken cancellationToken);
}
