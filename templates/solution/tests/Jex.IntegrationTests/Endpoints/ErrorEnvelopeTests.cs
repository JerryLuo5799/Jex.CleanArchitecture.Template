using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Jex.Application.Common;
using static Jex.IntegrationTests.ApiClientExtensions;

namespace Jex.IntegrationTests.Endpoints;

/// <summary>
/// Response contract: every failure on an API route is HTTP 200 with the envelope, and the business
/// code tells the client what happened. See AGENTS.md §8 for the full table.
/// </summary>
public sealed class ErrorEnvelopeTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task UnknownRoute_Returns4040()
    {
        var response = await factory.CreateClient().GetAsync("/api/does-not-exist");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var envelope = await response.ReadEnvelopeAsync();
        envelope.Code().ShouldBe(ErrorCodes.NotFound);
        envelope.GetProperty("requestId").GetString().ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task WrongMethod_Returns4050()
    {
        var response = await factory.CreateAuthenticatedClient().PatchAsync("/api/users", JsonBody(new { }));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.ReadEnvelopeAsync()).Code().ShouldBe(ErrorCodes.MethodNotAllowed);
    }

    [Fact]
    public async Task MalformedJson_Returns4000()
    {
        var content = new StringContent("{not json", Encoding.UTF8, "application/json");

        var response = await factory.CreateClient().PostAsync("/api/auth/login", content);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.ReadEnvelopeAsync()).Code().ShouldBe(ErrorCodes.Validation);
    }

    [Fact]
    public async Task WrongContentType_Returns4000()
    {
        var content = new StringContent("email=a@b.com&password=x", Encoding.UTF8, "text/plain");

        var response = await factory.CreateClient().PostAsync("/api/auth/login", content);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.ReadEnvelopeAsync()).Code().ShouldBe(ErrorCodes.Validation);
    }

    [Fact]
    public async Task MissingBody_Returns4000()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login");
        request.Content = new StringContent(string.Empty, Encoding.UTF8, "application/json");

        var response = await factory.CreateClient().SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.ReadEnvelopeAsync()).Code().ShouldBe(ErrorCodes.Validation);
    }

    [Fact]
    public async Task Unauthenticated_Returns4010()
    {
        var response = await factory.CreateClient().GetAsync("/api/users");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.ReadEnvelopeAsync()).Code().ShouldBe(ErrorCodes.Unauthenticated);
    }

    [Fact]
    public async Task InvalidToken_Returns4010()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-jwt");

        var response = await client.GetAsync("/api/users");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.ReadEnvelopeAsync()).Code().ShouldBe(ErrorCodes.Unauthenticated);
    }

    [Fact]
    public async Task NonApiRoute_KeepsRealStatusCode()
    {
        var response = await factory.CreateClient().GetAsync("/not-an-api-route");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
