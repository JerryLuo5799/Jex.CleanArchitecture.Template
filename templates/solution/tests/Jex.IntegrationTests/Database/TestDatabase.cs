namespace Jex.IntegrationTests.Database;

/// <summary>Picks the test database from <c>TEST_DATABASE</c> (see <see cref="ITestDatabase"/>).</summary>
public static class TestDatabase
{
    public const string EnvironmentVariable = "TEST_DATABASE";

    public static ITestDatabase Create()
    {
        var mode = (Environment.GetEnvironmentVariable(EnvironmentVariable) ?? "sqlite").Trim().ToLowerInvariant();

        switch (mode)
        {
            case "sqlite":
                return new SqliteTestDatabase();

            case "provider":
            case "auto":
#if (UseSqlite)
                // The project's provider is SQLite; nothing to start.
                return new SqliteTestDatabase();
#else
                try
                {
                    return ContainerTestDatabase.Start();
                }
                catch (Exception ex) when (mode == "auto")
                {
                    Console.Error.WriteLine($"TEST_DATABASE=auto: container unavailable ({ex.Message}); falling back to SQLite.");
                    return new SqliteTestDatabase();
                }
#endif

            default:
                throw new InvalidOperationException($"{EnvironmentVariable} must be sqlite, provider or auto (was '{mode}').");
        }
    }
}
