using FreeSql;
using Jex.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Jex.Infrastructure.Identity;

/// <summary>
/// FreeSQL-backed implementation of <see cref="IUserStore{TUser}"/> and related user store interfaces.
/// Provides ASP.NET Core Identity with a persistence layer backed by FreeSQL instead of EF Core.
/// </summary>
public sealed class FreeSqlUserStore(IFreeSql freeSql)
    : IUserStore<User>,
      IUserPasswordStore<User>,
      IUserEmailStore<User>,
      IUserRoleStore<User>,
      IUserSecurityStampStore<User>,
      IUserLockoutStore<User>,
      IUserTwoFactorStore<User>
{
    // ── IUserStore ───────────────────────────────────────────────────────────

    public async Task<IdentityResult> CreateAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // ExecuteIdentityAsync returns the auto-generated ROWID from the database.
        // We assign it to user.Id so the caller (e.g. UserManager) can retrieve the new Id.
        var id = await freeSql.Insert(user).ExecuteIdentityAsync(cancellationToken);
        user.Id = id;
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        user.UpdatedAt = DateTime.UtcNow;
        await freeSql.Update<User>().SetSource(user).ExecuteAffrowsAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await freeSql.Delete<User>().Where(u => u.Id == user.Id).ExecuteAffrowsAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<User?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!long.TryParse(userId, out var id)) return null;
        return await freeSql.Select<User>().Where(u => u.Id == id).FirstAsync(cancellationToken);
    }

    public async Task<User?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await freeSql.Select<User>()
            .Where(u => u.NormalizedUserName == normalizedUserName)
            .FirstAsync(cancellationToken);
    }

    public Task<string> GetUserIdAsync(User user, CancellationToken cancellationToken)
        => Task.FromResult(user.Id.ToString());

    public Task<string?> GetUserNameAsync(User user, CancellationToken cancellationToken)
        => Task.FromResult(user.UserName);

    public Task SetUserNameAsync(User user, string? userName, CancellationToken cancellationToken)
    {
        user.UserName = userName;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedUserNameAsync(User user, CancellationToken cancellationToken)
        => Task.FromResult(user.NormalizedUserName);

    public Task SetNormalizedUserNameAsync(User user, string? normalizedName, CancellationToken cancellationToken)
    {
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    // ── IUserPasswordStore ───────────────────────────────────────────────────

    public Task SetPasswordHashAsync(User user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }

    public Task<string?> GetPasswordHashAsync(User user, CancellationToken cancellationToken)
        => Task.FromResult(user.PasswordHash);

    public Task<bool> HasPasswordAsync(User user, CancellationToken cancellationToken)
        => Task.FromResult(user.PasswordHash != null);

    // ── IUserEmailStore ──────────────────────────────────────────────────────

    public Task SetEmailAsync(User user, string? email, CancellationToken cancellationToken)
    {
        user.Email = email;
        return Task.CompletedTask;
    }

    public Task<string?> GetEmailAsync(User user, CancellationToken cancellationToken)
        => Task.FromResult(user.Email);

    public Task<bool> GetEmailConfirmedAsync(User user, CancellationToken cancellationToken)
        => Task.FromResult(user.EmailConfirmed);

    public Task SetEmailConfirmedAsync(User user, bool confirmed, CancellationToken cancellationToken)
    {
        user.EmailConfirmed = confirmed;
        return Task.CompletedTask;
    }

    public async Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await freeSql.Select<User>()
            .Where(u => u.NormalizedEmail == normalizedEmail)
            .FirstAsync(cancellationToken);
    }

    public Task<string?> GetNormalizedEmailAsync(User user, CancellationToken cancellationToken)
        => Task.FromResult(user.NormalizedEmail);

    public Task SetNormalizedEmailAsync(User user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        user.NormalizedEmail = normalizedEmail;
        return Task.CompletedTask;
    }

    // ── IUserSecurityStampStore ──────────────────────────────────────────────

    public Task SetSecurityStampAsync(User user, string stamp, CancellationToken cancellationToken)
    {
        user.SecurityStamp = stamp;
        return Task.CompletedTask;
    }

    public Task<string?> GetSecurityStampAsync(User user, CancellationToken cancellationToken)
        => Task.FromResult(user.SecurityStamp);

    // ── IUserLockoutStore ────────────────────────────────────────────────────

    public Task<DateTimeOffset?> GetLockoutEndDateAsync(User user, CancellationToken cancellationToken)
        => Task.FromResult(user.LockoutEnd);

    public Task SetLockoutEndDateAsync(User user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
    {
        user.LockoutEnd = lockoutEnd;
        return Task.CompletedTask;
    }

    public Task<int> IncrementAccessFailedCountAsync(User user, CancellationToken cancellationToken)
    {
        user.AccessFailedCount++;
        return Task.FromResult(user.AccessFailedCount);
    }

    public Task ResetAccessFailedCountAsync(User user, CancellationToken cancellationToken)
    {
        user.AccessFailedCount = 0;
        return Task.CompletedTask;
    }

    public Task<int> GetAccessFailedCountAsync(User user, CancellationToken cancellationToken)
        => Task.FromResult(user.AccessFailedCount);

    public Task<bool> GetLockoutEnabledAsync(User user, CancellationToken cancellationToken)
        => Task.FromResult(user.LockoutEnabled);

    public Task SetLockoutEnabledAsync(User user, bool enabled, CancellationToken cancellationToken)
    {
        user.LockoutEnabled = enabled;
        return Task.CompletedTask;
    }

    // ── IUserTwoFactorStore ──────────────────────────────────────────────────

    public Task SetTwoFactorEnabledAsync(User user, bool enabled, CancellationToken cancellationToken)
    {
        user.TwoFactorEnabled = enabled;
        return Task.CompletedTask;
    }

    public Task<bool> GetTwoFactorEnabledAsync(User user, CancellationToken cancellationToken)
        => Task.FromResult(user.TwoFactorEnabled);

    // ── IUserRoleStore ───────────────────────────────────────────────────────

    public async Task AddToRoleAsync(User user, string roleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var role = await freeSql.Select<ApplicationRole>()
            .Where(r => r.NormalizedName == roleName.ToUpperInvariant())
            .FirstAsync(cancellationToken)
            ?? throw new InvalidOperationException($"Role '{roleName}' does not exist.");

        var userRole = new UserRole { UserId = user.Id, RoleId = role.Id };
        await freeSql.Insert(userRole).ExecuteAffrowsAsync(cancellationToken);
    }

    public async Task RemoveFromRoleAsync(User user, string roleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var role = await freeSql.Select<ApplicationRole>()
            .Where(r => r.NormalizedName == roleName.ToUpperInvariant())
            .FirstAsync(cancellationToken);

        if (role is null) return;

        await freeSql.Delete<UserRole>()
            .Where(ur => ur.UserId == user.Id && ur.RoleId == role.Id)
            .ExecuteAffrowsAsync(cancellationToken);
    }

    public async Task<IList<string>> GetRolesAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var roleIds = await freeSql.Select<UserRole>()
            .Where(ur => ur.UserId == user.Id)
            .ToListAsync(ur => ur.RoleId, cancellationToken);

        if (roleIds.Count == 0) return [];

        var roles = await freeSql.Select<ApplicationRole>()
            .Where(r => roleIds.Contains(r.Id))
            .ToListAsync(cancellationToken);

        return roles.Select(r => r.Name ?? string.Empty).ToList();
    }

    public async Task<bool> IsInRoleAsync(User user, string roleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var role = await freeSql.Select<ApplicationRole>()
            .Where(r => r.NormalizedName == roleName.ToUpperInvariant())
            .FirstAsync(cancellationToken);

        if (role is null) return false;

        return await freeSql.Select<UserRole>()
            .AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id, cancellationToken);
    }

    public async Task<IList<User>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var role = await freeSql.Select<ApplicationRole>()
            .Where(r => r.NormalizedName == roleName.ToUpperInvariant())
            .FirstAsync(cancellationToken);

        if (role is null) return [];

        var userIds = await freeSql.Select<UserRole>()
            .Where(ur => ur.RoleId == role.Id)
            .ToListAsync(ur => ur.UserId, cancellationToken);

        if (userIds.Count == 0) return [];

        return await freeSql.Select<User>()
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync(cancellationToken);
    }

    // ── IDisposable ──────────────────────────────────────────────────────────

    public void Dispose() { }
}
