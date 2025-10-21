using Core.Types.Dtos;
using CoreWeb.Api.Auth;
using CoreWeb.Api.Controllers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using Xunit;

namespace CoreWeb.Api.Tests;

public class AuthControllerTests
{
    [Fact]
    public async Task Login_returns_token_when_credentials_valid()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
        var serviceProvider = services.BuildServiceProvider();

        var controller = new AuthController(
            new DefaultPasswordSignInManager(new StubMfaProvider()),
            new DefaultJwtIssuer(new ConfigurationBuilder().Build()),
            new InMemorySessionStore(),
            new StubMfaProvider(),
            new StubOidcIdentityProvider());

        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider
            }
        };

        var result = await controller.Login(new LoginRequest
        {
            Username = "user",
            Password = "P@ssw0rd!"
        }, CancellationToken.None);

        var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var payload = Assert.IsType<LoginResponse>(okResult.Value);
        Assert.True(payload.Me.IsAuthenticated);
        Assert.False(string.IsNullOrEmpty(payload.JwtToken));
    }
}
