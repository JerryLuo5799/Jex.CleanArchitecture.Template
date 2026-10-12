using Jex.Application.Common.Exceptions;
using Jex.Application.Common.Interfaces;
using Jex.Domain.Entities;
using Jex.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Jex.Infrastructure.Identity;

/// <summary>
/// <see cref="IIdentityService"/> implementation over <see cref="UserManager{TUser}"/>.
/// All identity field changes go through UserManager so normalized columns, security stamps
/// and lockout counters stay consistent.
/// </summary>
public sealed class IdentityService(
    UserManager<User> userManager,
    TokenService tokenService) : IIdentityService
{
    public async Task<long> CreateUserAsync(
        string firstName,
        string lastName,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = new User
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Status = UserStatus.Active,
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(user, password);
        ThrowIfFailed(result, nameof(password));

        return user.Id;
    }

    public async Task UpdateUserAsync(
        long userId,
        string firstName,
        string lastName,
        string email,
        UserStatus status,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await FindAsync(userId);

        user.FirstName = firstName;
        user.LastName = lastName;
        user.Status = status;

        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            // SetEmail/SetUserName also refresh the normalized columns used for lookups and login.
            ThrowIfFailed(await userManager.SetEmailAsync(user, email), nameof(email));
            ThrowIfFailed(await userManager.SetUserNameAsync(user, email), nameof(email));
        }

        ThrowIfFailed(await userManager.UpdateAsync(user), nameof(email));
    }

    public async Task DeleteUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await FindAsync(userId);
        ThrowIfFailed(await userManager.DeleteAsync(user), "user");
    }

    public async Task<string?> GetAuthTokenAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return null;
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return null;
        }

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            // Counts towards lockout (MaxFailedAccessAttempts). The command running this is
            // [NonTransactional] so the counter is persisted even though login "fails".
            await userManager.AccessFailedAsync(user);
            return null;
        }

        if (user.Status != UserStatus.Active)
        {
            return null;
        }

        if (user.AccessFailedCount > 0)
        {
            await userManager.ResetAccessFailedCountAsync(user);
        }

        var roles = await userManager.GetRolesAsync(user);
        return tokenService.CreateToken(user, roles);
    }

    private async Task<User> FindAsync(long userId)
        => await userManager.FindByIdAsync(userId.ToString(System.Globalization.CultureInfo.InvariantCulture))
           ?? throw new NotFoundException(nameof(User), userId);

    private static void ThrowIfFailed(IdentityResult result, string member)
    {
        if (result.Succeeded)
        {
            return;
        }

        if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.ConcurrencyFailure)))
        {
            throw new ConflictException("The user was modified by another request. Reload and try again.");
        }

        throw new ValidationException(member, string.Join(" ", result.Errors.Select(e => e.Description)));
    }
}
