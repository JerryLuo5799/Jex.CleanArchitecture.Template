using Jex.Domain.Entities;
using Jex.Infrastructure.Persistence;

namespace Jex.Infrastructure.Identity;

/// <summary>
/// FreeSql-backed Identity user store. All writes go through unit-of-work bound repositories so they
/// join the command's transaction; updates are guarded by <see cref="IdentityUser{TKey}.ConcurrencyStamp"/>.
/// </summary>
public sealed class FreeSqlUserStore
    : IUserStore<User>,
      IUserPasswordStore<User>,
      IUserEmailStore<User>,
      IUserRoleStore<User>,
      IUserSecurityStampStore<User>,
      IUserLockoutStore<User>,
      IUserTwoFactorStore<User>
{
    private readonly DefaultRepository<User, long> _users;
    private readonly DefaultRepository<ApplicationRole, long> _roles;
    private readonly DefaultRepository<UserRole, long> _userRoles;
    private readonly AuditStamper _audit;
    private readonly IdentityErrorDescriber _errors;

    public FreeSqlUserStore(IFreeSql freeSql, UnitOfWorkManager unitOfWorkManager, AuditStamper audit, IdentityErrorDescriber errors)
    {
        _users = new DefaultRepository<User, long>(freeSql, unitOfWorkManager);
        _roles = new DefaultRepository<ApplicationRole, long>(freeSql, unitOfWorkManager);
        _userRoles = new DefaultRepository<UserRole, long>(freeSql, unitOfWorkManager);
        _audit = audit;
        _errors = errors;
    }

    // ── IUserStore ───────────────────────────────────────────────────────────

    public async Task<IdentityResult> CreateAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _audit.OnInsert(user);
        await _users.InsertAsync(user, cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Optimistic concurrency: the row must still carry the stamp we loaded; then rotate it.
        var expectedStamp = user.ConcurrencyStamp;
        user.ConcurrencyStamp = Guid.NewGuid().ToString();
        _audit.OnUpdate(user);

        var affected = await _users.UpdateDiy
            .SetSource(user)
            .Where(u => u.ConcurrencyStamp == expectedStamp)
            .ExecuteAffrowsAsync(cancellationToken);

        if (affected == 0)
        {
            user.ConcurrencyStamp = expectedStamp;
            return IdentityResult.Failed(_errors.ConcurrencyFailure());
        }

        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _userRoles.DeleteAsync(ur => ur.UserId == user.Id, cancellationToken);
        await _users.DeleteAsync(u => u.Id == user.Id, cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<User?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return long.TryParse(userId, System.Globalization.CultureInfo.InvariantCulture, out var id)
            ? await _users.Select.Where(u => u.Id == id).FirstAsync(cancellationToken)
            : null;
    }

    public async Task<User?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _users.Select.Where(u => u.NormalizedUserName == normalizedUserName).FirstAsync(cancellationToken);
    }

    public Task<string> GetUserIdAsync(User user, CancellationToken cancellationToken)
        => Task.FromResult(user.Id.ToString(System.Globalization.CultureInfo.InvariantCulture));

    public Task<string?> GetUserNameAsync(User user, CancellationToken cancellationToken) => Task.FromResult(user.UserName);

    public Task SetUserNameAsync(User user, string? userName, CancellationToken cancellationToken)
    {
        user.UserName = userName;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedUserNameAsync(User user, CancellationToken cancellationToken) => Task.FromResult(user.NormalizedUserName);

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

    public Task<string?> GetPasswordHashAsync(User user, CancellationToken cancellationToken) => Task.FromResult(user.PasswordHash);

    public Task<bool> HasPasswordAsync(User user, CancellationToken cancellationToken) => Task.FromResult(user.PasswordHash is not null);

    // ── IUserEmailStore ──────────────────────────────────────────────────────

    public Task SetEmailAsync(User user, string? email, CancellationToken cancellationToken)
    {
        user.Email = email;
        return Task.CompletedTask;
    }

    public Task<string?> GetEmailAsync(User user, CancellationToken cancellationToken) => Task.FromResult(user.Email);

    public Task<bool> GetEmailConfirmedAsync(User user, CancellationToken cancellationToken) => Task.FromResult(user.EmailConfirmed);

    public Task SetEmailConfirmedAsync(User user, bool confirmed, CancellationToken cancellationToken)
    {
        user.EmailConfirmed = confirmed;
        return Task.CompletedTask;
    }

    public async Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _users.Select.Where(u => u.NormalizedEmail == normalizedEmail).FirstAsync(cancellationToken);
    }

    public Task<string?> GetNormalizedEmailAsync(User user, CancellationToken cancellationToken) => Task.FromResult(user.NormalizedEmail);

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

    public Task<string?> GetSecurityStampAsync(User user, CancellationToken cancellationToken) => Task.FromResult(user.SecurityStamp);

    // ── IUserLockoutStore ────────────────────────────────────────────────────

    public Task<DateTimeOffset?> GetLockoutEndDateAsync(User user, CancellationToken cancellationToken) => Task.FromResult(user.LockoutEnd);

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

    public Task<int> GetAccessFailedCountAsync(User user, CancellationToken cancellationToken) => Task.FromResult(user.AccessFailedCount);

    public Task<bool> GetLockoutEnabledAsync(User user, CancellationToken cancellationToken) => Task.FromResult(user.LockoutEnabled);

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

    public Task<bool> GetTwoFactorEnabledAsync(User user, CancellationToken cancellationToken) => Task.FromResult(user.TwoFactorEnabled);

    // ── IUserRoleStore ───────────────────────────────────────────────────────

    public async Task AddToRoleAsync(User user, string roleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var role = await FindRoleAsync(roleName, cancellationToken)
            ?? throw new InvalidOperationException($"Role '{roleName}' does not exist.");

        await _userRoles.InsertAsync(new UserRole { UserId = user.Id, RoleId = role.Id }, cancellationToken);
    }

    public async Task RemoveFromRoleAsync(User user, string roleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var role = await FindRoleAsync(roleName, cancellationToken);
        if (role is null)
        {
            return;
        }

        await _userRoles.DeleteAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id, cancellationToken);
    }

    public async Task<IList<string>> GetRolesAsync(User user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var roleIds = await _userRoles.Select.Where(ur => ur.UserId == user.Id).ToListAsync(ur => ur.RoleId, cancellationToken);
        if (roleIds.Count == 0)
        {
            return [];
        }

        return await _roles.Select.Where(r => roleIds.Contains(r.Id)).ToListAsync(r => r.Name ?? string.Empty, cancellationToken);
    }

    public async Task<bool> IsInRoleAsync(User user, string roleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var role = await FindRoleAsync(roleName, cancellationToken);
        return role is not null && await _userRoles.Select.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id, cancellationToken);
    }

    public async Task<IList<User>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var role = await FindRoleAsync(roleName, cancellationToken);
        if (role is null)
        {
            return [];
        }

        var userIds = await _userRoles.Select.Where(ur => ur.RoleId == role.Id).ToListAsync(ur => ur.UserId, cancellationToken);
        return userIds.Count == 0 ? [] : await _users.Select.Where(u => userIds.Contains(u.Id)).ToListAsync(cancellationToken);
    }

    private async Task<ApplicationRole?> FindRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        var normalized = roleName.ToUpperInvariant();
        return await _roles.Select.Where(r => r.NormalizedName == normalized).FirstAsync(cancellationToken);
    }

    public void Dispose()
    {
        // Repositories are owned by the scoped unit of work; nothing to release here.
    }
}
