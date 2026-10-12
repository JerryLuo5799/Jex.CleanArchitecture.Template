using Jex.Application.Common.Interfaces;
using Jex.Application.Common.Messaging;
using Jex.Application.Common.Security;
using Jex.Domain.Entities;

namespace Jex.Application.Features.TodoLists.Commands.CreateTodoList;

/// <summary>Creates a todo list and returns its Id.</summary>
[AuthorizeRequest]
public sealed record CreateTodoListCommand(string Title, string? Colour = null) : ICommand<long>;

public sealed class CreateTodoListCommandHandler(ITodoListRepository repository)
    : IRequestHandler<CreateTodoListCommand, long>
{
    public async Task<long> Handle(CreateTodoListCommand request, CancellationToken cancellationToken)
    {
        var list = new TodoList
        {
            Title = request.Title,
            Colour = request.Colour ?? "#FFFFFF",
        };

        await repository.AddAsync(list, cancellationToken);

        return list.Id;
    }
}
