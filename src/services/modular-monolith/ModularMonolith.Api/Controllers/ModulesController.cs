using Microsoft.AspNetCore.Mvc;
using ModularMonolith.Application.Modules;

namespace ModularMonolith.Api.Controllers;

[ApiController]
[Route("api/modules")]
public sealed class ModulesController : ControllerBase
{
    private readonly GetModuleStatusQuery _query;

    public ModulesController(GetModuleStatusQuery query)
    {
        _query = query;
    }

    [HttpGet("status")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ModuleStatusResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ModuleStatusResponse>>> GetStatusAsync(CancellationToken cancellationToken)
    {
        var statuses = await _query.ExecuteAsync(cancellationToken);
        var response = statuses.Select(ModuleStatusResponse.FromDomain).ToArray();
        return Ok(response);
    }
}

public sealed record ModuleStatusResponse(string Name, string Description, bool Healthy, string Details)
{
    public static ModuleStatusResponse FromDomain(ModuleStatus status) => new(
        status.Registration.Name,
        status.Registration.Description,
        status.Healthy,
        status.Details);
}
