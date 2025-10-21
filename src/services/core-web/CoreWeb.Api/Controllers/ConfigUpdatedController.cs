using Core.Types.Dtos;
using CoreWeb.Api.Features.Flags;
using Dapr;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace CoreWeb.Api.Controllers;

[ApiController]
[Route("dapr/config")]
public sealed class ConfigUpdatedController : ControllerBase
{
    private readonly IFeatureFlagService _flags;

    public ConfigUpdatedController(IFeatureFlagService flags)
    {
        _flags = flags;
    }

    [Topic("pubsub", "config.updated")]
    [HttpPost("updated")]
    public async Task<IActionResult> OnConfigUpdated([FromBody] IDictionary<string, FlagValue> payload, CancellationToken cancellationToken)
    {
        foreach (var pair in payload)
        {
            await _flags.SetAsync(pair.Key, pair.Value, cancellationToken);
        }

        return Ok();
    }
}
