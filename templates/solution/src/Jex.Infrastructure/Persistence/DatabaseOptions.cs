using System.ComponentModel.DataAnnotations;

namespace Jex.Infrastructure.Persistence;

/// <summary>Bound from the <c>Database</c> section.</summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>FreeSql provider name: Sqlite, PostgreSQL, SqlServer or MySql.</summary>
    [Required]
    public string Provider { get; set; } = "Sqlite";

    /// <summary>
    /// Development convenience: synchronise the schema from the entity model at startup (and lazily).
    /// Must be false in production, where versioned migrations are the only way to change the schema (ADR-0008).
    /// </summary>
    public bool AutoSyncStructure { get; set; }

    public FreeSql.DataType DataType => Enum.Parse<FreeSql.DataType>(Provider, ignoreCase: true);
}

/// <summary>Bound from the <c>Migration</c> section.</summary>
public sealed class MigrationOptions
{
    public const string SectionName = "Migration";

    /// <summary>
    /// Apply pending migrations when the application starts. Only safe for single-instance deployments;
    /// the recommended path is the separate <c>migrate</c> command run by the deployment pipeline.
    /// </summary>
    public bool RunOnStartup { get; set; }

    /// <summary>
    /// Connection string of a scratch database used as the baseline when generating migration scripts
    /// (<c>db-script</c> command). Not needed for SQLite, which uses a temporary file.
    /// </summary>
    public string? BaselineConnection { get; set; }
}
