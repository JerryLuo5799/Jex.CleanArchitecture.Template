namespace Jex.IntegrationTests.Database;

/// <summary>One temporary SQLite file per host; deleted on dispose.</summary>
public sealed class SqliteTestDatabase : ITestDatabase
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"TPLNAMELOWER_test_{Guid.NewGuid():N}.db");

    public string Provider => "Sqlite";

    public string ConnectionString => $"Data Source={_path}";

    public void Dispose()
    {
        foreach (var path in new[] { _path, _path + "-shm", _path + "-wal", _path + "-journal" })
        {
            try
            {
                File.Delete(path);
            }
            catch (IOException)
            {
                // SQLite may still hold the file briefly; the OS temp folder is cleaned eventually.
            }
        }
    }
}
