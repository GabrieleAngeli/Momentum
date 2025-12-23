using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Linq;
using Core.Types.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CoreWeb.Api.Auth;

public sealed class DefaultPasswordSignInManager : IPasswordSignInManager
{
    private readonly IDictionary<string, (string Password, UserPrincipalDto User)> _users;

    public DefaultPasswordSignInManager(IMfaProvider _)
    {
        _users = new Dictionary<string, (string, UserPrincipalDto)>(StringComparer.OrdinalIgnoreCase)
        {
            ["admin"] = ("P@ssw0rd!", new UserPrincipalDto
            {
                Id = "1",
                Email = "admin@example.com",
                DisplayName = "Admin User",
                Roles = new[] { "admin" },
                Permissions = new[] { "flags:write", "feature-a:view" },
                Claims = new Dictionary<string, string>
                {
                    ["tenant"] = "tenant-default"
                }
            }),
            ["user"] = ("P@ssw0rd!", new UserPrincipalDto
            {
                Id = "2",
                Email = "user@example.com",
                DisplayName = "Standard User",
                Roles = new[] { "user" },
                Permissions = new[] { "feature-a:view" },
                Claims = new Dictionary<string, string>
                {
                    ["tenant"] = "tenant-default"
                }
            })
        };
    }

    public Task<AuthResult> PasswordSignInAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        if (!_users.TryGetValue(username, out var tuple))
        {
            return Task.FromResult(AuthResult.Failed("invalid_username"));
        }

        if (!string.Equals(tuple.Password, password, StringComparison.Ordinal))
        {
            return Task.FromResult(AuthResult.Failed("invalid_password"));
        }

        var requiresMfa = string.Equals(tuple.User.Email, "admin@example.com", StringComparison.OrdinalIgnoreCase);
        if (requiresMfa)
        {
            return Task.FromResult(AuthResult.MfaRequires(tuple.User));
        }

        return Task.FromResult(AuthResult.Success(tuple.User));
    }
}

public sealed class InMemorySessionStore : ISessionStore
{
    private readonly ConcurrentDictionary<string, UserPrincipalDto> _sessions = new();

    public Task<string> CreateAsync(UserPrincipalDto user, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid().ToString("N");
        _sessions[id] = user;
        return Task.FromResult(id);
    }

    public Task<bool> ExistsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var sessionId = principal.FindFirstValue("sid");
        if (sessionId is null)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(_sessions.ContainsKey(sessionId));
    }

    public Task RemoveAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _sessions.TryRemove(sessionId, out _);
        return Task.CompletedTask;
    }
}

public sealed class DefaultJwtIssuer : IJwtIssuer
{
    private readonly IConfiguration _configuration;

    public DefaultJwtIssuer(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string IssueToken(UserPrincipalDto user, TimeSpan lifetime)
    {
        var signingKey = _configuration["Auth:Jwt:SigningKey"] ?? "development-signing-key-please-change";
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Email, user.Email),
            new("tenant", user.TenantId ?? string.Empty),
        };

        claims.AddRange(user.Roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(user.Permissions.Select(p => new Claim("permission", p)));
        foreach (var pair in user.Claims)
        {
            claims.Add(new Claim(pair.Key, pair.Value));
        }

        var token = new JwtSecurityToken(
            issuer: _configuration["Auth:Jwt:Issuer"],
            audience: _configuration["Auth:Jwt:Audience"],
            claims: claims,
            notBefore: now,
            expires: now.Add(lifetime),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public sealed class StubMfaProvider : IMfaProvider
{
    public Task<bool> VerifyAsync(string userId, string code, CancellationToken cancellationToken = default)
        => Task.FromResult(code == "000000" || code == "123456");
}

public sealed class StubOidcIdentityProvider : IExternalIdentityProvider
{
    public Task<AuthResult> ChallengeAsync(HttpContext context, string providerName, CancellationToken cancellationToken = default)
    {
        context.Response.Headers["X-OIDC-Stub"] = "redirected";
        return Task.FromResult(AuthResult.Failed("oidc_challenge_not_configured"));
    }

    public Task<UserPrincipalDto?> ValidateCallbackAsync(HttpContext context, string providerName, CancellationToken cancellationToken = default)
    {
        var email = context.Request.Query["email"].FirstOrDefault();
        if (string.IsNullOrEmpty(email))
        {
            return Task.FromResult<UserPrincipalDto?>(null);
        }

        return Task.FromResult<UserPrincipalDto?>(new UserPrincipalDto
        {
            Id = Guid.NewGuid().ToString("N"),
            Email = email,
            DisplayName = email,
            Roles = new[] { "oidc" },
            Permissions = new[] { "feature-a:view" }
        });
    }
}
