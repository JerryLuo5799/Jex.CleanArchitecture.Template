using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jex.Domain.Entities;

/// <summary>
/// Base entity with common audit fields for all domain entities.
/// ORM mapping is configured via data annotation attributes.
/// </summary>
public abstract class BaseEntity : IAuditableEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
