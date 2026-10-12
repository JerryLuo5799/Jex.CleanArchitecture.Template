// .NET Aspire orchestration for local development: `task run:aspire` (dashboard URL is printed on start).
// The database resource is named "DefaultConnection" so Aspire injects ConnectionStrings__DefaultConnection
// into the API; no code in Infrastructure has to know about Aspire.
var builder = DistributedApplication.CreateBuilder(args);

#if (UsePostgreSQL)
var database = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("DefaultConnection", databaseName: "TPLNAMELOWER");
#endif
#if (UseSqlServer)
var database = builder.AddSqlServer("sqlserver")
    .WithDataVolume()
    .AddDatabase("DefaultConnection", databaseName: "TPLNAMELOWER");
#endif
#if (UseMySql)
var database = builder.AddMySql("mysql")
    .WithDataVolume()
    .AddDatabase("DefaultConnection", databaseName: "TPLNAMELOWER");
#endif

// Path overload: the generated Projects.* type name depends on the solution name (dots become underscores).
var api = builder.AddProject("api", "../Jex.WebAPI/Jex.WebAPI.csproj")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health/ready");

#if (!UseSqlite)
api.WithReference(database).WaitFor(database);
#endif

builder.Build().Run();
