using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jex.Cli.Project;

/// <summary>
/// <c>jex.json</c> in the solution root: marks a jex project and records the template version it was generated with.
/// Everything else (solution, projects, database) is derived from the real files at run time.
/// </summary>
public sealed record JexManifest(
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
    [property: JsonPropertyName("template")] string? Template,
    [property: JsonPropertyName("templateVersion")] string? TemplateVersion)
{
    public const string FileName = "jex.json";
    public const int CurrentSchemaVersion = 1;
    public const string TemplateName = "jex";

    private static readonly JsonSerializerOptions s_readOptions = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
    };

    private static readonly JsonSerializerOptions s_writeOptions = new() { WriteIndented = true };

    public static JexManifest Load(string path)
    {
        JexManifest? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<JexManifest>(File.ReadAllText(path), s_readOptions);
        }
        catch (JsonException ex)
        {
            throw new JexException($"{path} is not valid JSON: {ex.Message}", ex);
        }

        if (manifest is null || !string.Equals(manifest.Template, TemplateName, StringComparison.Ordinal))
        {
            throw new JexException($"{path} is not a jex manifest (expected \"template\": \"{TemplateName}\").");
        }

        return manifest;
    }

    public void Save(string path) => File.WriteAllText(path, JsonSerializer.Serialize(this, s_writeOptions).ReplaceLineEndings("\n") + "\n");
}
