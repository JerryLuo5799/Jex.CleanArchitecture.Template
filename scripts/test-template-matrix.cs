// Template matrix test: for each cell, generate a project, assert its structure, scaffold a feature with
// the jex CLI under test, then run the strict gate (jex check --ci: format check, warnings-as-errors build, tests).
//
// The CLI under test is always explicit (never looked up on PATH): --jex is the executable and every --jex-arg is
// placed before the jex command, so both an installed tool and "dotnet path/to/jex.dll" can be tested.
// `jex dev matrix` fills these in for the jex that runs it.
//
//   dotnet run --file scripts/test-template-matrix.cs -- --jex jex                                   # all cells, ./templates
//   dotnet run --file scripts/test-template-matrix.cs -- --jex dotnet --jex-arg src/Jex.Cli/bin/Debug/net10.0/jex.dll --cells sqlite,bare
//   dotnet run --file scripts/test-template-matrix.cs -- --jex jex --package artifacts/Jex.CleanArchitecture.Template.1.2.3.nupkg
//   dotnet run --file scripts/test-template-matrix.cs -- --jex jex --docker   # also `docker build` the cells marked docker
//
// Exit code: 0 when every selected cell passes.

using System.Diagnostics;
using System.Text.RegularExpressions;

var root = FindRepoRoot();
var work = Path.Combine(root, "artifacts", "matrix");
string? package = null;
string[]? onlyCells = null;
var runDocker = false;
string? jexExecutable = null;
var jexPrefix = new List<string>();

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--package":
            package = Path.GetFullPath(args[++i]);
            break;
        case "--cells":
            onlyCells = args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            break;
        case "--docker":
            runDocker = true;
            break;
        case "--jex":
            jexExecutable = Path.Exists(args[i + 1]) ? Path.GetFullPath(args[++i]) : args[++i];
            break;
        case "--jex-arg":
            jexPrefix.Add(Path.Exists(args[i + 1]) ? Path.GetFullPath(args[++i]) : args[++i]);
            break;
        default:
            Console.Error.WriteLine($"unknown argument '{args[i]}'");
            return 2;
    }
}

if (jexExecutable is null)
{
    Console.Error.WriteLine("--jex <executable> is required: the jex CLI under test (run through 'jex dev matrix' to test the running jex).");
    return 2;
}

// The stamped jex.json of a packed template must carry the package version.
string? packageVersion = null;
if (package is not null)
{
    var match = Regex.Match(Path.GetFileName(package), @"^Jex\.CleanArchitecture\.Template\.(?<version>.+)\.nupkg$", RegexOptions.IgnoreCase);
    packageVersion = match.Success ? match.Groups["version"].Value : null;
}

var cells = new List<Cell>
{
    new("sqlite", "Acme", "sqlite", Aspire: false, AiDocs: true, Docker: true, DockerBuild: true),
    new("postgresql", "Acme.Shop", "postgresql", Aspire: false, AiDocs: true, Docker: true, DockerBuild: false),
    new("sqlserver", "Acme", "sqlserver", Aspire: false, AiDocs: true, Docker: true, DockerBuild: false),
    new("mysql", "my-api", "mysql", Aspire: false, AiDocs: true, Docker: true, DockerBuild: false),
    new("aspire", "Acme.Shop", "postgresql", Aspire: true, AiDocs: true, Docker: true, DockerBuild: true),
    new("bare", "my-api", "sqlite", Aspire: false, AiDocs: false, Docker: false, DockerBuild: false),
};

if (onlyCells is not null)
{
    cells = cells.Where(c => onlyCells.Contains(c.Id, StringComparer.OrdinalIgnoreCase)).ToList();
}

// ── Install the templates under test ─────────────────────────────────────────
// Exactly one install of the templates may exist: reinstalling a nupkg over an installed copy of the same package
// adds a second entry, and two installs of the same templates make 'dotnet new jex' ambiguous (exit code 70).
var source = package ?? Path.Combine(root, "templates");
foreach (var existing in new[] { "Jex.CleanArchitecture.Template", Path.Combine(root, "templates") })
{
    for (var attempt = 0; attempt < 5 && RunQuiet(root, "dotnet", ["new", "uninstall", existing]) == 0; attempt++)
    {
    }
}

Run(root, "dotnet", ["new", "install", source]);

var results = new List<(Cell Cell, bool Ok, string Detail, TimeSpan Elapsed)>();

foreach (var cell in cells)
{
    var sw = Stopwatch.StartNew();
    var output = Path.Combine(work, $"{cell.Id} {cell.Name}");
    var safe = SafeName(cell.Name);
    Console.WriteLine($"\n=== {cell.Id}: {cell.Name} (database={cell.Database}, aspire={cell.Aspire}, ai={cell.AiDocs}, docker={cell.Docker}) ===");

    try
    {
        if (Directory.Exists(output))
        {
            Directory.Delete(output, recursive: true);
        }

        Run(root, "dotnet", ["new", "jex", "-n", cell.Name, "-o", output, "--database", cell.Database,
            "--use-aspire", Bool(cell.Aspire), "--include-ai-docs", Bool(cell.AiDocs), "--include-docker", Bool(cell.Docker)]);

        string[] versionArgs = packageVersion is null ? [] : ["--template-version", packageVersion];
        Run(root, "dotnet", ["run", "--file", Path.Combine(root, "scripts", "assert-structure.cs"), "--",
            output, cell.Name, cell.Database, Bool(cell.Aspire), Bool(cell.AiDocs), Bool(cell.Docker), .. versionArgs]);

        // Scaffold with the CLI under test from a subdirectory: it has to find the solution root via jex.json.
        var subdirectory = Path.Combine(output, "src");
        Jex(subdirectory, ["scaffold", "feature", "Order", "Orders"]);
        Jex(subdirectory, ["scaffold", "usecase", "GetOrdersWithPagination", "Orders", "--type", "query", "--return", "OrderDto", "--paged"]);
        Jex(subdirectory, ["scaffold", "usecase", "DeleteOrder", "Orders", "--type", "command", "--with-id"]);

        // Re-running a scaffold must refuse to overwrite.
        if (Jex(output, ["scaffold", "entity", "Order", "Orders"], allowFailure: true) == 0)
        {
            throw new InvalidOperationException("re-running 'jex scaffold entity' overwrote existing files");
        }

        Jex(output, ["check", "--ci"]);

        // db-script against a temporary SQLite baseline: a relative output directory resolves against the
        // solution root and the script is written with LF line endings and no trailing whitespace.
        if (cell.Database == "sqlite")
        {
            Run(output, "dotnet", ["run", "--project", $"src/{safe}.WebAPI", "-c", "Release", "--no-build", "--no-launch-profile",
                "--", "db-script", "initial", "db/migrations"]);
            var script = Path.Combine(output, "db", "migrations", "0001_initial.sql");
            var text = File.Exists(script)
                ? File.ReadAllText(script)
                : throw new InvalidOperationException($"db-script did not write {script}");
            if (text.Contains('\r', StringComparison.Ordinal) || text.Split('\n').Any(line => line.EndsWith(' ')))
            {
                throw new InvalidOperationException("db-script output has CRLF or trailing whitespace");
            }
        }

        if (runDocker && cell.Docker && cell.DockerBuild)
        {
            Run(output, "docker", ["build", "-f", $"src/{safe}.WebAPI/Dockerfile", "-t", $"jex-matrix-{cell.Id}", "."]);
        }

        results.Add((cell, true, "ok", sw.Elapsed));
    }
    catch (Exception ex)
    {
        results.Add((cell, false, ex.Message, sw.Elapsed));
    }
}

Console.WriteLine("\n=== Template matrix summary ===");
foreach (var (cell, ok, detail, elapsed) in results)
{
    Console.WriteLine($"{(ok ? "PASS" : "FAIL"),-5} {cell.Id,-12} {cell.Name,-10} {elapsed.TotalSeconds,6:0}s  {(ok ? string.Empty : detail)}");
}

return results.All(r => r.Ok) ? 0 : 1;

int Jex(string workingDirectory, string[] arguments, bool allowFailure = false) =>
    Run(workingDirectory, jexExecutable!, [.. jexPrefix, .. arguments], allowFailure);

static int Run(string workingDirectory, string fileName, string[] arguments, bool allowFailure = false)
{
    Console.WriteLine($"> {fileName} {string.Join(' ', arguments.Select(a => a.Contains(' ', StringComparison.Ordinal) ? $"\"{a}\"" : a))}");
    var info = new ProcessStartInfo(fileName) { WorkingDirectory = workingDirectory, UseShellExecute = false };
    foreach (var argument in arguments)
    {
        info.ArgumentList.Add(argument);
    }

    using var process = Process.Start(info) ?? throw new InvalidOperationException($"could not start {fileName}");
    process.WaitForExit();

    if (process.ExitCode != 0 && !allowFailure)
    {
        throw new InvalidOperationException($"'{Path.GetFileName(fileName)} {string.Join(' ', arguments.Take(3))}' exited with {process.ExitCode}");
    }

    return process.ExitCode;
}

static string Bool(bool value) => value ? "true" : "false";

static int RunQuiet(string workingDirectory, string fileName, string[] arguments)
{
    var info = new ProcessStartInfo(fileName)
    {
        WorkingDirectory = workingDirectory,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    };
    foreach (var argument in arguments)
    {
        info.ArgumentList.Add(argument);
    }

    using var process = Process.Start(info) ?? throw new InvalidOperationException($"could not start {fileName}");
    var standardError = process.StandardError.ReadToEndAsync();
    process.StandardOutput.ReadToEnd();
    standardError.GetAwaiter().GetResult();
    process.WaitForExit();
    return process.ExitCode;
}

static string SafeName(string name) => string.Concat(name.Select(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '.' ? c : '_'));

static string FindRepoRoot()
{
    var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Jex.CleanArchitecture.Template.csproj")))
    {
        directory = directory.Parent;
    }

    return directory?.FullName ?? throw new InvalidOperationException("Run from inside the template repository.");
}

internal sealed record Cell(string Id, string Name, string Database, bool Aspire, bool AiDocs, bool Docker, bool DockerBuild);
