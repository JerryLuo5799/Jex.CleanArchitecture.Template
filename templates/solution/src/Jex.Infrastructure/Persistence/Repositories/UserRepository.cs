using Jex.Application.Common.Events;
using Jex.Application.Common.Interfaces;
using Jex.Domain.Entities;

namespace Jex.Infrastructure.Persistence.Repositories;

/// <summary>User repository with normalized-email lookups.</summary>
public sealed class UserRepository(IFreeSql freeSql, UnitOfWorkManager unitOfWorkManager, AuditStamper audit, PendingDomainEvents events)
    : Repository<User>(freeSql, unitOfWorkManager, audit, events), IUserRepository
{
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.ToUpperInvariant();
        return await Query.Where(u => u.NormalizedEmail == normalized).FirstAsync(cancellationToken);
    }

    public async Task<bool> IsEmailUniqueAsync(string email, long? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        var normalized = email.ToUpperInvariant();
        var query = Query.Where(u => u.NormalizedEmail == normalized);

        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }

        return !await query.AnyAsync(cancellationToken);
    }
}
