using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Jex.IntegrationTests.Controllers;

/// <summary>
/// Integration tests for the authentication endpoint.
/// </summary>
public sealed class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly HttpClient _authClient;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        // Use an unauthenticated client — the login endpoint is public.
        _client = factory.CreateClient();
        // Pre-populate a user via the factory's authenticated client so we can test login.
        _authClient = factory.CreateAuthenticatedClient();
    }

    public async Task InitializeAsync()
    {
        await _authClient.PostAsync("/api/users", JsonBody(new
        {
            FirstName = "Login",
            LastName = "Tester",
            Email = "login.tester@example.com",
            Password = "Secure123!"
        }));
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static StringContent JsonBody(object body) =>
        new(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithToken()
    {
        var payload = new { Email = "login.tester@example.com", Password = "Secure123!" };
        var response = await _client.PostAsync("/api/auth/login", JsonBody(payload));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(2000, body.GetProperty("code").GetInt32());
        var data = body.GetProperty("data");
        Assert.True(data.TryGetProperty("token", out var tokenProp));
        var token = tokenProp.GetString();
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsOkWithErrorCode()
    {
        var payload = new { Email = "login.tester@example.com", Password = "WrongPassword!" };
        var response = await _client.PostAsync("/api/auth/login", JsonBody(payload));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(4000, body.GetProperty("code").GetInt32());
    }

    [Fact]
    public async Task Login_UnknownEmail_ReturnsOkWithErrorCode()
    {
        var payload = new { Email = "nobody@example.com", Password = "Secure123!" };
        var response = await _client.PostAsync("/api/auth/login", JsonBody(payload));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(4000, body.GetProperty("code").GetInt32());
    }

    [Fact]
    public async Task Login_MissingEmail_ReturnsOkWithErrorCode()
    {
        var payload = new { Email = "", Password = "Secure123!" };
        var response = await _client.PostAsync("/api/auth/login", JsonBody(payload));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(4000, body.GetProperty("code").GetInt32());
    }
}
