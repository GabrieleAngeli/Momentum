namespace ModularMonolith.Application.Modules;

public sealed class GetModuleStatusQuery
{
    private readonly IModuleStatusProvider _provider;

    public GetModuleStatusQuery(IModuleStatusProvider provider)
    {
        _provider = provider;
    }

    public Task<IReadOnlyCollection<ModuleStatus>> ExecuteAsync(CancellationToken cancellationToken = default)
        => _provider.GetStatusesAsync(cancellationToken);
}
