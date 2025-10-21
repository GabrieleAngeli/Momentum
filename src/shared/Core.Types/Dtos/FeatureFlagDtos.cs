namespace Core.Types.Dtos;

public sealed record FlagValue
{
    public required string Key { get; init; }
    public required string Type { get; init; }
    public required object? Value { get; init; }
    public required string Scope { get; init; }
    public string? ScopeReference { get; init; }
}

public sealed record FlagsEnvelope
{
    public required IDictionary<string, FlagValue> Flags { get; init; }
    public string? ETag { get; init; }
}

public sealed record FlagsDelta
{
    public required IDictionary<string, FlagValue> Updated { get; init; }
    public IReadOnlyList<string> Removed { get; init; } = Array.Empty<string>();
}

public sealed record EvaluationContext
{
    public string? TenantId { get; init; }
    public string? UserId { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
}
