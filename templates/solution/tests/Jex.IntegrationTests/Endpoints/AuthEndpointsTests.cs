using System.Net;
using Jex.Application.Common;
using static Jex.IntegrationTests.ApiClientExtensions;

namespace Jex.IntegrationTests.Endpoints;

public sealed class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string Email = "login.tester@example.com";
    private const string Password = "Secure123!";

    private readonly HttpClient _client;
    private readonly HttpClient _authClient;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _authClient = factory.CreateAuthenticatedClient();
    }

    public async ValueTask InitializeAsync()
    {
        await _authClient.EnsureUserAsync(Email, Password);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        var response = await _client.PostAsync("/api/auth/login", JsonBody(new { Email, Password }));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var envelope = await response.ReadEnvelopeAsync();
        envelope.Code().ShouldBe(ErrorCodes.Success);
        envelope.Data().GetProperty("token").GetString().ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsUnauthenticatedCode()
    {
        var response = await _client.PostAsync("/api/auth/login", JsonBody(new { Email, Password = "WrongPassword!" }));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.ReadEnvelopeAsync()).Code().ShouldBe(ErrorCodes.Unauthenticated);
    }

    [Fact]
    public async Task Login_UnknownEmail_ReturnsUnauthenticatedCode()
    {
        var response = await _client.PostAsync("/api/auth/login", JsonBody(new { Email = "nobody@example.com", Password }));

        (await response.ReadEnvelopeAsync()).Code().ShouldBe(ErrorCodes.Unauthenticated);
    }

    [Fact]
    public async Task Login_MissingEmail_ReturnsValidationCode()
    {
        var response = await _client.PostAsync("/api/auth/login", JsonBody(new { Email = "", Password }));

        var envelope = await response.ReadEnvelopeAsync();
        envelope.Code().ShouldBe(ErrorCodes.Validation);
        envelope.GetProperty("errors").TryGetProperty("Email", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Login_ChangedEmail_StillLogsIn()
    {
        var id = await _authClient.CreateUserAsync("before@example.com");

        var update = await _authClient.PutAsync($"/api/users/{id}", JsonBody(new
        {
            FirstName = "Test",
            LastName = "User",
            Email = "after@example.com",
            Status = 1,
        }));
        (await update.ReadEnvelopeAsync()).Code().ShouldBe(ErrorCodes.Success);

        var login = await _client.PostAsync("/api/auth/login", JsonBody(new { Email = "after@example.com", Password }));
        (await login.ReadEnvelopeAsync()).Code().ShouldBe(ErrorCodes.Success);

        var oldLogin = await _client.PostAsync("/api/auth/login", JsonBody(new { Email = "before@example.com", Password }));
        (await oldLogin.ReadEnvelopeAsync()).Code().ShouldBe(ErrorCodes.Unauthenticated);
    }

    [Fact]
    public async Task Login_InactiveUser_IsRejected()
    {
        var id = await _authClient.CreateUserAsync("inactive@example.com");

        await _authClient.PutAsync($"/api/users/{id}", JsonBody(new
        {
            FirstName = "Test",
            LastName = "User",
            Email = "inactive@example.com",
            Status = 2,
        }));

        var login = await _client.PostAsync("/api/auth/login", JsonBody(new { Email = "inactive@example.com", Password }));
        (await login.ReadEnvelopeAsync()).Code().ShouldBe(ErrorCodes.Unauthenticated);
    }

    [Fact]
    public async Task Login_FiveFailures_LocksAccount()
    {
        await _authClient.CreateUserAsync("lockout@example.com");

        for (var i = 0; i < 5; i++)
        {
            await _client.PostAsync("/api/auth/login", JsonBody(new { Email = "lockout@example.com", Password = "Wrong!" }));
        }

        var login = await _client.PostAsync("/api/auth/login", JsonBody(new { Email = "lockout@example.com", Password }));
        (await login.ReadEnvelopeAsync()).Code().ShouldBe(ErrorCodes.Unauthenticated);
    }
}
