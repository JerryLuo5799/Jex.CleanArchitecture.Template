using Jex.Application.Common.Interfaces;
using Jex.Domain.Common;

namespace Jex.Infrastructure.Persistence;

/// <summary>
/// Fills audit fields right before a write. Lives in the scoped repository layer (not in a singleton
/// AOP hook) so it can read the current user without ambient state. Inserts never overwrite
/// an explicitly provided <c>CreatedAt</c>; updates never touch creation fields.
/// </summary>
public sealed class AuditStamper(ICurrentUser currentUser, TimeProvider timeProvider)
{
    public void OnInsert(IAuditableEntity entity)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (entity.CreatedAt == default)
        {
            entity.CreatedAt = now;
        }

        entity.CreatedBy ??= currentUser.Id;
    }

    public void OnUpdate(IAuditableEntity entity)
    {
        entity.LastModifiedAt = timeProvider.GetUtcNow().UtcDateTime;
        entity.LastModifiedBy = currentUser.Id;
    }

    public void OnSoftDelete(ISoftDelete entity, long id)
    {
        entity.IsDeleted = true;
        entity.DeletedAt = timeProvider.GetUtcNow().UtcDateTime;
        entity.DeletedBy = currentUser.Id;
        entity.DeletedToken = id;
    }
}
