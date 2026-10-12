using JexSolution.Application.Common.Messaging;
using JexSolution.Application.Common.Security;

namespace JexSolution.Application.Features.JexFeature.Commands.JexUseCase;

/// <summary>TODO: describe the state change. Add the request fields as record parameters.</summary>
[AuthorizeRequest]
#if (HasReturn)
#if (WithId)
public sealed record JexUseCaseCommand(long Id) : ICommand<JexReturnType>;
#else
public sealed record JexUseCaseCommand : ICommand<JexReturnType>;
#endif
#else
#if (WithId)
public sealed record JexUseCaseCommand(long Id) : ICommand;
#else
public sealed record JexUseCaseCommand : ICommand;
#endif
#endif

/// <summary>
/// Handles <see cref="JexUseCaseCommand"/>. Load entities through an I*Repository, change them and save
/// through the same repository; the unit of work commits after this handler returns (AGENTS.md section 6).
/// </summary>
#if (HasReturn)
public sealed class JexUseCaseCommandHandler : IRequestHandler<JexUseCaseCommand, JexReturnType>
{
    public Task<JexReturnType> Handle(JexUseCaseCommand request, CancellationToken cancellationToken)
    {
        // TODO: implement.
        return Task.FromResult<JexReturnType>(default!);
    }
}
#else
public sealed class JexUseCaseCommandHandler : IRequestHandler<JexUseCaseCommand>
{
    public Task Handle(JexUseCaseCommand request, CancellationToken cancellationToken)
    {
#if (WithId)
        // TODO: implement. Update: GetByIdAsync -> NotFoundException if null -> change fields -> UpdateAsync
        // (UpdateTodoItemCommand). Delete: ExistsAsync -> DeleteAsync, soft-deleted for ISoftDelete (DeleteTodoListCommand).
#else
        // TODO: implement.
#endif
        return Task.CompletedTask;
    }
}
#endif
