using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Jex.IntegrationTests.Endpoints;

public sealed class OpenApiTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task OpenApiDocument_IsServed_WithBearerSchemeAndStableOperationIds()
    {
        var response = await factory.CreateClient().GetAsync("/openapi/v1.json");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var document = await response.Content.ReadFromJsonAsync<JsonElement>();

        document.GetProperty("components").GetProperty("securitySchemes").TryGetProperty("Bearer", out _).ShouldBeTrue();

        var operationIds = document.GetProperty("paths").EnumerateObject()
            .SelectMany(p => p.Value.EnumerateObject())
            .Select(op => op.Value.GetProperty("operationId").GetString())
            .ToList();

        operationIds.ShouldContain("Auth_Login");
        operationIds.ShouldContain("Users_GetUserById");
        operationIds.Distinct().Count().ShouldBe(operationIds.Count, "operation ids must be unique");
    }

    [Fact]
    public async Task ScalarUi_IsServed()
    {
        var response = await factory.CreateClient().GetAsync("/scalar");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
