using Microsoft.AspNetCore.Mvc;
using Notifier.Application.Dispatching;

namespace Notifier.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class NotificationsController : ControllerBase
{
    private readonly DispatchNotificationHandler _handler;

    public NotificationsController(DispatchNotificationHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    public async Task<IActionResult> Publish([FromBody] DispatchNotificationCommand command, CancellationToken cancellationToken)
    {
        await _handler.HandleAsync(command, cancellationToken);
        return Accepted();
    }
}
