using Core.Types.Dtos;
using CoreWeb.Api.Hubs;
using CoreWeb.Api.I18n;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;

namespace CoreWeb.Api.Controllers;

[ApiController]
[Route("api/i18n")]
public sealed class I18nController : ControllerBase
{
    private readonly II18nResourceProvider _provider;
    private readonly IHubContext<UiHub, IUiClient> _hubContext;

    public I18nController(II18nResourceProvider provider, IHubContext<UiHub, IUiClient> hubContext)
    {
        _provider = provider;
        _hubContext = hubContext;
    }

    [HttpGet]
    public async Task<ActionResult<IDictionary<string, object?>>> Get([FromQuery] string lang, [FromQuery] string ns, CancellationToken cancellationToken)
    {
        var resource = await _provider.GetAsync(lang, ns, cancellationToken);
        Response.Headers["ETag"] = resource.ETag ?? string.Empty;
        return Ok(resource.Resources);
    }

    [Authorize(Policy = "flags:write")]
    [HttpPost]
    public async Task<IActionResult> Refresh([FromBody] I18nResourceDto resource, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.All.I18nUpdated(resource);
        return Accepted();
    }
}
