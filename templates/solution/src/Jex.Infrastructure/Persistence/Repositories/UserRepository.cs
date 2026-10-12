using Jex.Application.Common.Interfaces;
using Jex.Domain.Entities;

namespace Jex.Infrastructure.Persistence.Repositories;

/// <summary>FreeSql-backed user repository with email lookups.</summary>
public sealed class UserRepository(IFreeSql freeSql)
    : Repository<User>(freeSql), IUserRepository
{
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.ToUpperInvariant();
        return await FreeSql.Select<User>()
            .Where(u => u.NormalizedEmail == normalized)
            .FirstAsync(cancellationToken);
    }

    public async Task<bool> IsEmailUniqueAsync(
        string email,
        long? excludeUserId = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = email.ToUpperInvariant();
        var query = FreeSql.Select<User>().Where(u => u.NormalizedEmail == normalized);

        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }

        return !await query.AnyAsync(cancellationToken);
    }
}
