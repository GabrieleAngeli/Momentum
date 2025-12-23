using Core.Types.Dtos;
using System.Collections.Generic;

namespace CoreWeb.Api.Manifest;

public interface IManifestProvider
{
    Task<UiManifestDto> GetAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(UiManifestDto manifest, CancellationToken cancellationToken = default);
}

public sealed class StaticManifestProvider : IManifestProvider
{
    private UiManifestDto _manifest = new()
    {
        Remotes = new List<RemoteModuleDescriptor>
        {
            new()
            {
                Id = "feature-a",
                Url = "local:feature-a",
                Permissions = new[] { "feature-a:view" },
                Flags = new[] { "featureA.enabled" },
                Semver = "^19.0.0"
            }
        },
        Shared = new Dictionary<string, string>
        {
            ["@angular/core"] = "19.x",
            ["@angular/router"] = "19.x"
        }
    };

    public Task<UiManifestDto> GetAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_manifest);

    public Task UpdateAsync(UiManifestDto manifest, CancellationToken cancellationToken = default)
    {
        _manifest = manifest;
        return Task.CompletedTask;
    }
}
