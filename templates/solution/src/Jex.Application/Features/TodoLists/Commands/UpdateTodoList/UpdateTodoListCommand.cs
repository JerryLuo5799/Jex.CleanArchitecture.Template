using Jex.Application.Common.Exceptions;
using Jex.Application.Common.Interfaces;
using Jex.Application.Common.Messaging;
using Jex.Application.Common.Security;
using Jex.Domain.Entities;

namespace Jex.Application.Features.TodoLists.Commands.UpdateTodoList;

[AuthorizeRequest]
public sealed record UpdateTodoListCommand(long Id, string Title, string? Colour = null) : ICommand;

public sealed class UpdateTodoListCommandHandler(ITodoListRepository repository)
    : IRequestHandler<UpdateTodoListCommand>
{
    public async Task Handle(UpdateTodoListCommand request, CancellationToken cancellationToken)
    {
        // Load through the same repository so the update only writes changed columns.
        var list = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(TodoList), request.Id);

        list.Title = request.Title;
        if (request.Colour is not null)
        {
            list.Colour = request.Colour;
        }

        await repository.UpdateAsync(list, cancellationToken);
    }
}
