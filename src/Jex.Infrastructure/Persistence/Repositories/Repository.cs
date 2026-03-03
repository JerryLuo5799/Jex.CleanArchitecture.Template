using System.Linq.Expressions;
using FreeSql;
using Jex.Application.Common.Interfaces;

namespace Jex.Infrastructure.Persistence.Repositories;

/// <summary>
/// Generic FreeSQL-backed repository implementation.
/// </summary>
public class Repository<TEntity>(IFreeSql freeSql) : IRepository<TEntity>
    where TEntity : Domain.Entities.BaseEntity
{
    protected readonly IFreeSql FreeSql = freeSql;

    public async Task<TEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => await FreeSql.Select<TEntity>()
            .Where(e => e.Id == id)
            .FirstAsync(cancellationToken);

    public async Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        => await FreeSql.Select<TEntity>().ToListAsync(cancellationToken);

    public async Task<List<TEntity>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        => await FreeSql.Select<TEntity>()
            .Page(page, pageSize)
            .ToListAsync(cancellationToken);

    public async Task<List<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        => await FreeSql.Select<TEntity>().Where(predicate).ToListAsync(cancellationToken);

    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var id = await FreeSql.Insert(entity).ExecuteIdentityAsync(cancellationToken);
        entity.Id = id;
        return entity;
    }

    public async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        await FreeSql.Update<TEntity>()
            .SetSource(entity)
            .ExecuteAffrowsAsync(cancellationToken);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
        => await FreeSql.Delete<TEntity>()
            .Where(e => e.Id == id)
            .ExecuteAffrowsAsync(cancellationToken);

    public async Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        => await FreeSql.Select<TEntity>().AnyAsync(predicate, cancellationToken);
}
