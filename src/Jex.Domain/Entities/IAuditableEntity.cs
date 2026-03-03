namespace Jex.Domain.Entities;

/// <summary>
/// Marker interface for entities managed by the generic repository.
/// Provides the common audit fields required by <see cref="IRepository{TEntity}"/>.
/// </summary>
public interface IAuditableEntity
{
    long Id { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
}
