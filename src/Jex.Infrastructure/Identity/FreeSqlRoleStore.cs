using FreeSql;
using Jex.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Jex.Infrastructure.Identity;

/// <summary>
/// FreeSQL-backed implementation of <see cref="IRoleStore{TRole}"/>.
/// Provides ASP.NET Core Identity with role persistence via FreeSQL.
/// </summary>
public sealed class FreeSqlRoleStore(IFreeSql freeSql) : IRoleStore<ApplicationRole>
{
    public async Task<IdentityResult> CreateAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var id = await freeSql.Insert(role).ExecuteIdentityAsync(cancellationToken);
        role.Id = id;
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> UpdateAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await freeSql.Update<ApplicationRole>().SetSource(role).ExecuteAffrowsAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await freeSql.Delete<ApplicationRole>().Where(r => r.Id == role.Id).ExecuteAffrowsAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public Task<string> GetRoleIdAsync(ApplicationRole role, CancellationToken cancellationToken)
        => Task.FromResult(role.Id.ToString());

    public Task<string?> GetRoleNameAsync(ApplicationRole role, CancellationToken cancellationToken)
        => Task.FromResult(role.Name);

    public Task SetRoleNameAsync(ApplicationRole role, string? roleName, CancellationToken cancellationToken)
    {
        role.Name = roleName;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedRoleNameAsync(ApplicationRole role, CancellationToken cancellationToken)
        => Task.FromResult(role.NormalizedName);

    public Task SetNormalizedRoleNameAsync(ApplicationRole role, string? normalizedName, CancellationToken cancellationToken)
    {
        role.NormalizedName = normalizedName;
        return Task.CompletedTask;
    }

    public async Task<ApplicationRole?> FindByIdAsync(string roleId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!long.TryParse(roleId, out var id)) return null;
        return await freeSql.Select<ApplicationRole>().Where(r => r.Id == id).FirstAsync(cancellationToken);
    }

    public async Task<ApplicationRole?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await freeSql.Select<ApplicationRole>()
            .Where(r => r.NormalizedName == normalizedRoleName)
            .FirstAsync(cancellationToken);
    }

    public void Dispose() { }
}
