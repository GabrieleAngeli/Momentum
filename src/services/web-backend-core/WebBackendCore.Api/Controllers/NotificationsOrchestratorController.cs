using Microsoft.AspNetCore.Mvc;
using WebBackendCore.Application.Notifications;

namespace WebBackendCore.Api.Controllers;

[ApiController]
[Route("api/orchestrator/notifications")]
public sealed class NotificationsOrchestratorController : ControllerBase
{
    private readonly NotificationOrchestrator _orchestrator;

    public NotificationsOrchestratorController(NotificationOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    [HttpPost]
    public async Task<IActionResult> Broadcast([FromBody] object payload, CancellationToken cancellationToken)
    {
        await _orchestrator.HandleAsync(payload, cancellationToken);
        return Accepted();
    }
}
