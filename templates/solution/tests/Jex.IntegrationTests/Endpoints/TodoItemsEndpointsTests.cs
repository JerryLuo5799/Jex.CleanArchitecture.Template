using Jex.Application.Common;
using static Jex.IntegrationTests.ApiClientExtensions;

namespace Jex.IntegrationTests.Endpoints;

public sealed class TodoItemsEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateAuthenticatedClient();

    [Fact]
    public async Task GetWithPagination_ReturnsPageAndTotalCount()
    {
        var listId = await _client.CreateTodoListAsync("Paged");
        for (var i = 0; i < 3; i++)
        {
            await _client.PostAsync("/api/todoitems", JsonBody(new { ListId = listId, Title = $"Item {i}" }));
        }

        var envelope = await (await _client.GetAsync($"/api/todoitems?listId={listId}&page=1&pageSize=2")).ReadEnvelopeAsync();

        envelope.Code().ShouldBe(ErrorCodes.Success);
        envelope.Data().EnumerateArray().Count().ShouldBe(2);
        envelope.GetProperty("totalCount").GetInt64().ShouldBe(3);
    }

    [Fact]
    public async Task GetWithPagination_PageSizeTooLarge_Returns4000()
    {
        var envelope = await (await _client.GetAsync("/api/todoitems?listId=1&page=1&pageSize=500")).ReadEnvelopeAsync();

        envelope.Code().ShouldBe(ErrorCodes.Validation);
        envelope.GetProperty("errors").TryGetProperty("PageSize", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Create_UnknownList_Returns4040()
    {
        var response = await _client.PostAsync("/api/todoitems", JsonBody(new { ListId = 999999, Title = "Orphan" }));

        (await response.ReadEnvelopeAsync()).Code().ShouldBe(ErrorCodes.NotFound);
    }

    [Fact]
    public async Task Update_MarksDone_AndDetailMovesItem()
    {
        var listA = await _client.CreateTodoListAsync("List A");
        var listB = await _client.CreateTodoListAsync("List B");
        var itemId = (await (await _client.PostAsync("/api/todoitems", JsonBody(new { ListId = listA, Title = "Move me" }))).ReadEnvelopeAsync())
            .Data().GetProperty("id").GetInt64();

        (await (await _client.PutAsync($"/api/todoitems/{itemId}", JsonBody(new { Title = "Moved", Done = true }))).ReadEnvelopeAsync())
            .Code().ShouldBe(ErrorCodes.Success);
        (await (await _client.PutAsync($"/api/todoitems/{itemId}/detail", JsonBody(new { ListId = listB, Priority = 3, Note = "high" }))).ReadEnvelopeAsync())
            .Code().ShouldBe(ErrorCodes.Success);

        var inB = (await (await _client.GetAsync($"/api/todoitems?listId={listB}")).ReadEnvelopeAsync()).Data().EnumerateArray().Single();
        inB.GetProperty("title").GetString().ShouldBe("Moved");
        inB.GetProperty("done").GetBoolean().ShouldBeTrue();
        (await (await _client.GetAsync($"/api/todoitems?listId={listA}")).ReadEnvelopeAsync()).Data().EnumerateArray().ShouldBeEmpty();
    }

    [Fact]
    public async Task Delete_RemovesItem()
    {
        var listId = await _client.CreateTodoListAsync("Delete items");
        var itemId = (await (await _client.PostAsync("/api/todoitems", JsonBody(new { ListId = listId, Title = "Gone" }))).ReadEnvelopeAsync())
            .Data().GetProperty("id").GetInt64();

        (await (await _client.DeleteAsync($"/api/todoitems/{itemId}")).ReadEnvelopeAsync()).Code().ShouldBe(ErrorCodes.Success);
        (await (await _client.DeleteAsync($"/api/todoitems/{itemId}")).ReadEnvelopeAsync()).Code().ShouldBe(ErrorCodes.NotFound);
    }
}
