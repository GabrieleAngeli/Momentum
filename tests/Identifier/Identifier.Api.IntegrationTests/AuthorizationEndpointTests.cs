using System.Net.Http.Json;
using FluentAssertions;
using Identifier.Infrastructure.Seeding;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Identifier.Api.IntegrationTests;

public class AuthorizationEndpointTests : IClassFixture<IdentifierApiFactory>
{
    private readonly IdentifierApiFactory _factory;

    public AuthorizationEndpointTests(IdentifierApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Authorize_Returns_Allowed_For_Seeded_Admin()
    {
        var client = _factory.CreateClient();

        using (var scope = _factory.Services.CreateScope())
        {
            var seeder = scope.ServiceProvider.GetRequiredService<IdentifierSeeder>();
            await seeder.SeedAsync();
        }

        var userId = Guid.Parse("08fb1fb2-541d-4720-9f61-89d33bd44ddc");
        var response = await client.GetAsync($"/api/identifier/authorize?userId={userId}&resource=devices&action=manage");
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<AuthorizationResponse>();
        payload.Should().NotBeNull();
        payload!.Allowed.Should().BeTrue();
    }

    [Fact]
    public async Task Flag_Evaluation_Respects_Precedence()
    {
        var client = _factory.CreateClient();

        using (var scope = _factory.Services.CreateScope())
        {
            var seeder = scope.ServiceProvider.GetRequiredService<IdentifierSeeder>();
            await seeder.SeedAsync();
        }

        var orgId = Guid.Parse("3f7b5937-5e63-4d0e-8267-29aef39915af");
        var userId = Guid.Parse("08fb1fb2-541d-4720-9f61-89d33bd44ddc");
        var flagKey = "ui.newDashboard";

        var response = await client.GetAsync($"/api/identifier/flags/{flagKey}/evaluate?orgId={orgId}&userId={userId}");
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<FlagResponse>();
        payload.Should().NotBeNull();
        payload!.Enabled.Should().BeTrue();
        payload.Variation.Should().Be("on");
    }

    private sealed record AuthorizationResponse(bool Allowed, string Reason);
    private sealed record FlagResponse(string FlagKey, string Variation, bool Enabled);
}
