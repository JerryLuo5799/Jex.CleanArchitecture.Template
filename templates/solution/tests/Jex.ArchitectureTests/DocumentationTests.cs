using System.Text.RegularExpressions;

namespace Jex.ArchitectureTests;

/// <summary>Keeps AGENTS.md honest: every `task xyz` it mentions must exist in Taskfile.yml.</summary>
public partial class DocumentationTests
{
    [Fact]
    public void AgentsMd_OnlyReferencesTasksThatExist()
    {
        var root = FindSolutionRoot();
        var agents = Path.Combine(root, "AGENTS.md");
        var taskfile = Path.Combine(root, "Taskfile.yml");

        if (!File.Exists(agents) || !File.Exists(taskfile))
        {
            // AI docs are optional (--include-ai-docs false); nothing to check.
            return;
        }

        var defined = TaskDefinition().Matches(File.ReadAllText(taskfile)).Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);
        var referenced = TaskReference().Matches(File.ReadAllText(agents)).Select(m => m.Groups[1].Value).Distinct().ToList();

        var unknown = referenced.Where(t => !defined.Contains(t)).ToList();

        unknown.ShouldBeEmpty($"AGENTS.md mentions tasks that are not defined in Taskfile.yml: {string.Join(", ", unknown)}.");
    }

    private static string FindSolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !directory.EnumerateFiles("*.slnx").Any())
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Solution root (*.slnx) not found above " + AppContext.BaseDirectory);
    }

    [GeneratedRegex(@"^  ([a-z][a-z0-9:_-]*):\s*$", RegexOptions.Multiline)]
    private static partial Regex TaskDefinition();

    [GeneratedRegex(@"`task ([a-z][a-z0-9:_-]*)")]
    private static partial Regex TaskReference();
}
