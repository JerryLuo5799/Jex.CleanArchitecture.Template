using System.Reflection;

namespace Jex.WebAPI.Infrastructure;

/// <summary>
/// Build-time OpenAPI generation (Microsoft.Extensions.ApiDescription.Server) really starts the
/// application with a no-op server. Startup side effects that need infrastructure (database
/// initialisation, migrations, seeding, background jobs) must be skipped in that mode, and
/// configuration that is normally required (secrets) is replaced by placeholders.
/// </summary>
public static class OpenApiGeneration
{
    public const string EnvironmentVariable = "OPENAPI_GENERATION";

    /// <summary>True while the app is being started by the GetDocument tool or when OPENAPI_GENERATION=1.</summary>
    public static bool IsActive { get; } =
        string.Equals(Assembly.GetEntryAssembly()?.GetName().Name, "GetDocument.Insider", StringComparison.OrdinalIgnoreCase)
        || Environment.GetEnvironmentVariable(EnvironmentVariable) == "1";

    /// <summary>Configuration overrides applied in generation mode so startup validation passes without real settings.</summary>
    public static IReadOnlyDictionary<string, string?> PlaceholderConfiguration { get; } = new Dictionary<string, string?>
    {
        ["Jwt:Secret"] = "openapi-generation-placeholder-secret-0000",
        ["Database:AutoSyncStructure"] = "false",
        ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:",
        ["Cors:AllowedOrigins:0"] = "*",
    };
}
