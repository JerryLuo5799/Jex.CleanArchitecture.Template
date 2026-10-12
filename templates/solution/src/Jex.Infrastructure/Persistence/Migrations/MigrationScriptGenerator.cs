using System.Globalization;
using System.Text.RegularExpressions;
using Jex.Infrastructure.Persistence.Configurations;

namespace Jex.Infrastructure.Persistence.Migrations;

/// <summary>
/// Generates the next migration script by comparing the entity model with a <b>baseline</b> database
/// that has only the committed migrations applied (never the auto-synced development database, which
/// would always look up to date). For SQLite the baseline is a temporary file; other providers need
/// <c>Migration:BaselineConnection</c> pointing at a scratch database.
/// </summary>
public sealed partial class MigrationScriptGenerator(
    DatabaseOptions database,
    MigrationOptions migration,
    DbUpMigrator migrator,
    ILogger<MigrationScriptGenerator> logger)
{
    /// <summary>Writes <c>{outputDirectory}/{NNNN}_{name}.sql</c> and returns its path, or <c>null</c> when the schema is unchanged.</summary>
    public string? Generate(string name, string outputDirectory)
    {
        if (!SafeName().IsMatch(name))
        {
            throw new ArgumentException("Migration name must be letters, digits, '-' or '_' (e.g. add-orders).", nameof(name));
        }

        var (baselineConnection, cleanup) = CreateBaseline();
        try
        {
            // Baseline = committed scripts from the directory (not embedded resources, which lag until the next build).
            migrator.ApplyDirectoryTo(outputDirectory, baselineConnection);

            var baseline = FreeSqlFactory.Create(database.DataType, baselineConnection, autoSyncStructure: false);
            var ddl = baseline.CodeFirst.GetComparisonDDLStatements([.. EntityConfigurations.EntityTypes]);
            baseline.Dispose();

            if (string.IsNullOrWhiteSpace(ddl))
            {
                logger.LogInformation("Entity model matches the migrated schema; no script generated.");
                return null;
            }

            Directory.CreateDirectory(outputDirectory);
            var next = NextNumber(outputDirectory);
            var path = Path.Combine(outputDirectory, $"{next:0000}_{name}.sql");

            var header = $"-- {next:0000}_{name} | generated {DateTime.UtcNow.ToString("u", CultureInfo.InvariantCulture)} | provider {database.Provider}\n" +
                         "-- Review before committing. Scripts run once, in order, journaled in SchemaVersions.\n\n";
            File.WriteAllText(path, header + ddl.Trim() + "\n");

            logger.LogInformation("Wrote migration {Path}", path);
            return path;
        }
        finally
        {
            cleanup();
        }
    }

    private (string ConnectionString, Action Cleanup) CreateBaseline()
    {
        if (database.DataType == DataType.Sqlite)
        {
            var file = Path.Combine(Path.GetTempPath(), $"migration-baseline-{Guid.NewGuid():N}.db");
            return ($"Data Source={file}", () =>
            {
                foreach (var suffix in new[] { "", "-journal", "-wal", "-shm" })
                {
                    try
                    {
                        File.Delete(file + suffix);
                    }
                    catch (IOException)
                    {
                        // Best effort; the OS temp folder gets cleaned eventually.
                    }
                }
            }
            );
        }

        var baseline = migration.BaselineConnection
            ?? throw new InvalidOperationException(
                "Migration:BaselineConnection must point at an empty scratch database for this provider (SQLite needs none).");

        return (baseline, () => { });
    }

    private static int NextNumber(string outputDirectory)
    {
        var last = Directory.EnumerateFiles(outputDirectory, "*.sql")
            .Select(Path.GetFileName)
            .Select(f => ScriptNumber().Match(f!))
            .Where(m => m.Success)
            .Select(m => int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture))
            .DefaultIfEmpty(0)
            .Max();

        return last + 1;
    }

    [GeneratedRegex("^[A-Za-z0-9_-]+$")]
    private static partial Regex SafeName();

    [GeneratedRegex(@"^(\d{4})_")]
    private static partial Regex ScriptNumber();
}
