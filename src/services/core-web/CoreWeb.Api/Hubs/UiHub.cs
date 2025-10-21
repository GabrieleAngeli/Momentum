using Core.Types.Dtos;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CoreWeb.Api.Hubs;

[Authorize]
public sealed class UiHub : Hub<IUiClient>
{
}

public interface IUiClient
{
    Task FlagsUpdated(FlagsDelta delta);
    Task I18nUpdated(I18nResourceDto payload);
    Task MenuUpdated(IEnumerable<MenuEntryDto> items);
    Task ManifestUpdated(UiManifestDto manifest);
}
