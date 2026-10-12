using Jex.Domain.Enums;

namespace Jex.Application.Common.Interfaces;

/// <summary>
/// Abstraction over ASP.NET Core Identity. All changes to identity fields
/// (email, user name, password, status) must go through this service so that
/// normalized columns, security stamps and lockout state stay consistent.
/// </summary>
public interface IIdentityService
{
    /// <summary>Creates a user and returns its Id. Throws <see cref="Exceptions.ValidationException"/> on Identity errors.</summary>
    Task<long> CreateUserAsync(
        string firstName,
        string lastName,
        string email,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>Updates profile and identity fields. Throws <see cref="Exceptions.NotFoundException"/> or <see cref="Exceptions.ValidationException"/>.</summary>
    Task UpdateUserAsync(
        long userId,
        string firstName,
        string lastName,
        string email,
        UserStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes a user. Throws <see cref="Exceptions.NotFoundException"/> if missing.</summary>
    Task DeleteUserAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates credentials (including lockout and <see cref="UserStatus"/>) and returns a signed JWT,
    /// or <c>null</c> when authentication fails. Failed attempts count towards lockout.
    /// </summary>
    Task<string?> GetAuthTokenAsync(string email, string password, CancellationToken cancellationToken = default);
}
