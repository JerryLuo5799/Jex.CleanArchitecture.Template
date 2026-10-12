using Jex.Infrastructure.Persistence.Migrations;

namespace Jex.WebAPI.Infrastructure;

/// <summary>
/// Command-line entry points that reuse the application's configuration and DI instead of starting the web server:
/// <code>
/// dotnet run --project src/Jex.WebAPI -- migrate                       # apply pending migrations, then exit
/// dotnet run --project src/Jex.WebAPI -- migrate --dry-run             # list pending migrations
/// dotnet run --project src/Jex.WebAPI -- db-script add-orders [dir]    # generate db/migrations/NNNN_add-orders.sql
/// dotnet Jex.WebAPI.dll health-probe                                   # container HEALTHCHECK: GET /health/live
/// </code>
/// </summary>
public static class CliCommands
{
    /// <summary>Returns true (and the exit code) when <paramref name="args"/> selected a command; false to start the web host.</summary>
    public static async Task<(bool Handled, int ExitCode)> TryRunAsync(WebApplication app, string[] args)
    {
        if (args.Length == 0)
        {
            return (false, 0);
        }

        switch (args[0])
        {
            case "migrate":
                return (true, await RunAsync(app, "migrate", sp =>
                {
                    var migrator = sp.GetRequiredService<DbUpMigrator>();
                    if (args.Contains("--dry-run", StringComparer.OrdinalIgnoreCase))
                    {
                        var pending = migrator.GetPending();
                        app.Logger.LogInformation("Pending migrations ({Count}): {Scripts}", pending.Count, string.Join(", ", pending));
                        return;
                    }

                    var result = migrator.Migrate();
                    app.Logger.LogInformation("Applied {Count} migration(s).", result.AppliedScripts.Count);
                }));

            case "db-script":
                if (args.Length < 2)
                {
                    app.Logger.LogError("Usage: db-script <name> [output-directory]");
                    return (true, 2);
                }

                return (true, await RunAsync(app, "db-script", sp =>
                {
                    var output = args.Length > 2 ? args[2] : Path.Combine(app.Environment.ContentRootPath, "..", "..", "db", "migrations");
                    var path = sp.GetRequiredService<MigrationScriptGenerator>().Generate(args[1], Path.GetFullPath(output));
                    if (path is null)
                    {
                        app.Logger.LogInformation("No schema changes detected.");
                    }
                    else
                    {
                        app.Logger.LogInformation("Generated {Path}", path);
                    }
                }));

            case "health-probe":
                // Used by the container HEALTHCHECK (the runtime image has no curl).
                return (true, await HealthProbeAsync(app));

            default:
                return (false, 0);
        }
    }

    private static async Task<int> HealthProbeAsync(WebApplication app)
    {
        var port = Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS")?.Split(';').FirstOrDefault() ?? "8080";
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        try
        {
            using var response = await client.GetAsync(new Uri($"http://localhost:{port}/health/live"));
            return response.IsSuccessStatusCode ? 0 : 1;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            app.Logger.LogWarning("Health probe failed: {Message}", ex.Message);
            return 1;
        }
    }

    private static Task<int> RunAsync(WebApplication app, string command, Action<IServiceProvider> action)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            action(scope.ServiceProvider);
            return Task.FromResult(0);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            app.Logger.LogError(ex, "Command '{Command}' failed.", command);
            return Task.FromResult(1);
        }
    }
}
