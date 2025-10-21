using Core.Types.Dtos;
using CoreWeb.Api.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Linq;
using System.Collections.Generic;

namespace CoreWeb.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IPasswordSignInManager _passwordSignInManager;
    private readonly IJwtIssuer _jwtIssuer;
    private readonly ISessionStore _sessionStore;
    private readonly IMfaProvider _mfaProvider;
    private readonly IExternalIdentityProvider _externalIdentityProvider;

    public AuthController(
        IPasswordSignInManager passwordSignInManager,
        IJwtIssuer jwtIssuer,
        ISessionStore sessionStore,
        IMfaProvider mfaProvider,
        IExternalIdentityProvider externalIdentityProvider)
    {
        _passwordSignInManager = passwordSignInManager;
        _jwtIssuer = jwtIssuer;
        _sessionStore = sessionStore;
        _mfaProvider = mfaProvider;
        _externalIdentityProvider = externalIdentityProvider;
    }

    [HttpGet("me")]
    public ActionResult<AuthMeResponse> Me()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Ok(new AuthMeResponse
            {
                IsAuthenticated = false,
                RequiresMfa = false,
                User = new UserPrincipalDto
                {
                    Id = string.Empty,
                    Email = string.Empty,
                    DisplayName = "Guest"
                }
            });
        }

        var user = new UserPrincipalDto
        {
            Id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
            Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            DisplayName = User.Identity?.Name ?? string.Empty,
            TenantId = User.FindFirstValue("tenant"),
            Roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToArray(),
            Permissions = User.FindAll("permission").Select(r => r.Value).ToArray(),
            Claims = User.Claims.ToDictionary(c => c.Type, c => c.Value)
        };

        return Ok(new AuthMeResponse
        {
            IsAuthenticated = true,
            RequiresMfa = false,
            User = user
        });
    }

    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _passwordSignInManager.PasswordSignInAsync(request.Username, request.Password, cancellationToken);
        if (!result.Succeeded)
        {
            if (result.RequiresMfa && result.User is not null)
            {
                return Ok(new LoginResponse
                {
                    Me = new AuthMeResponse
                    {
                        IsAuthenticated = false,
                        RequiresMfa = true,
                        User = result.User
                    }
                });
            }

            return Unauthorized(result.Error);
        }

        var principal = CreatePrincipal(result.User!);
        var sessionId = await _sessionStore.CreateAsync(result.User!, cancellationToken);
        var claimsIdentity = (ClaimsIdentity)principal.Identity!;
        claimsIdentity.AddClaim(new Claim("sid", sessionId));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        var token = _jwtIssuer.IssueToken(result.User!, TimeSpan.FromHours(8));

        return Ok(new LoginResponse
        {
            Me = new AuthMeResponse
            {
                IsAuthenticated = true,
                RequiresMfa = false,
                User = result.User!
            },
            JwtToken = token
        });
    }

    [HttpPost("logout")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var sessionId = User.FindFirstValue("sid");
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                await _sessionStore.RemoveAsync(sessionId, cancellationToken);
            }
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [HttpPost("mfa/verify")]
    public async Task<IActionResult> VerifyMfa([FromBody] MfaVerifyRequest request)
    {
        var userId = Request.Headers["X-User-Id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest("missing_user_id");
        }

        var result = await _mfaProvider.VerifyAsync(userId, request.Code);
        return result ? Ok() : Unauthorized();
    }

    [HttpGet("challenge/{provider}")]
    public Task<AuthResult> ChallengeExternal([FromRoute] string provider, CancellationToken cancellationToken)
        => _externalIdentityProvider.ChallengeAsync(HttpContext, provider, cancellationToken);

    [HttpGet("callback/{provider}")]
    public async Task<ActionResult<LoginResponse>> ExternalCallback([FromRoute] string provider, CancellationToken cancellationToken)
    {
        var user = await _externalIdentityProvider.ValidateCallbackAsync(HttpContext, provider, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var principal = CreatePrincipal(user);
        var sessionId = await _sessionStore.CreateAsync(user, cancellationToken);
        ((ClaimsIdentity)principal.Identity!).AddClaim(new Claim("sid", sessionId));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        var token = _jwtIssuer.IssueToken(user, TimeSpan.FromHours(8));

        return Ok(new LoginResponse
        {
            Me = new AuthMeResponse
            {
                IsAuthenticated = true,
                RequiresMfa = false,
                User = user
            },
            JwtToken = token
        });
    }

    private static ClaimsPrincipal CreatePrincipal(UserPrincipalDto user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Email, user.Email)
        };

        if (!string.IsNullOrEmpty(user.TenantId))
        {
            claims.Add(new Claim("tenant", user.TenantId));
        }

        claims.AddRange(user.Roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(user.Permissions.Select(p => new Claim("permission", p)));

        foreach (var pair in user.Claims)
        {
            claims.Add(new Claim(pair.Key, pair.Value));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }
}
