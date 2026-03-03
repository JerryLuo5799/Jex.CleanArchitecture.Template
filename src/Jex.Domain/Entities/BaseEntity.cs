namespace Jex.Domain.Entities;

/// <summary>
/// Base entity with common audit fields for all domain entities.
/// Table/column mappings are configured in Infrastructure via FreeSQL Fluent API.
/// </summary>
public abstract class BaseEntity
{
    public long Id { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
