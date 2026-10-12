using Jex.Application.Common.Interfaces;
using Jex.Application.Common.Messaging;
using Jex.Application.Common.Security;
using Jex.Domain.Enums;

namespace Jex.Application.Features.TodoLists.Queries.GetTodos;

/// <summary>All active lists with their items plus the priority lookup.</summary>
[AuthorizeRequest]
public sealed record GetTodosQuery : IQuery<TodosVm>;

public sealed class GetTodosQueryHandler(ITodoListRepository repository) : IRequestHandler<GetTodosQuery, TodosVm>
{
    public async Task<TodosVm> Handle(GetTodosQuery request, CancellationToken cancellationToken)
    {
        var lists = await repository.GetAllWithItemsAsync(cancellationToken);

        return new TodosVm(
            Enum.GetValues<PriorityLevel>().Select(p => new LookupDto((int)p, p.ToString())).ToList(),
            lists.Select(TodoListDto.From).ToList());
    }
}
