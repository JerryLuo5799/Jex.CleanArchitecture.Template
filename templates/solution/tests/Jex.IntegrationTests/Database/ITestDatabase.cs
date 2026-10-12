namespace Jex.IntegrationTests.Database;

/// <summary>
/// The database an integration-test host runs against. Selected by <see cref="TestDatabase.Create"/>
/// from the <c>TEST_DATABASE</c> environment variable:
/// <list type="bullet">
///   <item><c>sqlite</c> (default): a temporary SQLite file, no infrastructure needed.</item>
///   <item><c>provider</c>: the project's real provider in a Testcontainers container; fails if Docker is unavailable (CI).</item>
///   <item><c>auto</c>: like <c>provider</c> but falls back to SQLite when no container can be started (local only).</item>
/// </list>
/// </summary>
public interface ITestDatabase : IDisposable
{
    string Provider { get; }

    string ConnectionString { get; }
}
