namespace Jex.Domain.Common;

/// <summary>
/// Soft-deletable entity. <c>IRepository.DeleteAsync</c> marks instead of deleting; a global filter
/// hides deleted rows; <c>PurgeAsync</c> removes them physically.
/// <para>
/// Uniqueness semantics are "only active rows are unique": put <see cref="DeletedToken"/> into the
/// unique index together with the business key. It is <c>0</c> while active and becomes the row's
/// own <c>Id</c> on deletion, which works on every provider without partial indexes.
/// </para>
/// </summary>
public interface ISoftDelete
{
    bool IsDeleted { get; set; }

    DateTime? DeletedAt { get; set; }

    long? DeletedBy { get; set; }

    /// <summary><c>0</c> while active; the entity's own Id once soft-deleted.</summary>
    long DeletedToken { get; set; }
}
