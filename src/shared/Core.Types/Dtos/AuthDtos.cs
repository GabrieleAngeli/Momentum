namespace Core.Types.Dtos;

public sealed record UserPrincipalDto
{
    public required string Id { get; init; }
    public required string Email { get; init; }
    public required string DisplayName { get; init; }
    public string? TenantId { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();
    public IDictionary<string, string> Claims { get; init; } = new Dictionary<string, string>();
}

public sealed record AuthMeResponse
{
    public required UserPrincipalDto User { get; init; }
    public bool IsAuthenticated { get; init; }
    public bool RequiresMfa { get; init; }
}

public sealed record LoginRequest
{
    public required string Username { get; init; }
    public required string Password { get; init; }
    public string? MfaCode { get; init; }
}

public sealed record LoginResponse
{
    public required AuthMeResponse Me { get; init; }
    public string? JwtToken { get; init; }
}

public sealed record MfaVerifyRequest
{
    public required string Code { get; init; }
}
