// Structure assertions for a project generated from the Jex template.
// Usage: dotnet run scripts/assert-structure.cs -- <outputDir> <name> <database> <aspire> <aiDocs> <docker>
//   database: sqlite | postgresql | sqlserver | mysql
//   aspire / aiDocs / docker: true | false
// Exit code 0 when every assertion passes; 1 otherwise (each failure is printed).

using System.Text.RegularExpressions;

if (args.Length < 6)
{
    Console.Error.WriteLine("usage: assert-structure.cs <outputDir> <name> <database> <aspire> <aiDocs> <docker>");
    return 2;
}

var root = Path.GetFullPath(args[0]);
// The template engine renames with the safe_namespace form of --name (my-api -> my_api).
var name = Regex.Replace(args[1], @"[^A-Za-z0-9_.]", "_");
var database = args[2].ToLowerInvariant();
var aspire = bool.Parse(args[3]);
var aiDocs = bool.Parse(args[4]);
var docker = bool.Parse(args[5]);

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
})
{
    Exists(required, true, "core solution file");
}

Exists(".template.config", false, "template metadata must not be emitted");

// ── Optional feature files ──────────────────────────────────────────────────
Exists($"src/{name}.AppHost", aspire, "--use-aspire");
// ServiceDefaults is added in the Aspire phase; assertion enabled there.
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
var allowedLowerTokens = new[] { "jex-usecase", "jex-endpoint", "jex-entity" };
var skipDirs = new[] { "bin", "obj", ".git", ".vs", "logs", "TestResults", "artifacts", "node_modules" };

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
    if (!textExtensions.Contains(ext) && !textExtensions.Contains(fileName) && fileName != "Dockerfile" && fileName != "Taskfile.yml")
    {
        continue;
    }

    var content = File.ReadAllText(file);

    foreach (Match m in Regex.Matches(content, @"\bJex\b|Jex\.|\.Jex"))
    {
        Fail($"'Jex' leftover in {relative} at index {m.Index}: ...{Snippet(content, m.Index)}...");
        break;
    }

    foreach (Match m in Regex.Matches(content, @"\bjex(?:[-_.][a-z*]+)?"))
    {
        // Item template names (jex-usecase etc.), the "jex-*" permission glob and the Taskfile
        // prefix variable ("TPL: jex") legitimately keep the lowercase template prefix.
        var lineEnd = content.IndexOf('\n', m.Index);
        var line = content[(content.LastIndexOf('\n', m.Index) + 1)..(lineEnd < 0 ? content.Length : lineEnd)];
        var allowed = allowedLowerTokens.Contains(m.Value)
            || m.Value == "jex-*"
            || (m.Value == "jex" && line.Contains("TPL: jex", StringComparison.Ordinal));
        if (allowed)
        {
            continue;
        }

        Fail($"'jex' leftover in {relative}: '{m.Value}' ...{Snippet(content, m.Index)}...");
        break;
    }

    foreach (var token in new[] { "TPLPREFIX", "TPLNAMELOWER", "DB_PROFILE", "FREESQL_PROVIDER", "TPLCONN_PREFIX", "TPLCONN_SUFFIX", "TemplateSourceOnly" })
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
