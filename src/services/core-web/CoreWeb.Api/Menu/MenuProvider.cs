using Core.Types.Dtos;
using CoreWeb.Api.Features.Flags;
using System.Collections.Generic;
using System.Linq;

namespace CoreWeb.Api.Menu;

public interface IMenuProvider
{
    Task<IEnumerable<MenuEntryDto>> GetMenuAsync(EvaluationContext context, CancellationToken cancellationToken);
}

public sealed class DefaultMenuProvider : IMenuProvider
{
    private readonly IFeatureFlagService _flags;

    public DefaultMenuProvider(IFeatureFlagService flags)
    {
        _flags = flags;
    }

    public async Task<IEnumerable<MenuEntryDto>> GetMenuAsync(EvaluationContext context, CancellationToken cancellationToken)
    {
        var baseMenu = new List<MenuEntryDto>
        {
            new()
            {
                Id = "feature-a",
                Label = "featureA.title",
                Route = "/feature-a",
                RequiredFlags = new[] { "featureA.enabled" },
                RequiredPermissions = new[] { "feature-a:view" }
            }
        };

        var filtered = new List<MenuEntryDto>();
        foreach (var item in baseMenu)
        {
            var allowed = true;
            foreach (var flag in item.RequiredFlags)
            {
                if (!await _flags.GetBooleanAsync(flag, context, false, cancellationToken))
                {
                    allowed = false;
                    break;
                }
            }

            if (!allowed)
            {
                continue;
            }

            if (item.RequiredPermissions.Any() && !context.Roles.Any(r => r.Equals("admin", StringComparison.OrdinalIgnoreCase)))
            {
                // simple permission check stub
                allowed = false;
            }

            if (allowed)
            {
                filtered.Add(item);
            }
        }

        return filtered;
    }
}
