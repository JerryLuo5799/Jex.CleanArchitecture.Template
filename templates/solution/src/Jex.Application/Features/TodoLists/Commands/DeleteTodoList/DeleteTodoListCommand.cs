using Jex.Application.Common.Exceptions;
using Jex.Application.Common.Interfaces;
using Jex.Application.Common.Messaging;
using Jex.Application.Common.Security;
using Jex.Domain.Entities;

namespace Jex.Application.Features.TodoLists.Commands.DeleteTodoList;

/// <summary>Soft-deletes a list (it disappears from queries; <c>PurgeTodoListsCommand</c> removes it for good).</summary>
[AuthorizeRequest]
public sealed record DeleteTodoListCommand(long Id) : ICommand;

public sealed class DeleteTodoListCommandValidator : AbstractValidator<DeleteTodoListCommand>
{
    public DeleteTodoListCommandValidator() => RuleFor(x => x.Id).GreaterThan(0);
}

public sealed class DeleteTodoListCommandHandler(ITodoListRepository repository)
    : IRequestHandler<DeleteTodoListCommand>
{
    public async Task Handle(DeleteTodoListCommand request, CancellationToken cancellationToken)
    {
        if (!await repository.ExistsAsync(l => l.Id == request.Id, cancellationToken))
        {
            throw new NotFoundException(nameof(TodoList), request.Id);
        }

        await repository.DeleteAsync(request.Id, cancellationToken);
    }
}
