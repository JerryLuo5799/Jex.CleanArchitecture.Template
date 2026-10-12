using System.Reflection;
using DbUp;
using DbUp.Builder;
using DbUp.Engine.Output;

namespace Jex.Infrastructure.Persistence.Migrations;

/// <summary>
/// Applies versioned SQL migrations with DbUp. At runtime the scripts are embedded resources named
/// <c>NNNN_name.sql</c> (from <c>db/migrations</c>); tooling can also read them straight from that
/// directory so freshly generated, not-yet-embedded scripts count. Executed scripts are journaled in
/// the <c>SchemaVersions</c> table, so re-running is a no-op.
/// Transactions: one per script where the provider supports transactional DDL (SQLite, PostgreSQL,
/// SQL Server); MySQL DDL is not transactional, so scripts must be written to be re-runnable.
/// Concurrency: run migrations from a single deployment step; the journal is not a distributed lock.
/// </summary>
public sealed class DbUpMigrator(DatabaseOptions database, string connectionString, Assembly scriptsAssembly, ILogger<DbUpMigrator> logger)
{
    /// <summary>Applies pending embedded scripts to the configured database. Throws when a script fails; later scripts are not executed.</summary>
    public MigrationResult Migrate()
    {
        var engine = Build(connectionString, EmbeddedScripts).Build();

        var pending = engine.GetScriptsToExecute().Select(s => s.Name).ToList();
        if (pending.Count == 0)
        {
            logger.LogInformation("Database is up to date; no migrations to apply.");
            return new MigrationResult([]);
        }

        logger.LogInformation("Applying {Count} migration(s): {Scripts}", pending.Count, string.Join(", ", pending));

        var result = engine.PerformUpgrade();
        if (!result.Successful)
        {
            throw new InvalidOperationException(
                $"Migration '{result.ErrorScript?.Name}' failed: {result.Error?.Message}. Later scripts were not applied.",
                result.Error);
        }

        return new MigrationResult(result.Scripts.Select(s => s.Name).ToList());
    }

    /// <summary>Names of embedded scripts that have not been applied to the configured database yet.</summary>
    public IReadOnlyList<string> GetPending()
        => Build(connectionString, EmbeddedScripts).Build().GetScriptsToExecute().Select(s => s.Name).ToList();

    /// <summary>Applies every script in <paramref name="scriptsDirectory"/> to another database (migration baseline, tests).</summary>
    public MigrationResult ApplyDirectoryTo(string scriptsDirectory, string targetConnectionString)
    {
        if (!Directory.Exists(scriptsDirectory))
        {
            return new MigrationResult([]);
        }

        var result = Build(targetConnectionString, b => b.WithScriptsFromFileSystem(scriptsDirectory, IsMigrationScript)).Build().PerformUpgrade();
        if (!result.Successful)
        {
            throw new InvalidOperationException($"Migration '{result.ErrorScript?.Name}' failed: {result.Error?.Message}", result.Error);
        }

        return new MigrationResult(result.Scripts.Select(s => s.Name).ToList());
    }

    /// <summary>Names of scripts in <paramref name="scriptsDirectory"/> not yet applied to <paramref name="targetConnectionString"/>.</summary>
    public IReadOnlyList<string> GetPendingFromDirectory(string scriptsDirectory, string targetConnectionString)
        => Build(targetConnectionString, b => b.WithScriptsFromFileSystem(scriptsDirectory, IsMigrationScript))
            .Build().GetScriptsToExecute().Select(s => s.Name).ToList();

    private UpgradeEngineBuilder EmbeddedScripts(UpgradeEngineBuilder builder)
        => builder.WithScriptsEmbeddedInAssembly(scriptsAssembly, IsMigrationScript);

    /// <summary>Scripts are <c>NNNN_name.sql</c>; the same name is used for embedded and file-system sources so the journal matches.</summary>
    private static bool IsMigrationScript(string name)
    {
        var fileName = Path.GetFileName(name);
        return fileName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) && fileName.Length > 5 && char.IsAsciiDigit(fileName[0]);
    }

    private UpgradeEngineBuilder Build(string target, Func<UpgradeEngineBuilder, UpgradeEngineBuilder> scripts)
    {
        var builder = scripts(CreateForProvider(database.DataType, target)).LogTo(new LoggerAdapter(logger));

        return database.DataType == DataType.MySql
            ? builder.WithoutTransaction()
            : builder.WithTransactionPerScript();
    }

    private static UpgradeEngineBuilder CreateForProvider(DataType dataType, string connectionString) =>
        dataType switch
        {
            DataType.Sqlite => DeployChanges.To.SqliteDatabase(connectionString),
            _ => throw new NotSupportedException($"No DbUp provider is configured for '{dataType}'. Add the matching dbup-* package and a case here."),
        };

#pragma warning disable CA2254 // DbUp supplies the message templates.
    private sealed class LoggerAdapter(ILogger logger) : IUpgradeLog
    {
        public void LogTrace(string format, params object[] args) => logger.LogTrace(format, args);

        public void LogDebug(string format, params object[] args) => logger.LogDebug(format, args);

        public void LogInformation(string format, params object[] args) => logger.LogInformation(format, args);

        public void LogWarning(string format, params object[] args) => logger.LogWarning(format, args);

        public void LogError(string format, params object[] args) => logger.LogError(format, args);

        public void LogError(Exception ex, string format, params object[] args) => logger.LogError(ex, format, args);
    }
#pragma warning restore CA2254
}

/// <summary>Scripts applied by a migration run.</summary>
public sealed record MigrationResult(IReadOnlyList<string> AppliedScripts);
