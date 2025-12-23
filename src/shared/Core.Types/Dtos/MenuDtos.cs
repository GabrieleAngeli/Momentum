namespace Core.Types.Dtos;

public sealed record MenuEntryDto
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public required string Route { get; init; }
    public IReadOnlyList<string> RequiredFlags { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> RequiredPermissions { get; init; } = Array.Empty<string>();
    public IReadOnlyList<MenuEntryDto> Children { get; init; } = Array.Empty<MenuEntryDto>();
}
