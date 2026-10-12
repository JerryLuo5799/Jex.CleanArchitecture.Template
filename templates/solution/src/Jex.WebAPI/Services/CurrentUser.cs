using System.Security.Claims;
using Jex.Application.Common.Interfaces;

namespace Jex.WebAPI.Services;

/// <summary><see cref="ICurrentUser"/> backed by the current HTTP request's claims principal.</summary>
public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor, IAuthorizationService authorizationService) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public long? Id =>
        long.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), System.Globalization.CultureInfo.InvariantCulture, out var id)
            ? id
            : null;

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email);

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public IReadOnlyCollection<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray() ?? [];

    public bool IsInRole(string role) => Principal?.IsInRole(role) == true;

    public async Task<bool> AuthorizeAsync(string policy, CancellationToken cancellationToken = default)
    {
        if (Principal is null)
        {
            return false;
        }

        var result = await authorizationService.AuthorizeAsync(Principal, policy);
        return result.Succeeded;
    }
}
