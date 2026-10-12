using System.Net;

namespace Jex.IntegrationTests.Endpoints;

/// <summary>Health endpoints keep real HTTP status codes (probe semantics), never the envelope.</summary>
public sealed class HealthEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Theory]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    [InlineData("/health/live")]
    public async Task HealthEndpoints_ReturnHealthy(string path)
    {
        var response = await factory.CreateClient().GetAsync(path);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe("Healthy");
    }

    [Fact]
    public async Task HealthEndpoints_DoNotRequireAuthentication()
    {
        var response = await factory.CreateClient().GetAsync("/health/live");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
