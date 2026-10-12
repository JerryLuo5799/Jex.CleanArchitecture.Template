using Jex.Domain.Common;
using Jex.Infrastructure.Persistence.Configurations;

namespace Jex.Infrastructure.Persistence;

/// <summary>
/// Builds the single <see cref="IFreeSql"/> instance (and the throw-away instances used by the
/// migration tooling) with every <see cref="IEntityConfiguration"/> and the global filters applied.
/// </summary>
public static class FreeSqlFactory
{
    public const string SoftDeleteFilterName = "SoftDelete";

    // All timestamps are UTC DateTime stored without a time zone on every provider. Npgsql 6+ refuses to
    // write a UTC DateTime into "timestamp without time zone", so keep its pre-6 behaviour. The switch is
    // read once by Npgsql, so it must be set before the first connection; it is a no-op for other providers.
    static FreeSqlFactory() => AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

    /// <summary>Forces the static initialiser to run; call before any database access (startup, migrations).</summary>
    public static void EnsureDriverDefaults()
    {
    }

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

        EntityConfigurations.ApplyAll(freeSql.CodeFirst);
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
}
