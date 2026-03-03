using FreeSql;
using Jex.Application.Common;
using Jex.Domain.Entities;

namespace Jex.Infrastructure.Persistence.Repositories;

/// <summary>
/// FreeSQL-backed user repository with email-specific queries.
/// </summary>
public sealed class UserRepository(IFreeSql freeSql)
    : Repository<User>(freeSql), IUserRepository
{
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => await FreeSql.Select<User>()
            .Where(u => u.Email == email)
            .FirstAsync(cancellationToken);

    public async Task<bool> IsEmailUniqueAsync(
        string email,
        long? excludeUserId = null,
        CancellationToken cancellationToken = default)
    {
        var query = FreeSql.Select<User>().Where(u => u.Email == email);

        if (excludeUserId.HasValue)
            query = query.Where(u => u.Id != excludeUserId.Value);

        return !await query.AnyAsync(cancellationToken);
    }
}
