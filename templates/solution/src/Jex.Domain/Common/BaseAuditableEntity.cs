namespace Jex.Domain.Common;

/// <summary>Entity with creation/modification audit fields (UTC). Most aggregates derive from this.</summary>
public abstract class BaseAuditableEntity : BaseEntity, IAuditableEntity
{
    public DateTime CreatedAt { get; set; }

    public long? CreatedBy { get; set; }

    public DateTime? LastModifiedAt { get; set; }

    public long? LastModifiedBy { get; set; }
}
