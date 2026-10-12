using Jex.Application.Common.Events;
using Jex.Application.Common.Interfaces;
using Jex.Application.Common.Models;
using Jex.Domain.Entities;

namespace Jex.Infrastructure.Persistence.Repositories;

public sealed class TodoItemRepository(IFreeSql freeSql, UnitOfWorkManager unitOfWorkManager, AuditStamper audit, PendingDomainEvents events)
    : Repository<TodoItem>(freeSql, unitOfWorkManager, audit, events), ITodoItemRepository
{
    public async Task<PagedData<TodoItem>> GetByListAsync(long listId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var items = await Query
            .Where(i => i.ListId == listId)
            .OrderBy(i => i.Id)
            .Count(out var total)
            .Page(page, pageSize)
            .ToListAsync(cancellationToken);

        return new PagedData<TodoItem>(items, total, page, pageSize);
    }
}
