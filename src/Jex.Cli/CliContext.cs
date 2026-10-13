using System.Reflection;
using Jex.Cli.Process;
using Jex.Cli.Project;
using Jex.Cli.Templates;

namespace Jex.Cli;

/// <summary>Everything a command touches outside its arguments; tests substitute the runner, writers and directories.</summary>
public sealed class CliContext
{
    public required IProcessRunner Runner { get; init; }

    public required TextWriter Out { get; init; }

    public required TextWriter Error { get; init; }

    public required string CurrentDirectory { get; init; }

    public required SemVer Version { get; init; }

    /// <summary>How to start this CLI again as a child process (the template matrix needs it).</summary>
    public required CliLaunch Self { get; init; }

    /// <summary>Directory of the running jex.dll; under ~/.dotnet/tools/.store when installed as a global tool.</summary>
    public string BaseDirectory { get; init; } = AppContext.BaseDirectory;

    public string NuGetPackagesDirectory { get; init; } = DefaultNuGetPackagesDirectory();

    public static CliContext CreateDefault() => new()
    {
        Runner = new ProcessRunner(Console.Out),
        Out = Console.Out,
        Error = Console.Error,
        CurrentDirectory = Directory.GetCurrentDirectory(),
        Version = ReadVersion(),
        Self = CliLauncher.Self(),
    };

    public static SemVer ReadVersion()
    {
        var informational = typeof(CliContext).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var plus = informational?.IndexOf('+', StringComparison.Ordinal) ?? -1;
        return SemVer.TryParse(plus >= 0 ? informational![..plus] : informational, out var version) ? version : SemVer.Dev;
    }

    public ProjectContext Project() => ProjectContext.Discover(CurrentDirectory);

    public RepoContext Repo() => RepoContext.Discover(CurrentDirectory);

    /// <summary>Runs a step of a command; a non-zero exit code stops the command and becomes jex's exit code.</summary>
    public void Run(string workingDirectory, string fileName, IReadOnlyList<string> arguments)
    {
        var exitCode = Runner.Run(workingDirectory, fileName, arguments);
        if (exitCode != 0)
        {
            throw new JexException($"'{fileName} {(arguments.Count > 0 ? arguments[0] : string.Empty)}' exited with code {exitCode}.", exitCode);
        }
    }

    public int RunAllowingFailure(string workingDirectory, string fileName, IReadOnlyList<string> arguments) =>
        Runner.Run(workingDirectory, fileName, arguments);

    private static string DefaultNuGetPackagesDirectory()
    {
        var configured = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        return string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages")
            : configured;
    }
}
