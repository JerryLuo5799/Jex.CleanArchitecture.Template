using System.Net;
using Jex.Application.Common;
using Jex.Domain.Enums;
using static Jex.IntegrationTests.ApiClientExtensions;

namespace Jex.IntegrationTests.Endpoints;

/// <summary>Empty-state behaviour; uses its own factory so the database is guaranteed empty.</summary>
public sealed class UsersEndpointsEmptyTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task GetAll_EmptyDatabase_ReturnsEmptyList()
    {
        var client = factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/users");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var envelope = await response.ReadEnvelopeAsync();
        envelope.Code().ShouldBe(ErrorCodes.Success);
        envelope.Data().EnumerateArray().ShouldBeEmpty();
    }
}

public sealed class UsersEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public UsersEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task Create_ValidPayload_ReturnsId()
    {
        var id = await _client.CreateUserAsync("alice.wonder@example.com");

        id.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Create_DuplicateEmail_ReturnsValidationCode()
    {
        await _client.CreateUserAsync("bob.builder@example.com");

        var response = await _client.PostAsync("/api/users", JsonBody(new
        {
            FirstName = "Bob",
            LastName = "Builder",
            Email = "BOB.BUILDER@example.com",
            Password = "Secure123!",
        }));

        var envelope = await response.ReadEnvelopeAsync();
        envelope.Code().ShouldBe(ErrorCodes.Validation);
        envelope.GetProperty("errors").TryGetProperty("Email", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Create_InvalidPayload_ReturnsValidationCodeWithErrors()
    {
        var response = await _client.PostAsync("/api/users", JsonBody(new
        {
            FirstName = "",
            LastName = "Builder",
            Email = "not-an-email",
            Password = "short",
        }));

        var envelope = await response.ReadEnvelopeAsync();
        envelope.Code().ShouldBe(ErrorCodes.Validation);
        var errors = envelope.GetProperty("errors");
        errors.TryGetProperty("FirstName", out _).ShouldBeTrue();
        errors.TryGetProperty("Email", out _).ShouldBeTrue();
        errors.TryGetProperty("Password", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task GetById_ExistingUser_ReturnsUser()
    {
        var id = await _client.CreateUserAsync("carol@example.com");

        var response = await _client.GetAsync($"/api/users/{id}");

        var envelope = await response.ReadEnvelopeAsync();
        envelope.Code().ShouldBe(ErrorCodes.Success);
        envelope.Data().GetProperty("email").GetString().ShouldBe("carol@example.com");
    }

    [Fact]
    public async Task GetById_NonExistentUser_ReturnsNotFoundCode()
    {
        var response = await _client.GetAsync("/api/users/999999");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.ReadEnvelopeAsync()).Code().ShouldBe(ErrorCodes.NotFound);
    }

    [Fact]
    public async Task Update_ExistingUser_ReturnsSuccess()
    {
        var id = await _client.CreateUserAsync("dave@example.com");

        var response = await _client.PutAsync($"/api/users/{id}", JsonBody(new
        {
            FirstName = "Dave",
            LastName = "New",
            Email = "dave.new@example.com",
            Status = (int)UserStatus.Active,
        }));

        (await response.ReadEnvelopeAsync()).Code().ShouldBe(ErrorCodes.Success);

        var fetched = await (await _client.GetAsync($"/api/users/{id}")).ReadEnvelopeAsync();
        fetched.Data().GetProperty("lastName").GetString().ShouldBe("New");
        fetched.Data().GetProperty("email").GetString().ShouldBe("dave.new@example.com");
    }

    [Fact]
    public async Task Update_NonExistentUser_ReturnsNotFoundCode()
    {
        var response = await _client.PutAsync("/api/users/999999", JsonBody(new
        {
            FirstName = "X",
            LastName = "Y",
            Email = "xy@example.com",
            Status = (int)UserStatus.Active,
        }));

        (await response.ReadEnvelopeAsync()).Code().ShouldBe(ErrorCodes.NotFound);
    }

    [Fact]
    public async Task Delete_ExistingUser_ThenGetReturnsNotFound()
    {
        var id = await _client.CreateUserAsync("eve@example.com");

        var deleteResponse = await _client.DeleteAsync($"/api/users/{id}");
        (await deleteResponse.ReadEnvelopeAsync()).Code().ShouldBe(ErrorCodes.Success);

        var getResponse = await _client.GetAsync($"/api/users/{id}");
        (await getResponse.ReadEnvelopeAsync()).Code().ShouldBe(ErrorCodes.NotFound);
    }

    [Fact]
    public async Task Delete_NonExistentUser_ReturnsNotFoundCode()
    {
        var response = await _client.DeleteAsync("/api/users/999999");

        (await response.ReadEnvelopeAsync()).Code().ShouldBe(ErrorCodes.NotFound);
    }

    [Fact]
    public async Task AnyResponse_CarriesRequestIdHeaderMatchingEnvelope()
    {
        var response = await _client.GetAsync("/api/users/999999");

        var envelope = await response.ReadEnvelopeAsync();
        response.Headers.GetValues("X-Request-Id").Single().ShouldBe(envelope.GetProperty("requestId").GetString());
    }
}
