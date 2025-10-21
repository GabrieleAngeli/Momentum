using Core.Types.Dtos;
using System.Security.Claims;

namespace CoreWeb.Api.Auth;

public interface IExternalIdentityProvider
{
    Task<AuthResult> ChallengeAsync(HttpContext context, string providerName, CancellationToken cancellationToken = default);
    Task<UserPrincipalDto?> ValidateCallbackAsync(HttpContext context, string providerName, CancellationToken cancellationToken = default);
}

public interface IMfaProvider
{
    Task<bool> VerifyAsync(string userId, string code, CancellationToken cancellationToken = default);
}

public interface IPasswordSignInManager
{
    Task<AuthResult> PasswordSignInAsync(string username, string password, CancellationToken cancellationToken = default);
}

public interface ISessionStore
{
    Task<string> CreateAsync(UserPrincipalDto user, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);
    Task RemoveAsync(string sessionId, CancellationToken cancellationToken = default);
}

public interface IJwtIssuer
{
    string IssueToken(UserPrincipalDto user, TimeSpan lifetime);
}

public sealed record AuthResult(bool Succeeded, bool RequiresMfa, UserPrincipalDto? User, string? Error = null)
{
    public static AuthResult Success(UserPrincipalDto user) => new(true, false, user);
    public static AuthResult RequiresMfa(UserPrincipalDto user) => new(false, true, user);
    public static AuthResult Failed(string error) => new(false, false, null, error);
}
