#if (!UseSqlite)
using DotNet.Testcontainers.Containers;
#if (UsePostgreSQL)
using Testcontainers.PostgreSql;
#endif
#if (UseSqlServer)
using Testcontainers.MsSql;
#endif
#if (UseMySql)
using Testcontainers.MySql;
#endif

namespace Jex.IntegrationTests.Database;

/// <summary>The project's real database provider running in a Testcontainers container (one per host).</summary>
public sealed class ContainerTestDatabase : ITestDatabase
{
    private readonly IDatabaseContainer _container;

    private ContainerTestDatabase(IDatabaseContainer container, string provider)
    {
        _container = container;
        Provider = provider;
    }

    public string Provider { get; }

    public string ConnectionString => _container.GetConnectionString();

    public static ContainerTestDatabase Start()
    {
#if (UsePostgreSQL)
        var container = new PostgreSqlBuilder("postgres:17").Build();
        var provider = "PostgreSQL";
#endif
#if (UseSqlServer)
        var container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
        var provider = "SqlServer";
#endif
#if (UseMySql)
        var container = new MySqlBuilder("mysql:8.4").Build();
        var provider = "MySql";
#endif
        container.StartAsync().GetAwaiter().GetResult();
#if (UseSqlServer)
        WaitForTempDb(container.GetConnectionString());
#endif
        Console.Error.WriteLine($"Integration tests use {provider} in container {container.Name}.");
        return new ContainerTestDatabase(container, provider);
    }

    public void Dispose() => _container.DisposeAsync().AsTask().GetAwaiter().GetResult();
#if (UseSqlServer)

    // SQL Server reports ready before tempdb is online; early queries can then fail with
    // "Could not find database ID 2, name 'tempdb'". Probe it before handing out the connection string.
    private static void WaitForTempDb(string connectionString)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM tempdb.sys.objects";
                command.ExecuteScalar();
                return;
            }
            catch (Microsoft.Data.SqlClient.SqlException) when (attempt < 60)
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }
    }
#endif
}
#endif
