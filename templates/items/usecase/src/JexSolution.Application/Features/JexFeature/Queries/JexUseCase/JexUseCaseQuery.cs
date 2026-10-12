using JexSolution.Application.Common.Messaging;
using JexSolution.Application.Common.Security;

namespace JexSolution.Application.Features.JexFeature.Queries.JexUseCase;

/// <summary>TODO: describe what is read. Queries never change state.</summary>
[AuthorizeRequest]
#if (WithId)
public sealed record JexUseCaseQuery(long Id) : IQuery<JexReturnType>;
#else
public sealed record JexUseCaseQuery : IQuery<JexReturnType>;
#endif

/// <summary>Handles <see cref="JexUseCaseQuery"/>. Read through an I*Repository and map with a Dto.From method (ADR-0009).</summary>
public sealed class JexUseCaseQueryHandler : IRequestHandler<JexUseCaseQuery, JexReturnType>
{
    public Task<JexReturnType> Handle(JexUseCaseQuery request, CancellationToken cancellationToken)
    {
        // TODO: implement; throw NotFoundException when the entity does not exist.
        return Task.FromResult<JexReturnType>(default!);
    }
}
