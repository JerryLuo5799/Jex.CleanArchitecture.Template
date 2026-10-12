using Jex.Domain.Common;
using Jex.Domain.Entities;

namespace Jex.Infrastructure.Persistence;

/// <summary>
/// Builds the single <see cref="IFreeSql"/> instance (and the throw-away instances used by the
/// migration tooling) with the entity configuration and global filters that every query relies on.
/// </summary>
public static class FreeSqlFactory
{
    public const string SoftDeleteFilterName = "SoftDelete";

    public static IFreeSql Create(DataType dataType, string connectionString, bool autoSyncStructure, ILogger? sqlLogger = null)
    {
        var builder = new FreeSqlBuilder()
            .UseConnectionString(dataType, connectionString)
            .UseAutoSyncStructure(autoSyncStructure);

        if (sqlLogger is not null)
        {
            builder = builder.UseMonitorCommand(
                executing: _ => { },
                executed: (cmd, log) =>
                {
                    if (sqlLogger.IsEnabled(LogLevel.Debug))
                    {
                        sqlLogger.LogDebug("SQL={Sql} {Log}", cmd.CommandText, log);
                    }
                });
        }

        var freeSql = builder.Build();

        ConfigureEntities(freeSql);
        freeSql.GlobalFilter.Apply<ISoftDelete>(SoftDeleteFilterName, e => !e.IsDeleted);

        // Every timestamp is stored as UTC; providers without a time zone (SQLite) read it back as
        // Unspecified, which would serialize without the 'Z'. Restore the kind on the way out.
        freeSql.Aop.AuditDataReader += (_, e) =>
        {
            if (e.Value is DateTime { Kind: DateTimeKind.Unspecified } dateTime)
            {
                e.Value = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            }
        };

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
