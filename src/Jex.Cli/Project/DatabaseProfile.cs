using System.Text.Json;

namespace Jex.Cli.Project;

/// <summary>
/// docker compose profile for the configured database. Resolved on demand (only by <c>jex infra</c>) from the
/// API's <c>appsettings.json</c>, so template sources and unusual configurations never block other commands.
/// </summary>
public static class DatabaseProfile
{
    public const string None = "none";

    public static readonly string[] Profiles = [None, "postgresql", "sqlserver", "mysql"];

    private const string TemplatePlaceholder = "FREESQL_PROVIDER";

    public static string Resolve(string projectRoot, string apiProject)
    {
        var apiDirectory = Path.GetDirectoryName(Path.Combine(projectRoot, apiProject)) ?? projectRoot;
        var settings = Path.Combine(apiDirectory, "appsettings.json");
        if (!File.Exists(settings))
        {
            throw new JexException($"{settings} not found. Pass --profile <{string.Join('|', Profiles)}>.");
        }

        string? provider = null;
        try
        {
            using var document = JsonDocument.Parse(
                File.ReadAllText(settings),
                new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("Database", out var database)
                && database.ValueKind == JsonValueKind.Object
                && database.TryGetProperty("Provider", out var value)
                && value.ValueKind == JsonValueKind.String)
            {
                provider = value.GetString();
            }
        }
        catch (JsonException ex)
        {
            throw new JexException($"{settings} is not valid JSON ({ex.Message}). Pass --profile <{string.Join('|', Profiles)}>.", ex);
        }

        return FromProvider(provider, settings);
    }

    public static string FromProvider(string? provider, string source) => provider?.ToLowerInvariant() switch
    {
        "sqlite" => None,
        "postgresql" => "postgresql",
        "sqlserver" => "sqlserver",
        "mysql" => "mysql",
        _ when provider == TemplatePlaceholder => throw new JexException(
            $"{source} still contains the template placeholder {TemplatePlaceholder} (this is the template source, not a generated project). "
            + $"Pass --profile <{string.Join('|', Profiles)}>."),
        _ => throw new JexException(
            $"Unknown Database:Provider '{provider ?? "(missing)"}' in {source}. Pass --profile <{string.Join('|', Profiles)}>."),
    };
}
