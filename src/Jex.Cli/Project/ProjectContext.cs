using System.Xml;
using System.Xml.Linq;

namespace Jex.Cli.Project;

/// <summary>
/// A generated solution found by walking up to <c>jex.json</c>. Discovery only collects structure; conventions
/// (which project is the API, the scaffolding prefix, ...) are resolved on demand by the commands that need them,
/// so renaming or moving a project only affects the commands that actually use it.
/// </summary>
public sealed class ProjectContext
{
    private const string WebApiSuffix = ".WebAPI.csproj";

    private ProjectContext(string root, string startDirectory, JexManifest manifest, string solutionFile, IReadOnlyList<string> projects)
    {
        Root = root;
        StartDirectory = startDirectory;
        Manifest = manifest;
        SolutionFile = solutionFile;
        Projects = projects;
    }

    /// <summary>Solution root (the directory containing jex.json); every command runs here.</summary>
    public string Root { get; }

    /// <summary>Directory jex was started from; relative option values resolve against it.</summary>
    public string StartDirectory { get; }

    public JexManifest Manifest { get; }

    /// <summary>The single *.slnx in <see cref="Root"/>, as a file name.</summary>
    public string SolutionFile { get; }

    /// <summary>Solution file name without extension. Only used in messages, never to build paths.</summary>
    public string Name => Path.GetFileNameWithoutExtension(SolutionFile);

    /// <summary>Project paths referenced by the solution, relative to <see cref="Root"/> with '/' separators.</summary>
    public IReadOnlyList<string> Projects { get; }

    public bool HasDocker => File.Exists(Path.Combine(Root, "docker-compose.yml"));

    public static ProjectContext Discover(string startDirectory)
    {
        var start = Path.GetFullPath(startDirectory);
        var root = FindRoot(start) ?? throw new JexException(
            $"No {JexManifest.FileName} found in '{start}' or any parent directory. Run this inside a project created by 'jex new', "
            + $"or run 'jex init' in the solution root of a project generated before jex existed.");

        var manifest = JexManifest.Load(Path.Combine(root, JexManifest.FileName));
        var solutions = Directory.GetFiles(root, "*.slnx");
        if (solutions.Length != 1)
        {
            throw new JexException(solutions.Length == 0
                ? $"No *.slnx file in {root}."
                : $"Expected exactly one *.slnx file in {root}, found: {string.Join(", ", solutions.Select(Path.GetFileName))}.");
        }

        return new ProjectContext(root, start, manifest, Path.GetFileName(solutions[0]), ReadProjects(solutions[0]));
    }

    public static string? FindRoot(string startDirectory)
    {
        for (var directory = new DirectoryInfo(Path.GetFullPath(startDirectory)); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, JexManifest.FileName)))
            {
                return directory.FullName;
            }
        }

        return null;
    }

    public static IReadOnlyList<string> ReadProjects(string solutionFile)
    {
        try
        {
            return XDocument.Load(solutionFile)
                .Descendants("Project")
                .Select(project => (string?)project.Attribute("Path"))
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => Normalize(path!))
                .ToList();
        }
        catch (XmlException ex)
        {
            throw new JexException($"{solutionFile} is not a valid .slnx file: {ex.Message}", ex);
        }
    }

    /// <summary>The Web API project: <paramref name="overridePath"/> when given, else the only <c>*.WebAPI.csproj</c> in the solution.</summary>
    public string ResolveApiProject(string? overridePath = null) =>
        string.IsNullOrWhiteSpace(overridePath)
            ? Single(WebApiSuffix, "Web API project", "Pass --api-project <path> to choose it explicitly.")
            : ResolveOverride(overridePath, "--api-project");

    public string ResolveAppHostProject() =>
        Single(".AppHost.csproj", "Aspire AppHost project", "Create the project with --use-aspire, or run the API with 'jex run'.");

    public string ResolveTestProject(string suite)
    {
        var suffix = suite switch
        {
            "unit" => ".UnitTests.csproj",
            "integration" => ".IntegrationTests.csproj",
            "arch" => ".ArchitectureTests.csproj",
            _ => throw new JexException($"Unknown test suite '{suite}' (expected unit, integration or arch)."),
        };
        return Single(suffix, $"{suite} test project", "Run 'jex test' to run the whole solution instead.");
    }

    /// <summary>
    /// The <c>--solution-name</c> passed to the item templates (the prefix of src/&lt;Prefix&gt;.Domain etc.). An explicit
    /// value wins without any inference; otherwise it is the Web API project name without <c>.WebAPI</c>, provided the
    /// matching Domain and Application projects are in the solution. The solution file name plays no part.
    /// </summary>
    public string ResolveScaffoldPrefix(string? solutionName = null, string? apiOverride = null)
    {
        if (!string.IsNullOrWhiteSpace(solutionName))
        {
            return solutionName.Trim();
        }

        string api;
        try
        {
            api = ResolveApiProject(apiOverride);
        }
        catch (JexException ex)
        {
            throw new JexException($"{ex.Message} Or pass --solution-name <Prefix> to scaffold without inferring the prefix.", ex);
        }

        var fileName = Path.GetFileName(api);
        if (!fileName.EndsWith(WebApiSuffix, StringComparison.OrdinalIgnoreCase))
        {
            throw new JexException($"Cannot infer the scaffolding prefix from '{api}' (not a *{WebApiSuffix}). Pass --solution-name <Prefix>.");
        }

        var prefix = fileName[..^WebApiSuffix.Length];
        var missing = new[] { $"src/{prefix}.Domain/{prefix}.Domain.csproj", $"src/{prefix}.Application/{prefix}.Application.csproj" }
            .Where(expected => !Projects.Contains(expected, StringComparer.OrdinalIgnoreCase))
            .ToList();
        if (missing.Count > 0)
        {
            throw new JexException(
                $"Cannot infer the scaffolding prefix: {api} suggests '{prefix}', but {string.Join(" and ", missing)} "
                + $"{(missing.Count == 1 ? "is" : "are")} not in {SolutionFile}. Pass --solution-name <Prefix> "
                + "(the name prefix of your Domain and Application projects).");
        }

        return prefix;
    }

    private string Single(string suffix, string description, string hint)
    {
        var matches = Projects.Where(p => p.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)).ToList();
        return matches.Count switch
        {
            1 => matches[0],
            0 => throw new JexException($"No {description} (*{suffix}) in {SolutionFile}. {hint}"),
            _ => throw new JexException($"More than one {description} in {SolutionFile}: {string.Join(", ", matches)}. {hint}"),
        };
    }

    private string ResolveOverride(string path, string optionName)
    {
        var full = Path.GetFullPath(path, StartDirectory);
        if (Directory.Exists(full))
        {
            var projects = Directory.GetFiles(full, "*.csproj");
            full = projects.Length == 1
                ? projects[0]
                : throw new JexException($"{optionName} '{path}' is a directory with {projects.Length} project files; pass the .csproj path.");
        }
        else if (!File.Exists(full) || !full.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            throw new JexException($"{optionName} '{path}' is not a project file or a directory containing one.");
        }

        return Normalize(Path.GetRelativePath(Root, full));
    }

    private static string Normalize(string path)
    {
        var normalized = path.Replace('\\', '/');
        return normalized.StartsWith("./", StringComparison.Ordinal) ? normalized[2..] : normalized;
    }
}
