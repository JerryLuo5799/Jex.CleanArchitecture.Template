using System.Net;
using JexSolution.Application.Common;
using static JexSolution.IntegrationTests.ApiClientExtensions;

namespace JexSolution.IntegrationTests.Endpoints;

/// <summary>Scaffolded tests: extend with real payloads and assertions once the use cases are implemented.</summary>
public sealed class JexFeatureEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task CreateJexEntity_Authenticated_ReturnsSuccess()
    {
        var response = await factory.CreateAuthenticatedClient().PostAsync("/api/TPLROUTE", JsonBody(new { }));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.ReadEnvelopeAsync()).Code().ShouldBe(ErrorCodes.Success);
    }

    [Fact]
    public async Task GetJexEntityById_Anonymous_Returns4010()
    {
        var response = await factory.CreateClient().GetAsync("/api/TPLROUTE/1");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.ReadEnvelopeAsync()).Code().ShouldBe(ErrorCodes.Unauthenticated);
    }
}
