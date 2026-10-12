using Jex.Application.Common.Interfaces;
using Jex.Application.Common.Messaging;
using Jex.Application.Common.Security;
using Jex.Domain.Constants;

namespace Jex.Application.Features.TodoLists.Commands.PurgeTodoLists;

/// <summary>Physically removes soft-deleted lists. Requires the <see cref="Policies.CanPurge"/> policy.</summary>
[AuthorizeRequest(Policy = Policies.CanPurge)]
public sealed record PurgeTodoListsCommand : ICommand<int>;

public sealed class PurgeTodoListsCommandValidator : AbstractValidator<PurgeTodoListsCommand>;

public sealed class PurgeTodoListsCommandHandler(ITodoListRepository repository)
    : IRequestHandler<PurgeTodoListsCommand, int>
{
    public Task<int> Handle(PurgeTodoListsCommand request, CancellationToken cancellationToken)
        => repository.PurgeDeletedAsync(cancellationToken);
}
