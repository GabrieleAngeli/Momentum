using Core.Types.Dtos;
using CoreWeb.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CoreWeb.Api.Features.Flags;

public sealed class SignalRFlagChangeNotifier : IFlagChangeNotifier
{
    private readonly IHubContext<UiHub, IUiClient> _hubContext;

    public SignalRFlagChangeNotifier(IHubContext<UiHub, IUiClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyAsync(FlagsDelta delta, CancellationToken cancellationToken = default)
        => _hubContext.Clients.All.FlagsUpdated(delta);
}
