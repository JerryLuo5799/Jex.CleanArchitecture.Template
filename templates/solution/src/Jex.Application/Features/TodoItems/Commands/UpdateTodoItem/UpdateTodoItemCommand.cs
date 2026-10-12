using Jex.Application.Common.Exceptions;
using Jex.Application.Common.Interfaces;
using Jex.Application.Common.Messaging;
using Jex.Application.Common.Security;
using Jex.Domain.Entities;

namespace Jex.Application.Features.TodoItems.Commands.UpdateTodoItem;

/// <summary>Renames an item and/or marks it done. Setting <c>Done</c> raises a domain event.</summary>
[AuthorizeRequest]
public sealed record UpdateTodoItemCommand(long Id, string Title, bool Done) : ICommand;

public sealed class UpdateTodoItemCommandValidator : AbstractValidator<UpdateTodoItemCommand>
{
    public UpdateTodoItemCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
    }
}

public sealed class UpdateTodoItemCommandHandler(ITodoItemRepository repository)
    : IRequestHandler<UpdateTodoItemCommand>
{
    public async Task Handle(UpdateTodoItemCommand request, CancellationToken cancellationToken)
    {
        var item = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(TodoItem), request.Id);

        item.Title = request.Title;
        item.Done = request.Done;

        await repository.UpdateAsync(item, cancellationToken);
    }
}
