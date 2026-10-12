using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Jex.IntegrationTests;

/// <summary>Small helpers for calling the API and reading the response envelope.</summary>
public static class ApiClientExtensions
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static StringContent JsonBody(object body) =>
        new(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");

    public static async Task<JsonElement> ReadEnvelopeAsync(this HttpResponseMessage response)
        => await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

    public static int Code(this JsonElement envelope) => envelope.GetProperty("code").GetInt32();

    public static JsonElement Data(this JsonElement envelope) => envelope.GetProperty("data");

    /// <summary>Creates the user if it does not exist yet. Safe to call from per-test <c>InitializeAsync</c>.</summary>
    public static async Task EnsureUserAsync(this HttpClient client, string email, string password = "Secure123!")
    {
        var response = await client.PostAsync("/api/users", JsonBody(new
        {
            FirstName = "Test",
            LastName = "User",
            Email = email,
            Password = password,
        }));

        var envelope = await response.ReadEnvelopeAsync();
        if (envelope.Code() != 0 && !envelope.GetProperty("msg").GetString()!.Contains("already in use", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"ensure user failed: {envelope}");
        }
    }

    public static async Task<long> CreateTodoListAsync(this HttpClient client, string title)
    {
        var response = await client.PostAsync("/api/todolists", JsonBody(new { Title = title }));
        var envelope = await response.ReadEnvelopeAsync();
        envelope.Code().ShouldBe(0, $"create todo list failed: {envelope}");
        return envelope.Data().GetProperty("id").GetInt64();
    }

    public static async Task<long> CreateUserAsync(this HttpClient client, string email, string password = "Secure123!")
    {
        var response = await client.PostAsync("/api/users", JsonBody(new
        {
            FirstName = "Test",
            LastName = "User",
            Email = email,
            Password = password,
        }));

        var envelope = await response.ReadEnvelopeAsync();
        envelope.Code().ShouldBe(0, $"create user failed: {envelope}");
        return envelope.Data().GetProperty("id").GetInt64();
    }
}
