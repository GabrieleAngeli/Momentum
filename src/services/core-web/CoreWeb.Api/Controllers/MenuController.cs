using Core.Types.Dtos;
using CoreWeb.Api.Menu;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace CoreWeb.Api.Controllers;

[ApiController]
[Route("api/ui/menu")]
public sealed class MenuController : ControllerBase
{
    private readonly IMenuProvider _provider;
    private readonly AuthContextBuilder _authContextBuilder;

    public MenuController(IMenuProvider provider, AuthContextBuilder authContextBuilder)
    {
        _provider = provider;
        _authContextBuilder = authContextBuilder;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<MenuEntryDto>>> Get(CancellationToken cancellationToken)
    {
        var ctx = _authContextBuilder.BuildContext();
        var menu = await _provider.GetMenuAsync(ctx, cancellationToken);
        return Ok(menu);
    }
}
