using System.Text.RegularExpressions;

namespace Jex.Cli.Project;

/// <summary>The template repository itself (for <c>jex dev</c>), found by walking up to the template package project.</summary>
public sealed partial class RepoContext
{
    public const string MarkerFile = "Jex.CleanArchitecture.Template.csproj";
    public const string SolutionFile = "Jex.CleanArchitecture.slnx";

    private RepoContext(string root) => Root = root;

    public string Root { get; }

    public string TemplatesDirectory => Path.Combine(Root, "templates");

    public string TemplateSolutionDirectory => Path.Combine(Root, "templates", "solution");

    public string ArtifactsDirectory => Path.Combine(Root, "artifacts");

    public string SandboxDirectory => Path.Combine(ArtifactsDirectory, "sandbox");

    public string ScriptsDirectory => Path.Combine(Root, "scripts");

    public static RepoContext Discover(string startDirectory)
    {
        for (var directory = new DirectoryInfo(Path.GetFullPath(startDirectory)); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, MarkerFile)))
            {
                return new RepoContext(directory.FullName);
            }
        }

        throw new JexException(
            $"'jex dev' commands run inside the Jex.CleanArchitecture.Template repository: no {MarkerFile} found in '{startDirectory}' or any parent directory.");
    }

    /// <summary>
    /// <c>artifacts/sandbox/&lt;name&gt;</c>, after checking that <paramref name="name"/> is a plain template name
    /// (letters, digits, '_', '.', '-'; my-api is valid) and that the result really is inside the sandbox, because
    /// the directory is deleted before the project is regenerated.
    /// </summary>
    public string ResolveSandbox(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || !SandboxName().IsMatch(name) || name.Contains("..", StringComparison.Ordinal))
        {
            throw new JexException(
                $"'{name}' is not a valid project name: use letters, digits, '_', '.' or '-', starting with a letter or '_' (for example Acme, Acme.Shop, my-api).");
        }

        var sandbox = Path.GetFullPath(SandboxDirectory) + Path.DirectorySeparatorChar;
        var target = Path.GetFullPath(Path.Combine(SandboxDirectory, name));
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!target.StartsWith(sandbox, comparison) || target.Length == sandbox.Length)
        {
            throw new JexException($"'{name}' resolves to {target}, which is outside {sandbox}; refusing to delete it.");
        }

        return target;
    }

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_.\-]*$")]
    private static partial Regex SandboxName();
}
