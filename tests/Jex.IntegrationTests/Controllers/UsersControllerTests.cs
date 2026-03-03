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
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_EmptyDatabase_ReturnsOkWithEmptyList()
    {
        var response = await _client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var users = await response.Content.ReadFromJsonAsync<List<JsonElement>>(JsonOptions);
        Assert.NotNull(users);
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
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public UsersControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static StringContent JsonBody(object body) =>
        new(JsonSerializer.Serialize(body, JsonOptions), System.Text.Encoding.UTF8, "application/json");

    // ── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidPayload_Returns201WithId()
    {
        var payload = new
        {
            FirstName = "Alice",
            LastName = "Wonder",
            Email = "alice.wonder@example.com",
            Password = "Secure123!"
        };

        var response = await _client.PostAsync("/api/users", JsonBody(payload));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.True(body.TryGetProperty("id", out var idProp));
        Assert.True(idProp.GetInt64() > 0);
    }

    [Fact]
    public async Task Create_DuplicateEmail_Returns400()
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

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
        var id = created.GetProperty("id").GetInt64();

        // Retrieve it
        var response = await _client.GetAsync($"/api/users/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("Carol", user.GetProperty("firstName").GetString());
        Assert.Equal("carol@example.com", user.GetProperty("email").GetString());
    }

    [Fact]
    public async Task GetById_NonExistentUser_Returns404()
    {
        var response = await _client.GetAsync("/api/users/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_ExistingUser_Returns204()
    {
        // Create
        var create = new { FirstName = "Dave", LastName = "Old", Email = "dave@example.com", Password = "Secure123!" };
        var createResponse = await _client.PostAsync("/api/users", JsonBody(create));
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var id = created.GetProperty("id").GetInt64();

        // Update
        var update = new { FirstName = "Dave", LastName = "New", Email = "dave.new@example.com", Status = (int)UserStatus.Active };
        var response = await _client.PutAsync($"/api/users/{id}", JsonBody(update));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Update_NonExistentUser_Returns404()
    {
        var update = new { FirstName = "X", LastName = "Y", Email = "xy@example.com", Status = (int)UserStatus.Active };
        var response = await _client.PutAsync("/api/users/999999", JsonBody(update));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ExistingUser_Returns204ThenNotFound()
    {
        // Create
        var create = new { FirstName = "Eve", LastName = "Del", Email = "eve@example.com", Password = "Secure123!" };
        var createResponse = await _client.PostAsync("/api/users", JsonBody(create));
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var id = created.GetProperty("id").GetInt64();

        // Delete
        var deleteResponse = await _client.DeleteAsync($"/api/users/{id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Confirm gone
        var getResponse = await _client.GetAsync($"/api/users/{id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_NonExistentUser_Returns404()
    {
        var response = await _client.DeleteAsync("/api/users/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
