using Jex.Infrastructure.Persistence.Migrations;

namespace Jex.IntegrationTests.Persistence;

/// <summary>
/// Migration tooling end to end on SQLite: generate the initial script from an empty baseline,
/// apply it to an empty database, verify re-running is a no-op and that the model matches afterwards.
/// </summary>
public sealed class MigrationTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly string _workDir = Directory.CreateTempSubdirectory("migrations-").FullName;

    [Fact]
    public void GenerateThenMigrate_EmptyDatabase_CreatesSchemaOnce()
    {
        // This end-to-end check uses throw-away SQLite files; other providers need a scratch server
        // (Migration:BaselineConnection) and are exercised by `task db:script` against real infrastructure.
        Assert.SkipUnless(DbUpMigrator.Supports(FreeSql.DataType.Sqlite), "Project provider is not SQLite.");

        using var scope = factory.Services.CreateScope();
        var generator = scope.ServiceProvider.GetRequiredService<MigrationScriptGenerator>();
        var migrator = scope.ServiceProvider.GetRequiredService<DbUpMigrator>();

        // 1. Initial script from an empty baseline.
        var scriptPath = generator.Generate("initial", _workDir);
        scriptPath.ShouldNotBeNull();
        Path.GetFileName(scriptPath).ShouldBe("0001_initial.sql");
        File.ReadAllText(scriptPath).ShouldContain("CREATE TABLE", Case.Insensitive);

        // 2. Apply to a fresh database.
        var targetDb = Path.Combine(_workDir, "target.db");
        var target = $"Data Source={targetDb}";
        var applied = migrator.ApplyDirectoryTo(_workDir, target);
        applied.AppliedScripts.ShouldBe(["0001_initial.sql"]);

        // 3. Re-running applies nothing.
        migrator.GetPendingFromDirectory(_workDir, target).ShouldBeEmpty();
        migrator.ApplyDirectoryTo(_workDir, target).AppliedScripts.ShouldBeEmpty();

        // 4. The migrated schema matches the entity model (no second script needed).
        generator.Generate("noop", _workDir).ShouldBeNull();
    }

    [Fact]
    public void Generate_InvalidName_Throws()
    {
        Assert.SkipUnless(DbUpMigrator.Supports(FreeSql.DataType.Sqlite), "Project provider is not SQLite.");

        using var scope = factory.Services.CreateScope();
        var generator = scope.ServiceProvider.GetRequiredService<MigrationScriptGenerator>();

        Should.Throw<ArgumentException>(() => generator.Generate("bad name!", _workDir));
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_workDir, recursive: true);
        }
        catch (IOException)
        {
            // Temp folder; best effort.
        }
    }
}
