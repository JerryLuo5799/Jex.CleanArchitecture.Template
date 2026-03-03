using System.Linq.Expressions;
using Jex.Domain.Entities;

namespace Jex.Application.Common.Interfaces;

/// <summary>
/// Generic repository interface for FreeSQL-backed persistence.
/// </summary>
public interface IRepository<TEntity> where TEntity : BaseEntity
{
    Task<TEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<List<TEntity>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task DeleteAsync(long id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
}
