using System.Reflection;
using Jex.Application.Common.Interfaces;
using Jex.Domain.Entities;
using Jex.Infrastructure.HealthChecks;
using Jex.Infrastructure.Identity;
using Jex.Infrastructure.Persistence;
using Jex.Infrastructure.Persistence.Migrations;
using IUnitOfWork = Jex.Application.Common.Interfaces.IUnitOfWork;

namespace Jex.Infrastructure;

/// <summary>
/// Infrastructure layer registration: FreeSql, unit of work, repositories, ASP.NET Core Identity on
/// FreeSql stores, JWT options, migrations and health checks.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Registers persistence, identity and migration services.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration (connection strings and option sections).</param>
    /// <param name="migrationScriptsAssembly">Assembly that embeds <c>Migrations/*.sql</c> (the WebAPI project).</param>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        Assembly migrationScriptsAssembly)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddOptions<DatabaseOptions>().BindConfiguration(DatabaseOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<MigrationOptions>().BindConfiguration(MigrationOptions.SectionName);
        services.AddOptions<SeedOptions>().BindConfiguration(SeedOptions.SectionName).ValidateOnStart();
        services.AddSingleton<IValidateOptions<SeedOptions>, SeedOptionsValidator>();
        services.AddOptions<JwtOptions>().BindConfiguration(JwtOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();

        // One IFreeSql per application; transactions are scoped through UnitOfWorkManager.
        services.AddSingleton<IFreeSql>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            return FreeSqlFactory.Create(
                options.DataType,
                ConnectionString(configuration),
                options.AutoSyncStructure,
                sp.GetRequiredService<ILoggerFactory>().CreateLogger("FreeSql"));
        });
        services.AddScoped<UnitOfWorkManager>();
        services.AddScoped<IUnitOfWork, FreeSqlUnitOfWork>();
        services.AddScoped<AuditStamper>();

        // Repositories are registered by convention: every class ending in "Repository" is registered against
        // the Application interfaces it implements (e.g. TodoListRepository -> ITodoListRepository).
        RegisterRepositoriesByConvention(services);

        services.AddIdentityCore<User>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<ApplicationRole>()
            .AddUserStore<FreeSqlUserStore>()
            .AddRoleStore<FreeSqlRoleStore>();

        services.AddScoped<TokenService>();
        services.AddScoped<IIdentityService, IdentityService>();

        services.AddScoped(sp => new DbUpMigrator(
            sp.GetRequiredService<IOptions<DatabaseOptions>>().Value,
            ConnectionString(configuration),
            migrationScriptsAssembly,
            sp.GetRequiredService<ILogger<DbUpMigrator>>()));
        services.AddScoped(sp => new MigrationScriptGenerator(
            sp.GetRequiredService<IOptions<DatabaseOptions>>().Value,
            sp.GetRequiredService<IOptions<MigrationOptions>>().Value,
            sp.GetRequiredService<DbUpMigrator>(),
            sp.GetRequiredService<ILogger<MigrationScriptGenerator>>()));
        services.AddScoped<ApplicationDbInitialiser>();

        services.AddHealthChecks()
            .AddCheck<FreeSqlHealthCheck>("database", tags: ["ready"]);

        return services;
    }

    /// <summary>Runs schema sync/migrations and seeding. Call once at startup (not during OpenAPI generation).</summary>
    public static async Task InitialiseDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ApplicationDbInitialiser>().InitialiseAsync(cancellationToken);
    }

    private static void RegisterRepositoriesByConvention(IServiceCollection services)
    {
        var applicationNamespace = typeof(IUserRepository).Namespace!;

        var implementations = typeof(DependencyInjection).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.Name.EndsWith("Repository", StringComparison.Ordinal));

        foreach (var implementation in implementations)
        {
            foreach (var contract in implementation.GetInterfaces().Where(i => !i.IsGenericType && i.Namespace == applicationNamespace))
            {
                services.AddScoped(contract, implementation);
            }
        }
    }

    private static string ConnectionString(IConfiguration configuration)
        => configuration.GetConnectionString("DefaultConnection")
           ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");
}
