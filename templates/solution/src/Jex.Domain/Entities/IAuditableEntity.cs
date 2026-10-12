namespace Jex.Domain.Entities;

/// <summary>
/// Common identity and audit fields required by the generic repository.
/// </summary>
public interface IAuditableEntity
{
    long Id { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
}
