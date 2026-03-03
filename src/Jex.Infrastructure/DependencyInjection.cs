using FreeSql;
using Jex.Application.Common.Interfaces;
using Jex.Domain.Entities;
using Jex.Infrastructure.Identity;
using Jex.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jex.Infrastructure;

/// <summary>
/// Infrastructure layer service registration.
/// Configures FreeSQL (Code First), ASP.NET Core Identity with FreeSQL stores, and registers repositories.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<string, long>? onSqlExecuted = null)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=jex.db";

        var dataType = Enum.Parse<DataType>(
            configuration["Database:Provider"] ?? "Sqlite",
            ignoreCase: true);

        var freeSql = new FreeSqlBuilder()
            .UseConnectionString(dataType, connectionString)
            // Code First: automatically sync entity schemas to the database on startup.
            .UseAutoSyncStructure(true)
            .Build();

        // ── FreeSQL entity key configuration ─────────────────────────────────
        // User.Id is a long inherited from IdentityUser<long>; mark it as auto-identity.
        freeSql.CodeFirst.ConfigEntity<User>(e =>
        {
            e.Property(u => u.Id).IsPrimary(true).IsIdentity(true);
        });

        // ApplicationRole.Id: same treatment.
        freeSql.CodeFirst.ConfigEntity<ApplicationRole>(e =>
        {
            e.Property(r => r.Id).IsPrimary(true).IsIdentity(true);
        });

        // UserRole has a composite primary key (UserId + RoleId).
        freeSql.CodeFirst.ConfigEntity<UserRole>(e =>
        {
            e.Property(ur => ur.UserId).IsPrimary(true);
            e.Property(ur => ur.RoleId).IsPrimary(true);
        });

        services.AddSingleton(freeSql);

        // ── SQL command monitoring ───────────────────────────────────────────
        if (onSqlExecuted is not null)
        {
            freeSql.Aop.CommandAfter += (_, e) =>
                onSqlExecuted(e.Command.CommandText, e.ElapsedMilliseconds);
        }

        // ── ASP.NET Core Identity (FreeSQL-backed stores) ────────────────────
        services.AddIdentityCore<User>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;
            })
            .AddRoles<ApplicationRole>()
            .AddUserStore<FreeSqlUserStore>()
            .AddRoleStore<FreeSqlRoleStore>()
            .AddDefaultTokenProviders();

        services.AddScoped<TokenService>();
        services.AddScoped<IIdentityService, IdentityService>();

        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}
