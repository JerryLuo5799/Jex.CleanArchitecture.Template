using Jex.Domain.Entities;

namespace Jex.Infrastructure.Identity;

/// <summary>FreeSql-backed Identity role store; writes join the ambient unit of work.</summary>
public sealed class FreeSqlRoleStore : IRoleStore<ApplicationRole>
{
    private readonly DefaultRepository<ApplicationRole, long> _roles;
    private readonly IdentityErrorDescriber _errors;

    public FreeSqlRoleStore(IFreeSql freeSql, UnitOfWorkManager unitOfWorkManager, IdentityErrorDescriber errors)
    {
        _roles = new DefaultRepository<ApplicationRole, long>(freeSql, unitOfWorkManager);
        _errors = errors;
    }

    public async Task<IdentityResult> CreateAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _roles.InsertAsync(role, cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> UpdateAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var expectedStamp = role.ConcurrencyStamp;
        role.ConcurrencyStamp = Guid.NewGuid().ToString();

        var affected = await _roles.UpdateDiy
            .SetSource(role)
            .Where(r => r.ConcurrencyStamp == expectedStamp)
            .ExecuteAffrowsAsync(cancellationToken);

        if (affected == 0)
        {
            role.ConcurrencyStamp = expectedStamp;
            return IdentityResult.Failed(_errors.ConcurrencyFailure());
        }

        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _roles.DeleteAsync(r => r.Id == role.Id, cancellationToken);
        return IdentityResult.Success;
    }

    public Task<string> GetRoleIdAsync(ApplicationRole role, CancellationToken cancellationToken)
        => Task.FromResult(role.Id.ToString(System.Globalization.CultureInfo.InvariantCulture));

    public Task<string?> GetRoleNameAsync(ApplicationRole role, CancellationToken cancellationToken) => Task.FromResult(role.Name);

    public Task SetRoleNameAsync(ApplicationRole role, string? roleName, CancellationToken cancellationToken)
    {
        role.Name = roleName;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedRoleNameAsync(ApplicationRole role, CancellationToken cancellationToken) => Task.FromResult(role.NormalizedName);

    public Task SetNormalizedRoleNameAsync(ApplicationRole role, string? normalizedName, CancellationToken cancellationToken)
    {
        role.NormalizedName = normalizedName;
        return Task.CompletedTask;
    }

    public async Task<ApplicationRole?> FindByIdAsync(string roleId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return long.TryParse(roleId, System.Globalization.CultureInfo.InvariantCulture, out var id)
            ? await _roles.Select.Where(r => r.Id == id).FirstAsync(cancellationToken)
            : null;
    }

    public async Task<ApplicationRole?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _roles.Select.Where(r => r.NormalizedName == normalizedRoleName).FirstAsync(cancellationToken);
    }

    public void Dispose()
    {
        // Repositories are owned by the scoped unit of work; nothing to release here.
    }
}
