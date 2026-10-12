using Jex.Application.Common.Interfaces;
using Jex.Application.Common.Messaging;
using Jex.Application.Common.Models;
using Jex.Application.Common.Security;
using Jex.Domain.Entities;

namespace Jex.Application.Features.TodoItems.Queries.GetTodoItemsWithPagination;

/// <summary>Items of one list, paged. Returns <see cref="PagedData{T}"/>; the endpoint exposes items + totalCount.</summary>
[AuthorizeRequest]
public sealed record GetTodoItemsWithPaginationQuery(long ListId, int Page = 1, int PageSize = 20)
    : PagedQuery(Page, PageSize), IQuery<PagedData<TodoItemBriefDto>>;

public sealed class GetTodoItemsWithPaginationQueryValidator : PagedQueryValidator<GetTodoItemsWithPaginationQuery>
{
    public GetTodoItemsWithPaginationQueryValidator() => RuleFor(x => x.ListId).GreaterThan(0);
}

public sealed class GetTodoItemsWithPaginationQueryHandler(ITodoItemRepository repository)
    : IRequestHandler<GetTodoItemsWithPaginationQuery, PagedData<TodoItemBriefDto>>
{
    public async Task<PagedData<TodoItemBriefDto>> Handle(GetTodoItemsWithPaginationQuery request, CancellationToken cancellationToken)
    {
        var page = await repository.GetByListAsync(request.ListId, request.Page, request.PageSize, cancellationToken);
        return page.Map(TodoItemBriefDto.From);
    }
}

public sealed record TodoItemBriefDto(long Id, long ListId, string Title, bool Done)
{
    public static TodoItemBriefDto From(TodoItem item) => new(item.Id, item.ListId, item.Title, item.Done);
}
