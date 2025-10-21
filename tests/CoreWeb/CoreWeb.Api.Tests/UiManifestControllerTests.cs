using CoreWeb.Api.Controllers;
using CoreWeb.Api.Hubs;
using CoreWeb.Api.Manifest;
using Microsoft.AspNetCore.SignalR;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CoreWeb.Api.Tests;

public class UiManifestControllerTests
{
    [Fact]
    public async Task Returns_manifest_from_provider()
    {
        var manifest = await new StaticManifestProvider().GetAsync();
        var providerMock = new Mock<IManifestProvider>();
        providerMock.Setup(p => p.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(manifest);
        var hubMock = new Mock<IHubContext<UiHub, IUiClient>>();

        var controller = new UiManifestController(providerMock.Object, hubMock.Object);

        var result = await controller.Get(CancellationToken.None);

        var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        Assert.Equal(manifest, ok.Value);
    }
}
