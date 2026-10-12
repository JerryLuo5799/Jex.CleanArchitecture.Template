using System.Net;
using Jex.Application.Common;
using Jex.Domain.Constants;
using static Jex.IntegrationTests.ApiClientExtensions;

namespace Jex.IntegrationTests.Endpoints;

public sealed class TodoListsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TodoListsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task Create_ThenGet_ReturnsListWithItems()
    {
        var listId = await _client.CreateTodoListAsync("Groceries");
        await _client.PostAsync("/api/todoitems", JsonBody(new { ListId = listId, Title = "Milk" }));

        var envelope = await (await _client.GetAsync("/api/todolists")).ReadEnvelopeAsync();

        envelope.Code().ShouldBe(ErrorCodes.Success);
        var list = envelope.Data().GetProperty("lists").EnumerateArray().Single(l => l.GetProperty("id").GetInt64() == listId);
        list.GetProperty("title").GetString().ShouldBe("Groceries");
        list.GetProperty("items").EnumerateArray().Single().GetProperty("title").GetString().ShouldBe("Milk");
        envelope.Data().GetProperty("priorityLevels").EnumerateArray().Count().ShouldBe(4);
    }

    [Fact]
    public async Task Create_DuplicateTitle_Returns4000()
    {
        await _client.CreateTodoListAsync("Unique");

        var response = await _client.PostAsync("/api/todolists", JsonBody(new { Title = "Unique" }));

        var envelope = await response.ReadEnvelopeAsync();
        envelope.Code().ShouldBe(ErrorCodes.Validation);
        envelope.GetProperty("errors").TryGetProperty("Title", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Create_InvalidColour_Returns4000()
    {
        var response = await _client.PostAsync("/api/todolists", JsonBody(new { Title = "Colourful", Colour = "red" }));

        (await response.ReadEnvelopeAsync()).Code().ShouldBe(ErrorCodes.Validation);
    }

    [Fact]
    public async Task Update_ExistingList_ChangesTitle()
    {
        var listId = await _client.CreateTodoListAsync("Before");

        var update = await _client.PutAsync($"/api/todolists/{listId}", JsonBody(new { Title = "After", Colour = "#1E90FF" }));
        (await update.ReadEnvelopeAsync()).Code().ShouldBe(ErrorCodes.Success);

        var envelope = await (await _client.GetAsync("/api/todolists")).ReadEnvelopeAsync();
        var list = envelope.Data().GetProperty("lists").EnumerateArray().Single(l => l.GetProperty("id").GetInt64() == listId);
        list.GetProperty("title").GetString().ShouldBe("After");
        list.GetProperty("colour").GetString().ShouldBe("#1E90FF");
    }

    [Fact]
    public async Task Delete_SoftDeletes_AndTitleBecomesReusable()
    {
        var listId = await _client.CreateTodoListAsync("Reusable");

        (await (await _client.DeleteAsync($"/api/todolists/{listId}")).ReadEnvelopeAsync()).Code().ShouldBe(ErrorCodes.Success);

        var lists = (await (await _client.GetAsync("/api/todolists")).ReadEnvelopeAsync()).Data().GetProperty("lists").EnumerateArray();
        lists.Any(l => l.GetProperty("id").GetInt64() == listId).ShouldBeFalse("soft-deleted lists are hidden");

        // Only active rows are unique: the same title can be created again, and deleted again.
        var second = await _client.CreateTodoListAsync("Reusable");
        (await (await _client.DeleteAsync($"/api/todolists/{second}")).ReadEnvelopeAsync()).Code().ShouldBe(ErrorCodes.Success);
    }

    [Fact]
    public async Task Delete_Unknown_Returns4040()
    {
        var response = await _client.DeleteAsync("/api/todolists/999999");

        (await response.ReadEnvelopeAsync()).Code().ShouldBe(ErrorCodes.NotFound);
    }

    [Fact]
    public async Task Purge_AsNonAdministrator_Returns4030()
    {
        var response = await _client.PostAsync("/api/todolists/purge", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.ReadEnvelopeAsync()).Code().ShouldBe(ErrorCodes.Forbidden);
    }

    [Fact]
    public async Task Purge_AsAdministrator_RemovesSoftDeletedLists()
    {
        var admin = _factory.CreateAuthenticatedClient(Roles.Administrator);
        var listId = await admin.CreateTodoListAsync("Purge me");
        await admin.DeleteAsync($"/api/todolists/{listId}");

        var response = await admin.PostAsync("/api/todolists/purge", null);

        var envelope = await response.ReadEnvelopeAsync();
        envelope.Code().ShouldBe(ErrorCodes.Success);
        envelope.Data().GetProperty("count").GetInt32().ShouldBeGreaterThanOrEqualTo(1);
    }
}
