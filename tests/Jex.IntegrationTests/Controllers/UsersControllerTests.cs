using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jex.Domain.Enums;

namespace Jex.IntegrationTests.Controllers;

/// <summary>
/// Integration tests that verify the empty-state behaviour of the Users API.
/// Uses its own factory so the database is guaranteed to be empty.
/// </summary>
public sealed class UsersControllerEmptyTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public UsersControllerEmptyTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task GetAll_EmptyDatabase_ReturnsOkWithEmptyList()
    {
        var response = await _client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(2000, body.GetProperty("code").GetInt32());
        var users = body.GetProperty("data").EnumerateArray().ToList();
        Assert.Empty(users);
    }
}

/// <summary>
/// End-to-end integration tests that exercise the Users REST API against a real SQLite database.
/// Each test class gets its own isolated factory (and therefore its own database file).
/// </summary>
public sealed class UsersControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public UsersControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClient();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static StringContent JsonBody(object body) =>
        new(JsonSerializer.Serialize(body, JsonOptions), System.Text.Encoding.UTF8, "application/json");

    // ── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidPayload_ReturnsOkWithId()
    {
        var payload = new
        {
            FirstName = "Alice",
            LastName = "Wonder",
            Email = "alice.wonder@example.com",
            Password = "Secure123!"
        };

        var response = await _client.PostAsync("/api/users", JsonBody(payload));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(2000, body.GetProperty("code").GetInt32());
        var data = body.GetProperty("data");
        Assert.True(data.TryGetProperty("id", out var idProp));
        Assert.True(idProp.GetInt64() > 0);
    }

    [Fact]
    public async Task Create_DuplicateEmail_ReturnsOkWithErrorCode()
    {
        var payload = new
        {
            FirstName = "Bob",
            LastName = "Builder",
            Email = "bob.builder@example.com",
            Password = "Secure123!"
        };

        await _client.PostAsync("/api/users", JsonBody(payload));
        var response = await _client.PostAsync("/api/users", JsonBody(payload));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(4000, body.GetProperty("code").GetInt32());
    }

    [Fact]
    public async Task GetById_ExistingUser_ReturnsOkWithUser()
    {
        // Create a user first
        var create = new
        {
            FirstName = "Carol",
            LastName = "Danvers",
            Email = "carol@example.com",
            Password = "Secure123!"
        };
        var createResponse = await _client.PostAsync("/api/users", JsonBody(create));
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var id = created.GetProperty("data").GetProperty("id").GetInt64();

        // Retrieve it
        var response = await _client.GetAsync($"/api/users/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(2000, body.GetProperty("code").GetInt32());
        var user = body.GetProperty("data");
        Assert.Equal("Carol", user.GetProperty("firstName").GetString());
        Assert.Equal("carol@example.com", user.GetProperty("email").GetString());
    }

    [Fact]
    public async Task GetById_NonExistentUser_ReturnsOkWithNotFoundCode()
    {
        var response = await _client.GetAsync("/api/users/999999");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(4040, body.GetProperty("code").GetInt32());
    }

    [Fact]
    public async Task Update_ExistingUser_ReturnsOkWithSuccessCode()
    {
        // Create
        var create = new { FirstName = "Dave", LastName = "Old", Email = "dave@example.com", Password = "Secure123!" };
        var createResponse = await _client.PostAsync("/api/users", JsonBody(create));
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var id = created.GetProperty("data").GetProperty("id").GetInt64();

        // Update
        var update = new { FirstName = "Dave", LastName = "New", Email = "dave.new@example.com", Status = (int)UserStatus.Active };
        var response = await _client.PutAsync($"/api/users/{id}", JsonBody(update));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(2000, body.GetProperty("code").GetInt32());
    }

    [Fact]
    public async Task Update_NonExistentUser_ReturnsOkWithNotFoundCode()
    {
        var update = new { FirstName = "X", LastName = "Y", Email = "xy@example.com", Status = (int)UserStatus.Active };
        var response = await _client.PutAsync("/api/users/999999", JsonBody(update));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(4040, body.GetProperty("code").GetInt32());
    }

    [Fact]
    public async Task Delete_ExistingUser_ReturnsOkThenNotFoundCode()
    {
        // Create
        var create = new { FirstName = "Eve", LastName = "Del", Email = "eve@example.com", Password = "Secure123!" };
        var createResponse = await _client.PostAsync("/api/users", JsonBody(create));
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var id = created.GetProperty("data").GetProperty("id").GetInt64();

        // Delete
        var deleteResponse = await _client.DeleteAsync($"/api/users/{id}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        var deleteBody = await deleteResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(2000, deleteBody.GetProperty("code").GetInt32());

        // Confirm gone
        var getResponse = await _client.GetAsync($"/api/users/{id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var getBody = await getResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(4040, getBody.GetProperty("code").GetInt32());
    }

    [Fact]
    public async Task Delete_NonExistentUser_ReturnsOkWithNotFoundCode()
    {
        var response = await _client.DeleteAsync("/api/users/999999");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(4040, body.GetProperty("code").GetInt32());
    }

    [Fact]
    public async Task Unauthenticated_ReturnsUnauthorized()
    {
        // Use a client without a JWT token.
        var unauthenticatedClient = _factory.CreateClient();
        var response = await unauthenticatedClient.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
