using Jex.Application.Common.Exceptions;
using Jex.Application.Common.Interfaces;
using Jex.Application.Common.Messaging;
using Jex.Application.Common.Security;
using Jex.Domain.Entities;
using Jex.Domain.Events;

namespace Jex.Application.Features.TodoItems.Commands.DeleteTodoItem;

[AuthorizeRequest]
public sealed record DeleteTodoItemCommand(long Id) : ICommand;

public sealed class DeleteTodoItemCommandValidator : AbstractValidator<DeleteTodoItemCommand>
{
    public DeleteTodoItemCommandValidator() => RuleFor(x => x.Id).GreaterThan(0);
}

public sealed class DeleteTodoItemCommandHandler(ITodoItemRepository repository)
    : IRequestHandler<DeleteTodoItemCommand>
{
    public async Task Handle(DeleteTodoItemCommand request, CancellationToken cancellationToken)
    {
        var item = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(TodoItem), request.Id);

        item.AddDomainEvent(new TodoItemDeletedEvent(item));

        await repository.DeleteAsync(item.Id, cancellationToken);
    }
}
