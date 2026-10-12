namespace Jex.Application.Common.Interfaces;

/// <summary>
/// The caller of the current request, derived from the validated JWT. Implemented in the Web layer.
/// Role and policy checks are claims-based: changes take effect when a new token is issued.
/// </summary>
public interface ICurrentUser
{
    /// <summary>User id, or <c>null</c> when the request is anonymous.</summary>
    long? Id { get; }

    string? Email { get; }

    bool IsAuthenticated { get; }

    IReadOnlyCollection<string> Roles { get; }

    bool IsInRole(string role);

    /// <summary>Evaluates a named authorization policy (see <c>Jex.Domain.Constants.Policies</c>) for the caller.</summary>
    Task<bool> AuthorizeAsync(string policy, CancellationToken cancellationToken = default);
}
