using Core.Types.Dtos;
using CoreWeb.Api.Hubs;
using CoreWeb.Api.Manifest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace CoreWeb.Api.Controllers;

[ApiController]
[Route("api/ui/manifest")]
public sealed class UiManifestController : ControllerBase
{
    private readonly IManifestProvider _provider;
    private readonly IHubContext<UiHub, IUiClient> _hubContext;

    public UiManifestController(IManifestProvider provider, IHubContext<UiHub, IUiClient> hubContext)
    {
        _provider = provider;
        _hubContext = hubContext;
    }

    [HttpGet]
    public async Task<ActionResult<UiManifestDto>> Get(CancellationToken cancellationToken)
    {
        var manifest = await _provider.GetAsync(cancellationToken);
        return Ok(manifest);
    }

    [Authorize(Policy = "flags:write")]
    [HttpPost]
    public async Task<IActionResult> Update([FromBody] UiManifestDto manifest, CancellationToken cancellationToken)
    {
        await _provider.UpdateAsync(manifest, cancellationToken);
        await _hubContext.Clients.All.ManifestUpdated(manifest);
        return Accepted();
    }
}
