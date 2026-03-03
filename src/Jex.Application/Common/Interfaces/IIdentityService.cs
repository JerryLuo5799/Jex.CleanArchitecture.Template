namespace Jex.Application.Common.Interfaces;

/// <summary>
/// Abstraction over ASP.NET Core Identity operations.
/// Keeps the Application layer free from direct Identity dependencies.
/// </summary>
public interface IIdentityService
{
    /// <summary>Creates a new user and returns the new user's Id on success.</summary>
    Task<(long UserId, bool Succeeded, IEnumerable<string> Errors)> CreateUserAsync(
        string firstName,
        string lastName,
        string email,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>Validates credentials and returns a signed JWT on success, or null on failure.</summary>
    Task<string?> GetAuthTokenAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);
}
