using Jex.Application.Common.Exceptions;
using Jex.Application.Common.Interfaces;
using Jex.Application.Common.Messaging;
using Jex.Application.Common.Security;
using Jex.Domain.Entities;
using Jex.Domain.Events;

namespace Jex.Application.Features.TodoItems.Commands.CreateTodoItem;

[AuthorizeRequest]
public sealed record CreateTodoItemCommand(long ListId, string Title) : ICommand<long>;

public sealed class CreateTodoItemCommandValidator : AbstractValidator<CreateTodoItemCommand>
{
    public CreateTodoItemCommandValidator()
    {
        RuleFor(x => x.ListId).GreaterThan(0);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
    }
}

public sealed class CreateTodoItemCommandHandler(ITodoListRepository lists, ITodoItemRepository items)
    : IRequestHandler<CreateTodoItemCommand, long>
{
    public async Task<long> Handle(CreateTodoItemCommand request, CancellationToken cancellationToken)
    {
        if (!await lists.ExistsAsync(l => l.Id == request.ListId, cancellationToken))
        {
            throw new NotFoundException(nameof(TodoList), request.ListId);
        }

        var item = new TodoItem
        {
            ListId = request.ListId,
            Title = request.Title,
            Done = false,
        };

        // Domain events are dispatched by the unit of work right before commit.
        item.AddDomainEvent(new TodoItemCreatedEvent(item));

        await items.AddAsync(item, cancellationToken);

        return item.Id;
    }
}
