using Jex.Application.Features.TodoLists.Commands.CreateTodoList;
using Jex.Application.Features.TodoLists.Commands.DeleteTodoList;
using Jex.Application.Features.TodoLists.Commands.PurgeTodoLists;
using Jex.Application.Features.TodoLists.Commands.UpdateTodoList;
using Jex.Application.Features.TodoLists.Queries.GetTodos;
using Jex.WebAPI.Infrastructure;
using Jex.WebAPI.Models;

namespace Jex.WebAPI.Endpoints;

/// <summary>Sample feature: todo lists. Mapped under <c>/api/todolists</c>.</summary>
public sealed class TodoLists : IEndpointGroup
{
    public static string RoutePrefix => "/todolists";

    public static void Map(RouteGroupBuilder group)
    {
        group.RequireAuthorization();

        group.MapGet(GetTodoLists);
        group.MapPost(CreateTodoList);
        group.MapPut(UpdateTodoList, "{id:long}");
        group.MapDelete(DeleteTodoList, "{id:long}");
        group.MapPost(PurgeTodoLists, "purge");
    }

    private static async Task<ApiResponse<TodosVm>> GetTodoLists(ISender sender, CancellationToken cancellationToken)
        => ApiResponse.Success(await sender.Send(new GetTodosQuery(), cancellationToken));

    private static async Task<ApiResponse<CreatedResponse>> CreateTodoList(ISender sender, CreateTodoListCommand command, CancellationToken cancellationToken)
        => ApiResponse.Success(new CreatedResponse(await sender.Send(command, cancellationToken)));

    private static async Task<ApiResponse<object>> UpdateTodoList(ISender sender, long id, UpdateTodoListRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateTodoListCommand(id, request.Title, request.Colour), cancellationToken);
        return ApiResponse.Success();
    }

    private static async Task<ApiResponse<object>> DeleteTodoList(ISender sender, long id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteTodoListCommand(id), cancellationToken);
        return ApiResponse.Success();
    }

    /// <summary>Physically removes soft-deleted lists. Needs the CanPurge policy (Administrator).</summary>
    private static async Task<ApiResponse<PurgedResponse>> PurgeTodoLists(ISender sender, CancellationToken cancellationToken)
        => ApiResponse.Success(new PurgedResponse(await sender.Send(new PurgeTodoListsCommand(), cancellationToken)));
}

public sealed record UpdateTodoListRequest(string Title, string? Colour);

public sealed record PurgedResponse(int Count);
