namespace Jex.Application.Common.Models;

/// <summary>
/// Base record for paged queries. Derive your validator from <see cref="PagedQueryValidator{TQuery}"/>
/// to get the Page/PageSize rules.
/// </summary>
public abstract record PagedQuery(int Page = 1, int PageSize = 20)
{
    public const int MaxPageSize = 100;
}

/// <summary>Validates Page ≥ 1 and 1 ≤ PageSize ≤ <see cref="PagedQuery.MaxPageSize"/>. Add query-specific rules in the subclass.</summary>
public abstract class PagedQueryValidator<TQuery> : AbstractValidator<TQuery>
    where TQuery : PagedQuery
{
    protected PagedQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, PagedQuery.MaxPageSize);
    }
}
