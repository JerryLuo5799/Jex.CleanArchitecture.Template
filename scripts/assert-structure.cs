// Structure assertions for a project generated from the Jex template.
// Usage: dotnet run scripts/assert-structure.cs -- <outputDir> <name> <database> <aspire> <aiDocs> <docker> [--template-version <v>]
//   database: sqlite | postgresql | sqlserver | mysql
//   aspire / aiDocs / docker: true | false
//   --template-version: expected jex.json templateVersion (the package version when testing a nupkg)
// Exit code 0 when every assertion passes; 1 otherwise (each failure is printed).

using System.Text.Json;
using System.Text.RegularExpressions;

if (args.Length < 6)
{
    Console.Error.WriteLine("usage: assert-structure.cs <outputDir> <name> <database> <aspire> <aiDocs> <docker> [--template-version <v>]");
    return 2;
}

var root = Path.GetFullPath(args[0]);
// The template engine renames with the safe_namespace form of --name (my-api -> my_api).
var name = Regex.Replace(args[1], @"[^A-Za-z0-9_.]", "_");
var database = args[2].ToLowerInvariant();
var aspire = bool.Parse(args[3]);
var aiDocs = bool.Parse(args[4]);
var docker = bool.Parse(args[5]);
string? expectedTemplateVersion = null;
for (var i = 6; i < args.Length; i++)
{
    if (args[i] == "--template-version" && i + 1 < args.Length)
    {
        expectedTemplateVersion = args[++i];
    }
    else
    {
        Console.Error.WriteLine($"unknown argument '{args[i]}'");
        return 2;
    }
}

var failures = new List<string>();
void Fail(string message) => failures.Add(message);
void Exists(string relative, bool expected, string why)
{
    var path = Path.Combine(root, relative);
    var exists = File.Exists(path) || Directory.Exists(path);
    if (exists != expected)
    {
        Fail($"{(expected ? "missing" : "unexpected")}: {relative} ({why})");
    }
}

if (!Directory.Exists(root))
{
    Console.Error.WriteLine($"output directory not found: {root}");
    return 1;
}

// ── Files that must always exist ────────────────────────────────────────────
foreach (var required in new[]
{
    $"{name}.slnx",
    "Directory.Build.props",
    "Directory.Packages.props",
    "global.json",
    ".editorconfig",
    ".gitignore",
    "nuget.config",
    "BannedSymbols.txt",
    $"src/{name}.Domain/{name}.Domain.csproj",
    $"src/{name}.Application/{name}.Application.csproj",
    $"src/{name}.Infrastructure/{name}.Infrastructure.csproj",
    $"src/{name}.WebAPI/{name}.WebAPI.csproj",
    $"src/{name}.WebAPI/Program.cs",
    $"src/{name}.WebAPI/appsettings.json",
    $"src/{name}.WebAPI/appsettings.Development.json",
    $"src/{name}.WebAPI/Properties/launchSettings.json",
    $"tests/{name}.UnitTests/{name}.UnitTests.csproj",
    $"tests/{name}.IntegrationTests/{name}.IntegrationTests.csproj",
    $"tests/{name}.ArchitectureTests/{name}.ArchitectureTests.csproj",
    "tests/Directory.Build.props",
    "jex.json",
})
{
    Exists(required, true, "core solution file");
}

Exists(".template.config", false, "template metadata must not be emitted");
Exists("Taskfile.yml", false, "the jex CLI replaced go-task");

// ── jex.json: marks the project for the jex CLI and records the template version ──
var manifestPath = Path.Combine(root, "jex.json");
if (File.Exists(manifestPath))
{
    try
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var schema = manifest.RootElement.TryGetProperty("schemaVersion", out var s) && s.ValueKind == JsonValueKind.Number ? s.GetInt32() : 0;
        var template = manifest.RootElement.TryGetProperty("template", out var t) ? t.GetString() : null;
        var templateVersion = manifest.RootElement.TryGetProperty("templateVersion", out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
        if (schema != 1 || template != "jex" || string.IsNullOrEmpty(templateVersion))
        {
            Fail($"jex.json must have schemaVersion 1, template \"jex\" and a templateVersion: {File.ReadAllText(manifestPath).Trim()}");
        }
        else if (expectedTemplateVersion is not null && templateVersion != expectedTemplateVersion)
        {
            Fail($"jex.json templateVersion is {templateVersion}, expected the package version {expectedTemplateVersion}");
        }
    }
    catch (JsonException ex)
    {
        Fail($"jex.json is not valid JSON: {ex.Message}");
    }
}

// ── Install instructions name the real tool package, not <Name>.Cli ──────────
foreach (var (doc, expected) in new[] { ("README.md", true), ("README.zh-CN.md", true), ("AGENTS.md", aiDocs) })
{
    var path = Path.Combine(root, doc);
    if (expected && File.Exists(path) && !File.ReadAllText(path).Contains("dotnet tool install -g Jex.Cli", StringComparison.Ordinal))
    {
        Fail($"{doc} must tell readers to 'dotnet tool install -g Jex.Cli'");
    }
}

// ── Optional feature files ──────────────────────────────────────────────────
Exists("README.md", true, "the English README is always generated");
Exists("README.zh-CN.md", true, "the Chinese README is always generated");
foreach (var (doc, other) in new[] { ("README.md", "README.zh-CN.md"), ("README.zh-CN.md", "README.md") })
{
    var path = Path.Combine(root, doc);
    if (File.Exists(path) && !File.ReadAllText(path).Contains($"]({other})", StringComparison.Ordinal))
    {
        Fail($"{doc} must link to {other} (language switch)");
    }

    if (!aiDocs && File.Exists(path) && File.ReadAllText(path).Contains("AGENTS.md", StringComparison.Ordinal))
    {
        Fail($"{doc} mentions AGENTS.md, which --include-ai-docs false does not generate");
    }
}
Exists($"src/{name}.AppHost", aspire, "--use-aspire");
Exists($"src/{name}.ServiceDefaults", true, "ServiceDefaults is always generated");
Exists("AGENTS.md", aiDocs, "--include-ai-docs");
Exists("CLAUDE.md", aiDocs, "--include-ai-docs");
Exists(".claude/settings.json", aiDocs, "--include-ai-docs");
Exists(".github/copilot-instructions.md", aiDocs, "--include-ai-docs");
Exists("docker-compose.yml", docker, "--include-docker");
Exists($"src/{name}.WebAPI/Dockerfile", docker, "--include-docker");
Exists(".dockerignore", docker, "--include-docker");

// ── Leftover placeholders / template-only tokens ────────────────────────────
var textExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    ".cs", ".csproj", ".props", ".targets", ".slnx", ".json", ".yml", ".yaml", ".md", ".http", ".txt",
    ".editorconfig", ".gitignore", ".dockerignore", ".config", ".xml", ".sql", ".env", ".example",
};
var allowedLowerTokens = new[] { "jex", "jex.json", "jex-usecase", "jex-endpoint", "jex-entity", "jex-*" };
var skipDirs = new[] { "bin", "obj", ".git", ".vs", "logs", "TestResults", "artifacts", "node_modules" };
var renamedCli = new Regex($@"\b{Regex.Escape(name)}\.Cli\b");

foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
{
    var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
    if (skipDirs.Any(d => relative.Split('/').Contains(d)))
    {
        continue;
    }

    var fileName = Path.GetFileName(file);
    if (fileName.Contains("Jex", StringComparison.Ordinal))
    {
        Fail($"file name still contains 'Jex': {relative}");
    }

    var ext = Path.GetExtension(file);
    if (!textExtensions.Contains(ext) && !textExtensions.Contains(fileName) && fileName != "Dockerfile")
    {
        continue;
    }

    var content = File.ReadAllText(file);

    foreach (Match m in Regex.Matches(content, @"\bJex\b|Jex\.|\.Jex"))
    {
        // The jex CLI's NuGet id is the one legitimate capitalised "Jex" (restored from the TPLTOOL placeholder).
        if (Regex.IsMatch(content[m.Index..], @"^Jex\.Cli\b"))
        {
            continue;
        }

        Fail($"'Jex' leftover in {relative} at index {m.Index}: ...{Snippet(content, m.Index)}...");
        break;
    }

    foreach (Match m in Regex.Matches(content, @"\bjex(?:[-_.][a-z*]+)?"))
    {
        // The jex CLI (jex check, jex.json), item template names (jex-usecase etc.) and the "jex-*"
        // permission glob legitimately keep the lowercase template prefix.
        if (allowedLowerTokens.Contains(m.Value))
        {
            continue;
        }

        Fail($"'jex' leftover in {relative}: '{m.Value}' ...{Snippet(content, m.Index)}...");
        break;
    }

    if (renamedCli.Match(content) is { Success: true } renamed)
    {
        Fail($"'{renamed.Value}' in {relative}: the tool package is Jex.Cli whatever the project is called (use TPLTOOL in the template)");
    }

    foreach (var token in new[] { "TPLPREFIX", "TPLTOOL", "TPLNAMELOWER", "DB_PROFILE", "FREESQL_PROVIDER", "TPLCONN_PREFIX", "TPLCONN_SUFFIX", "TemplateSourceOnly" })
    {
        if (content.Contains(token, StringComparison.Ordinal))
        {
            Fail($"placeholder '{token}' leftover in {relative}");
        }
    }
}

// ── Database provider: Infrastructure references exactly one FreeSql provider ──
var infraCsproj = File.ReadAllText(Path.Combine(root, $"src/{name}.Infrastructure/{name}.Infrastructure.csproj"));
var providers = Regex.Matches(infraCsproj, @"FreeSql\.Provider\.(\w+)").Select(m => m.Groups[1].Value).Distinct().ToList();
if (providers.Count != 1)
{
    Fail($"Infrastructure must reference exactly one FreeSql provider, found: {string.Join(", ", providers)}");
}
else
{
    var expectedProvider = database switch
    {
        "sqlite" => "Sqlite",
        "postgresql" => "PostgreSQL",
        "sqlserver" => "SqlServer",
        "mysql" => "MySqlConnector",
        _ => throw new ArgumentException($"unknown database '{database}'"),
    };
    if (providers[0] != expectedProvider)
    {
        Fail($"Infrastructure references FreeSql.Provider.{providers[0]} but --database {database} expects {expectedProvider}");
    }
}

var appsettings = File.ReadAllText(Path.Combine(root, $"src/{name}.WebAPI/appsettings.json"));
var expectedProviderSetting = database switch
{
    "sqlite" => "Sqlite",
    "postgresql" => "PostgreSQL",
    "sqlserver" => "SqlServer",
    "mysql" => "MySql",
    _ => database,
};
if (!Regex.IsMatch(appsettings, $"\"Provider\"\\s*:\\s*\"{expectedProviderSetting}\""))
{
    Fail($"appsettings.json Database:Provider should be \"{expectedProviderSetting}\" for --database {database}");
}

// ── Ports: launchSettings and .http agree ────────────────────────────────────
var launch = File.ReadAllText(Path.Combine(root, $"src/{name}.WebAPI/Properties/launchSettings.json"));
var httpPort = Regex.Match(launch, @"http://localhost:(\d+)").Groups[1].Value;
if (string.IsNullOrEmpty(httpPort))
{
    Fail("launchSettings.json has no http://localhost:<port>");
}
else
{
    var httpFile = Path.Combine(root, $"src/{name}.WebAPI/{name}.WebAPI.http");
    if (File.Exists(httpFile) && !File.ReadAllText(httpFile).Contains($"localhost:{httpPort}", StringComparison.Ordinal))
    {
        Fail($".http file does not use the generated port {httpPort}");
    }
}

// ── Report ──────────────────────────────────────────────────────────────────
if (failures.Count == 0)
{
    Console.WriteLine($"assert-structure: OK ({name}, database={database}, aspire={aspire}, aiDocs={aiDocs}, docker={docker})");
    return 0;
}

Console.Error.WriteLine($"assert-structure: {failures.Count} failure(s) in {root}");
foreach (var failure in failures)
{
    Console.Error.WriteLine($"  - {failure}");
}

return 1;

static string Snippet(string content, int index)
{
    var start = Math.Max(0, index - 20);
    var length = Math.Min(60, content.Length - start);
    return content.Substring(start, length).Replace("\r", string.Empty).Replace("\n", "\\n");
}
