using System.Security.Claims;
using Identifier.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Identifier.Api.Services;

public class HttpContextIdentityProvider : IIdentityProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextIdentityProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? GetUserId()
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        return TryGetGuid(principal, ClaimTypes.NameIdentifier) ?? TryGetGuid(principal, "sub");
    }

    public Guid? GetOrganizationId()
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        return TryGetGuid(principal, "org_id") ?? TryGetGuid(principal, "tenant_id");
    }

    private static Guid? TryGetGuid(ClaimsPrincipal? principal, string claimType)
    {
        var value = principal?.FindFirstValue(claimType);
        return Guid.TryParse(value, out var guid) ? guid : null;
    }
}
