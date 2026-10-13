using System.CommandLine;
using System.Text.Json;
using System.Text.RegularExpressions;
using Jex.Cli.Project;

namespace Jex.Cli.Tests;

/// <summary>
/// Every jex command the documentation tells people (and AI agents) to run must parse with the real command tree.
/// Full commands must parse without errors; permission globs such as Bash(jex scaffold *) only need their prefix
/// to name real commands, because the arguments after the wildcard are not part of the rule.
/// </summary>
public sealed partial class DocsCommandTests
{
    private static readonly string s_repo = FindRepoRoot();
    private static readonly RootCommand s_root = CliApp.Build(new CliHarness(Path.GetTempPath()).Context);

    public static TheoryData<string, string> DocumentedCommands()
    {
        var data = new TheoryData<string, string>();
        foreach (var file in MarkdownFiles())
        {
            foreach (var command in CommandsInMarkdown(File.ReadAllText(file)).Distinct())
            {
                data.Add(Relative(file), command);
            }
        }

        var solution = Path.Combine(s_repo, "templates", "solution");
        foreach (var command in Permissions(Path.Combine(solution, ".claude", "settings.json")).Where(p => !p.EndsWith('*')))
        {
            data.Add("templates/solution/.claude/settings.json", command);
        }

        using var tasks = ParseJson(Path.Combine(solution, ".vscode", "tasks.json"));
        foreach (var task in tasks.RootElement.GetProperty("tasks").EnumerateArray())
        {
            if (task.TryGetProperty("command", out var command) && command.GetString() is { } text && text.StartsWith("jex ", StringComparison.Ordinal))
            {
                data.Add("templates/solution/.vscode/tasks.json", text);
            }
        }

        return data;
    }

    public static TheoryData<string> PermissionPrefixes()
    {
        var data = new TheoryData<string>();
        foreach (var permission in Permissions(Path.Combine(s_repo, "templates", "solution", ".claude", "settings.json")).Where(p => p.EndsWith('*')))
        {
            data.Add(permission);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(DocumentedCommands))]
    public void Documented_command_parses(string file, string command)
    {
        // Prose may name a command or command group (`jex new`, `jex scaffold …`): only the path has to exist.
        // As soon as arguments or options appear, the whole command line has to parse.
        var tokens = Tokenize(ReferenceSuffix().Replace(command, string.Empty));
        if (IsCommandPath(tokens))
        {
            return;
        }

        var errors = s_root.Parse(tokens).Errors;

        errors.ShouldBeEmpty($"{file}: `{command}` -> {string.Join("; ", errors.Select(e => e.Message))}");
    }

    [Theory]
    [MemberData(nameof(PermissionPrefixes))]
    public void Permission_prefix_names_real_commands(string permission)
    {
        var words = permission.TrimEnd('*').Split(' ', StringSplitOptions.RemoveEmptyEntries);
        words[0].ShouldBe("jex");

        IsCommandPath(words[1..]).ShouldBeTrue($"'{permission}' does not name a jex command.");
    }

    [Theory]
    [InlineData("jex scaffold usecase X Y --type event")]
    [InlineData("jex dev new --check")]
    [InlineData("jex db script a b c")]
    [InlineData("jex scaffold feature Order")]
    public void Commands_with_arguments_must_parse_completely(string command) =>
        s_root.Parse(Tokenize(command)).Errors.ShouldNotBeEmpty();

    [Theory]
    [InlineData("jex scaffold", true)]
    [InlineData("jex dev new", true)]
    [InlineData("jex scaffold entities", false)]
    [InlineData("jex deploy", false)]
    public void Command_references_must_name_real_commands(string command, bool exists) =>
        IsCommandPath(Tokenize(command)).ShouldBe(exists);

    [Fact]
    public void Documentation_mentions_jex_commands()
    {
        DocumentedCommands().Count.ShouldBeGreaterThan(30);
        PermissionPrefixes().Count.ShouldBeGreaterThanOrEqualTo(4);
    }

    [Fact]
    public void Extraction_handles_inline_code_fences_placeholders_and_comments()
    {
        const string markdown = """
            Run `jex scaffold feature {Entity} {Plural}` then `jex.json` and `jex check`.
            ```bash
            jex new Acme.Shop        # comment
            dotnet build Jex.slnx
            jex db script add-orders && jex check --ci
            ```
            """;

        CommandsInMarkdown(markdown).ShouldBe([
            "jex scaffold feature X X",
            "jex check",
            "jex new Acme.Shop",
            "jex db script add-orders",
            "jex check --ci",
        ]);
    }

    private static IEnumerable<string> MarkdownFiles()
    {
        var roots = new[] { Path.Combine(s_repo, "templates", "solution"), Path.Combine(s_repo, "docs") };
        var skip = new[] { "bin", "obj", "node_modules" };
        return roots
            .SelectMany(root => Directory.EnumerateFiles(root, "*.md", SearchOption.AllDirectories))
            .Concat([Path.Combine(s_repo, "README.md"), Path.Combine(s_repo, "CONTRIBUTING.md")])
            .Where(file => !Relative(file).Split('/').Intersect(skip).Any());
    }

    internal static IEnumerable<string> CommandsInMarkdown(string markdown)
    {
        var inFence = false;
        foreach (var rawLine in markdown.ReplaceLineEndings("\n").Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                inFence = !inFence;
                continue;
            }

            if (inFence)
            {
                var withoutComment = Regex.Replace(line, @"\s+#.*$", string.Empty);
                foreach (var segment in withoutComment.Split("&&", StringSplitOptions.TrimEntries))
                {
                    if (segment.StartsWith("jex ", StringComparison.Ordinal))
                    {
                        yield return Placeholders(segment);
                    }
                }

                continue;
            }

            foreach (Match match in InlineCommand().Matches(line))
            {
                yield return Placeholders(match.Groups["command"].Value.Trim());
            }
        }
    }

    private static bool IsCommandPath(IEnumerable<string> words)
    {
        Command current = s_root;
        foreach (var word in words)
        {
            var next = current.Subcommands.SingleOrDefault(c => c.Name == word);
            if (next is null)
            {
                return false;
            }

            current = next;
        }

        return !ReferenceEquals(current, s_root);
    }

    private static string Placeholders(string command) => PlaceholderToken().Replace(command, "X");

    private static IEnumerable<string> Permissions(string settingsFile)
    {
        using var settings = ParseJson(settingsFile);
        var permissions = settings.RootElement.GetProperty("permissions");
        foreach (var list in new[] { "allow", "deny" })
        {
            foreach (var entry in permissions.GetProperty(list).EnumerateArray())
            {
                var match = BashPermission().Match(entry.GetString() ?? string.Empty);
                if (match.Success)
                {
                    yield return match.Groups["command"].Value;
                }
            }
        }
    }

    private static string[] Tokenize(string command)
    {
        var tokens = TokenPattern().Matches(command).Select(m => m.Groups["quoted"].Success ? m.Groups["quoted"].Value : m.Value).ToList();
        tokens[0].ShouldBe("jex");
        return [.. tokens.Skip(1)];
    }

    private static JsonDocument ParseJson(string file) =>
        JsonDocument.Parse(File.ReadAllText(file), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });

    private static string Relative(string file) => Path.GetRelativePath(s_repo, file).Replace('\\', '/');

    private static string FindRepoRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, RepoContext.MarkerFile)))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Run the tests from inside the template repository.");
    }

    [GeneratedRegex(@"`(?<command>jex [^`]+)`")]
    private static partial Regex InlineCommand();

    [GeneratedRegex(@"<[A-Za-z][^<>\s]*>|\{[^{}\s]+\}")]
    private static partial Regex PlaceholderToken();

    [GeneratedRegex(@"^Bash\((?<command>jex(?: [^)]*)?)\)$")]
    private static partial Regex BashPermission();

    [GeneratedRegex("\"(?<quoted>[^\"]*)\"|\\S+")]
    private static partial Regex TokenPattern();

    /// <summary>A trailing wildcard or ellipsis marks a reference to a command group, not a command line.</summary>
    [GeneratedRegex(@"\s*(\*|…|\.\.\.)$")]
    private static partial Regex ReferenceSuffix();
}
