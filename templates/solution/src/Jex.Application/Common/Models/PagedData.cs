namespace Jex.Application.Common.Models;

/// <summary>
/// One page of results. Internal to the Application layer: endpoints unwrap it into the envelope's
/// <c>data</c> (items) and <c>totalCount</c> so clients see a single paging shape.
/// </summary>
public sealed record PagedData<T>(IReadOnlyList<T> Items, long TotalCount, int Page, int PageSize)
{
    public PagedData<TResult> Map<TResult>(Func<T, TResult> selector) =>
        new(Items.Select(selector).ToList(), TotalCount, Page, PageSize);
}
