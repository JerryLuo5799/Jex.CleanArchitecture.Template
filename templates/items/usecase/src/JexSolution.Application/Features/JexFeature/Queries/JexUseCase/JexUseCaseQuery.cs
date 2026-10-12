using JexSolution.Application.Common.Messaging;
#if (Paged)
using JexSolution.Application.Common.Models;
#endif
using JexSolution.Application.Common.Security;

namespace JexSolution.Application.Features.JexFeature.Queries.JexUseCase;

#if (Paged)
/// <summary>TODO: describe what is read. Paged: the endpoint exposes items + totalCount (see GetTodoItemsWithPaginationQuery).</summary>
[AuthorizeRequest]
#if (WithId)
public sealed record JexUseCaseQuery(long Id, int Page = 1, int PageSize = 20)
    : PagedQuery(Page, PageSize), IQuery<PagedData<JexReturnType>>;
#else
public sealed record JexUseCaseQuery(int Page = 1, int PageSize = 20)
    : PagedQuery(Page, PageSize), IQuery<PagedData<JexReturnType>>;
#endif

/// <summary>Page/PageSize rules come from the base class; add query-specific rules here.</summary>
public sealed class JexUseCaseQueryValidator : PagedQueryValidator<JexUseCaseQuery>
{
}

/// <summary>Handles <see cref="JexUseCaseQuery"/>. Page through an I*Repository and map with <c>page.Map(Dto.From)</c> (ADR-0009).</summary>
public sealed class JexUseCaseQueryHandler : IRequestHandler<JexUseCaseQuery, PagedData<JexReturnType>>
{
    public Task<PagedData<JexReturnType>> Handle(JexUseCaseQuery request, CancellationToken cancellationToken)
    {
        // TODO: implement with a repository paging method (e.g. ITodoItemRepository.GetByListAsync).
        return Task.FromResult(new PagedData<JexReturnType>([], 0, request.Page, request.PageSize));
    }
}
#else
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
#if (WithId)
        // TODO: implement; load by request.Id and throw NotFoundException when it does not exist.
#else
        // TODO: implement.
#endif
        return Task.FromResult<JexReturnType>(default!);
    }
}
#endif
