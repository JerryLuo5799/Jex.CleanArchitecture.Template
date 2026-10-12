using Jex.Domain.Entities;

namespace Jex.Application.Common.Interfaces;

/// <summary>User-specific read queries that go beyond the generic repository.</summary>
public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> IsEmailUniqueAsync(string email, long? excludeUserId = null, CancellationToken cancellationToken = default);
}
