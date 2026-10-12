using System.Linq.Expressions;
using Jex.Application.Common.Models;
using Jex.Domain.Common;

namespace Jex.Application.Common.Interfaces;

/// <summary>
/// Generic repository. Implemented in Infrastructure over FreeSql with change tracking:
/// entities returned by <see cref="GetByIdAsync"/>/<see cref="FindAsync"/> are tracked, so a later
/// <see cref="UpdateAsync"/> writes only the changed columns. Always update an entity you loaded
/// through the same repository instance.
/// Writes join the ambient unit of work; audit fields are filled here, never in handlers.
/// </summary>
public interface IRepository<TEntity> where TEntity : class, IAuditableEntity
{
    Task<TEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    Task<PagedData<TEntity>> GetPagedAsync(
        int page,
        int pageSize,
        Expression<Func<TEntity, bool>>? filter = null,
        CancellationToken cancellationToken = default);

    Task<long> CountAsync(Expression<Func<TEntity, bool>>? filter = null, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes <see cref="ISoftDelete"/> entities, otherwise deletes the row.</summary>
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Physically removes the row, including soft-deleted ones.</summary>
    Task PurgeAsync(long id, CancellationToken cancellationToken = default);
}
