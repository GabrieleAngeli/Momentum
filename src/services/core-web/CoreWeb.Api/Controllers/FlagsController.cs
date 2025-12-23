using Core.Types.Dtos;
using CoreWeb.Api.Features.Flags;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreWeb.Api.Controllers;

[ApiController]
[Route("api/flags")]
public sealed class FlagsController : ControllerBase
{
    private readonly IFeatureFlagService _service;
    private readonly AuthContextBuilder _authContextBuilder;

    public FlagsController(IFeatureFlagService service, AuthContextBuilder authContextBuilder)
    {
        _service = service;
        _authContextBuilder = authContextBuilder;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IDictionary<string, FlagValue>>> Get(CancellationToken cancellationToken)
    {
        var ctx = _authContextBuilder.BuildContext();
        var snapshot = await _service.GetSnapshotAsync(ctx, cancellationToken);
        var etag = GenerateEtag(snapshot);
        Response.Headers["ETag"] = etag;
        return Ok(snapshot);
    }

    [Authorize(Policy = "flags:write")]
    [HttpPost("{key}")]
    public async Task<IActionResult> SetFlag(string key, [FromBody] FlagValue value, CancellationToken cancellationToken)
    {
        await _service.SetAsync(key, value, cancellationToken);
        return Accepted();
    }

    private static string GenerateEtag(IDictionary<string, FlagValue> payload)
    {
        var data = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(payload);
        var hash = System.Security.Cryptography.SHA256.HashData(data);
        return $"\"{Convert.ToHexString(hash)}\"";
    }
}
