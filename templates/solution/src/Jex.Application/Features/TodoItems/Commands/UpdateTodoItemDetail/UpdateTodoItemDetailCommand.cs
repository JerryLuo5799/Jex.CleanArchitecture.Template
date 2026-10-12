using Jex.Application.Common.Exceptions;
using Jex.Application.Common.Interfaces;
using Jex.Application.Common.Messaging;
using Jex.Application.Common.Security;
using Jex.Domain.Entities;
using Jex.Domain.Enums;

namespace Jex.Application.Features.TodoItems.Commands.UpdateTodoItemDetail;

/// <summary>Moves an item to another list and updates its priority/note.</summary>
[AuthorizeRequest]
public sealed record UpdateTodoItemDetailCommand(long Id, long ListId, PriorityLevel Priority, string? Note) : ICommand;

public sealed class UpdateTodoItemDetailCommandValidator : AbstractValidator<UpdateTodoItemDetailCommand>
{
    public UpdateTodoItemDetailCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.ListId).GreaterThan(0);
        RuleFor(x => x.Priority).IsInEnum();
        RuleFor(x => x.Note).MaximumLength(2000);
    }
}

public sealed class UpdateTodoItemDetailCommandHandler(ITodoListRepository lists, ITodoItemRepository items)
    : IRequestHandler<UpdateTodoItemDetailCommand>
{
    public async Task Handle(UpdateTodoItemDetailCommand request, CancellationToken cancellationToken)
    {
        var item = await items.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(TodoItem), request.Id);

        if (!await lists.ExistsAsync(l => l.Id == request.ListId, cancellationToken))
        {
            throw new NotFoundException(nameof(TodoList), request.ListId);
        }

        item.ListId = request.ListId;
        item.Priority = request.Priority;
        item.Note = request.Note;

        await items.UpdateAsync(item, cancellationToken);
    }
}
