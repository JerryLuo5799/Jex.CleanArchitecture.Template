using Jex.Domain.Constants;
using Jex.Domain.Entities;
using Jex.Domain.Enums;
using Jex.Infrastructure.Persistence.Migrations;

namespace Jex.Infrastructure.Persistence;

/// <summary>
/// Startup database work, run from <c>Program.cs</c> (never during OpenAPI generation):
/// <list type="number">
///   <item>Development schema sync (<c>Database:AutoSyncStructure</c>), or migrations when <c>Migration:RunOnStartup</c></item>
///   <item>Idempotent role seeding (<c>Seed:Roles</c>)</item>
///   <item>Optional administrator seeding (<c>Seed:Admin</c>, disabled by default)</item>
/// </list>
/// </summary>
public sealed class ApplicationDbInitialiser(
    IFreeSql freeSql,
    IOptions<DatabaseOptions> database,
    IOptions<MigrationOptions> migration,
    IOptions<SeedOptions> seed,
    DbUpMigrator migrator,
    UserManager<User> userManager,
    RoleManager<ApplicationRole> roleManager,
    ILogger<ApplicationDbInitialiser> logger)
{
    public async Task InitialiseAsync(CancellationToken cancellationToken = default)
    {
        if (database.Value.AutoSyncStructure)
        {
            logger.LogInformation("Synchronising schema from the entity model (Database:AutoSyncStructure=true; development only).");
            freeSql.CodeFirst.SyncStructure([.. EntityRegistry.Types]);
        }
        else if (migration.Value.RunOnStartup)
        {
            migrator.Migrate();
        }

        await SeedAsync(cancellationToken);
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var roleName in seed.Value.Roles.Append(Roles.Administrator).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                (await roleManager.CreateAsync(new ApplicationRole(roleName))).ThrowIfFailed($"seed role '{roleName}'");
                logger.LogInformation("Seeded role {Role}", roleName);
            }
        }

        var admin = seed.Value.Admin;
        if (!admin.Enabled)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (await userManager.FindByEmailAsync(admin.Email!) is not null)
        {
            logger.LogInformation("Administrator {Email} already exists; skipping seed.", admin.Email);
            return;
        }

        var user = new User
        {
            UserName = admin.Email,
            Email = admin.Email,
            EmailConfirmed = true,
            FirstName = admin.FirstName,
            LastName = admin.LastName,
            Status = UserStatus.Active,
        };

        (await userManager.CreateAsync(user, admin.Password!)).ThrowIfFailed("seed administrator");
        (await userManager.AddToRoleAsync(user, Roles.Administrator)).ThrowIfFailed("assign administrator role");
        logger.LogInformation("Seeded administrator {Email}", admin.Email);
    }
}

internal static class IdentityResultExtensions
{
    public static void ThrowIfFailed(this IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to {operation}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
        }
    }
}
