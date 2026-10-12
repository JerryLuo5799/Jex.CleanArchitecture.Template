using Jex.Application.Common.Events;
using Jex.Application.Common.Interfaces;
using Jex.Domain.Entities;

namespace Jex.Infrastructure.Persistence.Repositories;

/// <summary>Registered automatically: Infrastructure scans for classes implementing an Application repository interface.</summary>
public sealed class TodoListRepository(IFreeSql freeSql, UnitOfWorkManager unitOfWorkManager, AuditStamper audit, PendingDomainEvents events)
    : Repository<TodoList>(freeSql, unitOfWorkManager, audit, events), ITodoListRepository
{
    public async Task<TodoList?> GetWithItemsAsync(long id, CancellationToken cancellationToken = default)
        => await Query.Where(l => l.Id == id).IncludeMany(l => l.Items).FirstAsync(cancellationToken);

    public async Task<List<TodoList>> GetAllWithItemsAsync(CancellationToken cancellationToken = default)
        => await Query.IncludeMany(l => l.Items).OrderBy(l => l.Title).ToListAsync(cancellationToken);

    public async Task<bool> IsTitleUniqueAsync(string title, long? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = Query.Where(l => l.Title == title);
        if (excludeId.HasValue)
        {
            query = query.Where(l => l.Id != excludeId.Value);
        }

        return !await query.AnyAsync(cancellationToken);
    }

    public async Task<int> PurgeDeletedAsync(CancellationToken cancellationToken = default)
    {
        var deletedIds = await Query
            .DisableGlobalFilter(FreeSqlFactory.SoftDeleteFilterName)
            .Where(l => l.IsDeleted)
            .ToListAsync(l => l.Id, cancellationToken);

        foreach (var id in deletedIds)
        {
            await Inner.Orm.Delete<TodoItem>()
                .WithTransaction(Inner.UnitOfWork?.GetOrBeginTransaction())
                .Where(i => i.ListId == id)
                .ExecuteAffrowsAsync(cancellationToken);

            await PurgeAsync(id, cancellationToken);
        }

        return deletedIds.Count;
    }
}
