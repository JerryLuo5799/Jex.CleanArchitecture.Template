using System.Net;
using Jex.Application.Common;
using static Jex.IntegrationTests.ApiClientExtensions;

namespace Jex.IntegrationTests.Endpoints;

/// <summary>Uses its own factory with a tiny login limit so other test classes are unaffected.</summary>
public sealed class RateLimitingTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new CustomWebApplicationFactory()
        .WithSettings(("RateLimiting:Enabled", "true"), ("RateLimiting:AuthPermitLimit", "2"), ("RateLimiting:AuthWindowSeconds", "60"));

    [Fact]
    public async Task Login_BeyondAuthLimit_Returns4290WithRetryAfter()
    {
        var client = _factory.CreateClient();
        var body = new { Email = "nobody@example.com", Password = "x" };

        await client.PostAsync("/api/auth/login", JsonBody(body));
        await client.PostAsync("/api/auth/login", JsonBody(body));
        var limited = await client.PostAsync("/api/auth/login", JsonBody(body));

        limited.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await limited.ReadEnvelopeAsync()).Code().ShouldBe(ErrorCodes.TooManyRequests);
        limited.Headers.RetryAfter.ShouldNotBeNull();
    }

    [Fact]
    public async Task HealthEndpoint_IsNotRateLimited()
    {
        var client = _factory.CreateClient();

        for (var i = 0; i < 5; i++)
        {
            (await client.GetAsync("/health/live")).StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }

    public void Dispose() => _factory.Dispose();
}
