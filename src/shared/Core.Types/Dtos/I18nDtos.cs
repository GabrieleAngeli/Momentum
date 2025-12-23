namespace Core.Types.Dtos;

public sealed record I18nResourceDto
{
    public required string Language { get; init; }
    public required string Namespace { get; init; }
    public required IDictionary<string, object?> Resources { get; init; }
    public string? ETag { get; init; }
}
