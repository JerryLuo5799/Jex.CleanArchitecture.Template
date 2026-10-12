namespace Jex.Domain.Common;

/// <summary>
/// Identity and audit fields required by the generic repository. The repository fills the
/// audit fields; handlers never set them (see <c>Jex.Infrastructure.Persistence.AuditStamper</c>).
/// All timestamps are UTC.
/// </summary>
public interface IAuditableEntity
{
    long Id { get; set; }

    DateTime CreatedAt { get; set; }

    long? CreatedBy { get; set; }

    DateTime? LastModifiedAt { get; set; }

    long? LastModifiedBy { get; set; }
}
