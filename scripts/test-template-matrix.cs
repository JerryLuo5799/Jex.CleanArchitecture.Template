// Template matrix test: for each cell, generate a project, assert its structure, scaffold a feature with
// the item templates, then run the strict gate (format check, warnings-as-errors build, tests).
//
//   dotnet run --file scripts/test-template-matrix.cs                       # all cells, templates installed from ./templates
//   dotnet run --file scripts/test-template-matrix.cs -- --cells sqlite,bare
//   dotnet run --file scripts/test-template-matrix.cs -- --package artifacts/Jex.CleanArchitecture.Template.1.2.3.nupkg
//   dotnet run --file scripts/test-template-matrix.cs -- --docker           # also `docker build` the cells marked docker
//
// Exit code: 0 when every selected cell passes.

using System.Diagnostics;

var root = FindRepoRoot();
var work = Path.Combine(root, "artifacts", "matrix");
string? package = null;
string[]? onlyCells = null;
var runDocker = false;

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
        default:
            Console.Error.WriteLine($"unknown argument '{args[i]}'");
            return 2;
    }
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
var source = package ?? Path.Combine(root, "templates");
Run(root, "dotnet", ["new", "install", source, "--force"]);

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

        Run(root, "dotnet", ["run", "--file", Path.Combine(root, "scripts", "assert-structure.cs"), "--",
            output, cell.Name, cell.Database, Bool(cell.Aspire), Bool(cell.AiDocs), Bool(cell.Docker)]);

        // Scaffold a full feature with the item templates (what `task scaffold:feature` does).
        Run(output, "dotnet", ["new", "jex-entity", "-n", "Order", "--plural", "Orders", "--solution-name", safe, "-o", "."]);
        Run(output, "dotnet", ["new", "jex-usecase", "-n", "CreateOrder", "--feature", "Orders", "--usecase-type", "command", "--return-type", "long", "--solution-name", safe, "-o", "."]);
        Run(output, "dotnet", ["new", "jex-usecase", "-n", "GetOrderById", "--feature", "Orders", "--usecase-type", "query", "--return-type", "OrderDto", "--with-id", "--solution-name", safe, "-o", "."]);
        Run(output, "dotnet", ["new", "jex-usecase", "-n", "GetOrdersWithPagination", "--feature", "Orders", "--usecase-type", "query", "--return-type", "OrderDto", "--paged", "--solution-name", safe, "-o", "."]);
        Run(output, "dotnet", ["new", "jex-usecase", "-n", "DeleteOrder", "--feature", "Orders", "--usecase-type", "command", "--with-id", "--solution-name", safe, "-o", "."]);
        Run(output, "dotnet", ["new", "jex-endpoint", "-n", "Orders", "--entity", "Order", "--solution-name", safe, "-o", "."]);

        // Re-running a scaffold must refuse to overwrite.
        if (Run(output, "dotnet", ["new", "jex-entity", "-n", "Order", "--plural", "Orders", "--solution-name", safe, "-o", "."], allowFailure: true) == 0)
        {
            throw new InvalidOperationException("re-running jex-entity overwrote existing files");
        }

        var slnx = $"{safe}.slnx";
        Run(output, "dotnet", ["format", slnx, "--verify-no-changes"]);
        Run(output, "dotnet", ["build", slnx, "-c", "Release", "-p:ContinuousIntegrationBuild=true"]);
        Run(output, "dotnet", ["test", "--solution", slnx, "-c", "Release", "--no-build"]);

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
        throw new InvalidOperationException($"'{fileName} {arguments.FirstOrDefault()}' exited with {process.ExitCode}");
    }

    return process.ExitCode;
}

static string Bool(bool value) => value ? "true" : "false";

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
