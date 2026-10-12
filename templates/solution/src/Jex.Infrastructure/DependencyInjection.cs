using Jex.Application.Common.Interfaces;
using Jex.Domain.Entities;
using Jex.Infrastructure.Identity;
using Jex.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jex.Infrastructure;

/// <summary>
/// Infrastructure layer registration: FreeSql (Code First), ASP.NET Core Identity on FreeSql stores,
/// JWT options and repositories.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IFreeSql>(sp => BuildFreeSql(configuration, sp.GetRequiredService<ILoggerFactory>()));

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

        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }

    private static IFreeSql BuildFreeSql(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

        var dataType = Enum.Parse<DataType>(configuration["Database:Provider"] ?? "Sqlite", ignoreCase: true);

        // Schema auto-sync is a development convenience only; production uses versioned migrations (ADR-0008).
        var autoSync = configuration.GetValue("Database:AutoSyncStructure", true);

        var sqlLogger = loggerFactory.CreateLogger("FreeSql");

        var freeSql = new FreeSqlBuilder()
            .UseConnectionString(dataType, connectionString)
            .UseAutoSyncStructure(autoSync)
            .UseMonitorCommand(
                executing: _ => { },
                executed: (cmd, log) =>
                {
                    if (sqlLogger.IsEnabled(LogLevel.Debug))
                    {
                        sqlLogger.LogDebug("SQL={Sql} {Log}", cmd.CommandText, log);
                    }
                })
            .Build();

        ConfigureEntities(freeSql);

        return freeSql;
    }

    private static void ConfigureEntities(IFreeSql freeSql)
    {
        freeSql.CodeFirst.ConfigEntity<User>(e =>
        {
            e.Property(u => u.Id).IsPrimary(true).IsIdentity(true);
            e.Property(u => u.NormalizedEmail).StringLength(256);
            e.Property(u => u.NormalizedUserName).StringLength(256);
            e.Index("uk_users_normalized_email", nameof(User.NormalizedEmail), true);
            e.Index("uk_users_normalized_user_name", nameof(User.NormalizedUserName), true);
        });

        freeSql.CodeFirst.ConfigEntity<ApplicationRole>(e =>
        {
            e.Property(r => r.Id).IsPrimary(true).IsIdentity(true);
            e.Property(r => r.NormalizedName).StringLength(256);
            e.Index("uk_roles_normalized_name", nameof(ApplicationRole.NormalizedName), true);
        });

        freeSql.CodeFirst.ConfigEntity<UserRole>(e =>
        {
            e.Property(ur => ur.UserId).IsPrimary(true);
            e.Property(ur => ur.RoleId).IsPrimary(true);
        });
    }
}
