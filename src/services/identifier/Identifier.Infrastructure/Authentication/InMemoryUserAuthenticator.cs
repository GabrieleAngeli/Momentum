using System.Security.Cryptography;
using System.Text;
using System.Linq;
using Identifier.Application.Authentication;
using Identifier.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Identifier.Infrastructure.Authentication;

public sealed class InMemoryUserAuthenticator : IUserAuthenticator
{
    private readonly ILogger<InMemoryUserAuthenticator> _logger;
    private readonly IDictionary<string, (TenantUser user, string passwordHash)> _users;

    public InMemoryUserAuthenticator(ILogger<InMemoryUserAuthenticator> logger)
    {
        _logger = logger;
        _users = SeedUsers();
    }

    public Task<AuthenticationResult?> AuthenticateAsync(AuthenticateUserCommand command, CancellationToken cancellationToken)
    {
        if (_users.TryGetValue(command.Email, out var entry) && VerifyPassword(command.Password, entry.passwordHash))
        {
            _logger.LogInformation("User {Email} authenticated", command.Email);
            var token = GenerateJwt(entry.user);
            return Task.FromResult<AuthenticationResult?>(new AuthenticationResult(entry.user.Id, entry.user.DisplayName, token, entry.user.Roles.ToArray()));
        }

        _logger.LogWarning("Failed authentication for {Email}", command.Email);
        return Task.FromResult<AuthenticationResult?>(null);
    }

    private static IDictionary<string, (TenantUser user, string passwordHash)> SeedUsers()
    {
        var user = new TenantUser(Guid.Parse("0ddcaa5e-8e91-4b7c-8ad1-9155491d68a5"), "demo@momentum.dev", "Demo Admin", new[] { "admin" }, new[] { "feature:alerting" }, DateTimeOffset.UtcNow.AddYears(1));
        return new Dictionary<string, (TenantUser user, string passwordHash)>(StringComparer.OrdinalIgnoreCase)
        {
            [user.Email] = (user, HashPassword("P@ssw0rd!"))
        };
    }

    private static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    private static bool VerifyPassword(string password, string hash) => HashPassword(password) == hash;

    private static string GenerateJwt(TenantUser user)
    {
        var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes("super-secret-key-super-secret-key"));
        var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(securityKey, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var token = handler.CreateJwtSecurityToken(subject: new System.Security.Claims.ClaimsIdentity(new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, user.Email),
            new System.Security.Claims.Claim("display_name", user.DisplayName)
        }.Concat(user.Roles.Select(role => new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role)))),
            notBefore: DateTime.UtcNow,
            expires: user.LicenseValidUntil.UtcDateTime,
            signingCredentials: credentials,
            issuer: "momentum.identifier",
            audience: "momentum.clients");
        return handler.WriteToken(token);
    }
}
