using Jex.Application.Common;
using Jex.Domain.Entities;
using Jex.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Jex.Infrastructure.Identity;

/// <summary>
/// Application-layer <see cref="IIdentityService"/> implementation backed by
/// ASP.NET Core Identity's <see cref="UserManager{TUser}"/>.
/// </summary>
public sealed class IdentityService(
    UserManager<User> userManager,
    TokenService tokenService) : IIdentityService
{
    public async Task<(long UserId, bool Succeeded, IEnumerable<string> Errors)> CreateUserAsync(
        string firstName,
        string lastName,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = new User
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(user, password);

        return result.Succeeded
            ? (user.Id, true, [])
            : (0, false, result.Errors.Select(e => e.Description));
    }

    public async Task<string?> GetAuthTokenAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null) return null;

        var isValidPassword = await userManager.CheckPasswordAsync(user, password);
        if (!isValidPassword) return null;

        return tokenService.CreateToken(user);
    }
}
