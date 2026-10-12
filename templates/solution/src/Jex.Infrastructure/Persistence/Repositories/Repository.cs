using System.Linq.Expressions;
using Jex.Application.Common.Events;
using Jex.Application.Common.Interfaces;
using Jex.Application.Common.Models;
using Jex.Domain.Common;

namespace Jex.Infrastructure.Persistence.Repositories;

/// <summary>
/// Generic repository over a FreeSql <see cref="DefaultRepository{TEntity, TKey}"/> bound to the scoped
/// <see cref="UnitOfWorkManager"/>: every write joins the ambient transaction, entities loaded here are
/// change-tracked so updates only write dirty columns, audit fields are stamped, and domain events are
/// queued for dispatch before commit. This is the only place Infrastructure talks to FreeSql for writes.
/// </summary>
public class Repository<TEntity> : IRepository<TEntity>
    where TEntity : class, IAuditableEntity
{
    private static readonly bool s_isSoftDelete = typeof(ISoftDelete).IsAssignableFrom(typeof(TEntity));

    private readonly AuditStamper _audit;
    private readonly PendingDomainEvents _events;

    public Repository(IFreeSql freeSql, UnitOfWorkManager unitOfWorkManager, AuditStamper audit, PendingDomainEvents events)
    {
        Inner = new DefaultRepository<TEntity, long>(freeSql, unitOfWorkManager);
        _audit = audit;
        _events = events;
    }

    /// <summary>The underlying FreeSql repository, for provider-specific queries in derived repositories.</summary>
    protected IBaseRepository<TEntity, long> Inner { get; }

    /// <summary>Tracked query root. Entities returned from it can be passed to <see cref="UpdateAsync"/> for diff updates.</summary>
    protected ISelect<TEntity> Query => Inner.Select;

    public async Task<TEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => await Query.Where(e => e.Id == id).FirstAsync(cancellationToken);

    public async Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        => await Query.OrderBy(e => e.Id).ToListAsync(cancellationToken);

    public async Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        => await Query.Where(predicate).OrderBy(e => e.Id).ToListAsync(cancellationToken);

    public async Task<PagedData<TEntity>> GetPagedAsync(
        int page,
        int pageSize,
        Expression<Func<TEntity, bool>>? filter = null,
        CancellationToken cancellationToken = default)
    {
        var items = await Query
            .WhereIf(filter is not null, filter)
            .OrderBy(e => e.Id)
            .Count(out var total)
            .Page(page, pageSize)
            .ToListAsync(cancellationToken);

        return new PagedData<TEntity>(items, total, page, pageSize);
    }

    public async Task<long> CountAsync(Expression<Func<TEntity, bool>>? filter = null, CancellationToken cancellationToken = default)
        => await Query.WhereIf(filter is not null, filter).CountAsync(cancellationToken);

    public async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        => await Query.AnyAsync(predicate, cancellationToken);

    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _audit.OnInsert(entity);
        await Inner.InsertAsync(entity, cancellationToken);
        Track(entity);
        return entity;
    }

    public async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _audit.OnUpdate(entity);
        await Inner.UpdateAsync(entity, cancellationToken);
        Track(entity);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        if (s_isSoftDelete)
        {
            var entity = await GetByIdAsync(id, cancellationToken);
            if (entity is null)
            {
                return;
            }

            _audit.OnSoftDelete((ISoftDelete)entity, id);
            _audit.OnUpdate(entity);
            await Inner.UpdateAsync(entity, cancellationToken);
            Track(entity);
            return;
        }

        await Inner.DeleteAsync(e => e.Id == id, cancellationToken);
    }

    public async Task PurgeAsync(long id, CancellationToken cancellationToken = default)
    {
        // The one place that bypasses the soft-delete filter: physically remove the row.
        await Inner.Orm.Delete<TEntity>()
            .WithTransaction(Inner.UnitOfWork?.GetOrBeginTransaction())
            .DisableGlobalFilter(FreeSqlFactory.SoftDeleteFilterName)
            .Where(e => e.Id == id)
            .ExecuteAffrowsAsync(cancellationToken);
    }

    private void Track(TEntity entity)
    {
        if (entity is BaseEntity withEvents)
        {
            _events.Enqueue(withEvents);
        }
    }
}
