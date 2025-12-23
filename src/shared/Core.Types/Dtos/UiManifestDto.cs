namespace Core.Types.Dtos;

public sealed record UiManifestDto
{
    public required IReadOnlyList<RemoteModuleDescriptor> Remotes { get; init; }
    public required IReadOnlyDictionary<string, string> Shared { get; init; }
}

public sealed record RemoteModuleDescriptor
{
    public required string Id { get; init; }
    public required string Url { get; init; }
    public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Flags { get; init; } = Array.Empty<string>();
    public string? Semver { get; init; }
}
