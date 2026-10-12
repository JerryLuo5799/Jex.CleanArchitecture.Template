using Jex.Application.Features.TodoItems.Commands.CreateTodoItem;
using Jex.Application.Features.TodoItems.Commands.DeleteTodoItem;
using Jex.Application.Features.TodoItems.Commands.UpdateTodoItem;
using Jex.Application.Features.TodoItems.Commands.UpdateTodoItemDetail;
using Jex.Application.Features.TodoItems.Queries.GetTodoItemsWithPagination;
using Jex.Domain.Enums;
using Jex.WebAPI.Infrastructure;
using Jex.WebAPI.Models;

namespace Jex.WebAPI.Endpoints;

/// <summary>Sample feature: todo items. Mapped under <c>/api/todoitems</c>.</summary>
public sealed class TodoItems : IEndpointGroup
{
    public static string RoutePrefix => "/todoitems";

    public static void Map(RouteGroupBuilder group)
    {
        group.RequireAuthorization();

        group.MapGet(GetTodoItemsWithPagination);
        group.MapPost(CreateTodoItem);
        group.MapPut(UpdateTodoItem, "{id:long}");
        group.MapPut(UpdateTodoItemDetail, "{id:long}/detail");
        group.MapDelete(DeleteTodoItem, "{id:long}");
    }

    /// <summary>Paged items of a list: <c>data</c> holds the page, <c>totalCount</c> the total.</summary>
    private static async Task<ApiResponse<IReadOnlyList<TodoItemBriefDto>>> GetTodoItemsWithPagination(
        ISender sender, long listId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetTodoItemsWithPaginationQuery(listId, page, pageSize), cancellationToken);
        return ApiResponse.Success(result.Items, result.TotalCount);
    }

    private static async Task<ApiResponse<CreatedResponse>> CreateTodoItem(ISender sender, CreateTodoItemCommand command, CancellationToken cancellationToken)
        => ApiResponse.Success(new CreatedResponse(await sender.Send(command, cancellationToken)));

    private static async Task<ApiResponse<object>> UpdateTodoItem(ISender sender, long id, UpdateTodoItemRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateTodoItemCommand(id, request.Title, request.Done), cancellationToken);
        return ApiResponse.Success();
    }

    private static async Task<ApiResponse<object>> UpdateTodoItemDetail(ISender sender, long id, UpdateTodoItemDetailRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateTodoItemDetailCommand(id, request.ListId, request.Priority, request.Note), cancellationToken);
        return ApiResponse.Success();
    }

    private static async Task<ApiResponse<object>> DeleteTodoItem(ISender sender, long id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteTodoItemCommand(id), cancellationToken);
        return ApiResponse.Success();
    }
}

public sealed record UpdateTodoItemRequest(string Title, bool Done);

public sealed record UpdateTodoItemDetailRequest(long ListId, PriorityLevel Priority, string? Note);
