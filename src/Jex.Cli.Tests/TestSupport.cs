using Jex.Cli.Process;
using Jex.Cli.Templates;

namespace Jex.Cli.Tests;

internal sealed record Invocation(string WorkingDirectory, string FileName, IReadOnlyList<string> Arguments)
{
    public string CommandLine => ProcessRunner.Format(FileName, Arguments);
}

/// <summary>Records every process jex would start; exit codes and captured output are scripted per test.</summary>
internal sealed class FakeProcessRunner : IProcessRunner
{
    public const string DevDirectoryInstall = """
        Currently installed items:
           Microsoft.DotNet.Web.ProjectTemplates.10.0
              Version: 10.0.0
              Templates:
                 ASP.NET Core Empty (web) C#
           D:\repo\templates
              Templates:
                 Jex Endpoint Group (jex-endpoint) C#
                 Jex Clean Architecture Solution (jex) C#
              Uninstall Command:
                 dotnet new uninstall D:\repo\templates
        """;

    private readonly Queue<int> _exitCodes = new();

    public List<Invocation> Invocations { get; } = [];

    public List<Invocation> Captures { get; } = [];

    /// <summary>Output of <c>dotnet new uninstall</c>; defaults to a development directory install.</summary>
    public string InstalledTemplates { get; set; } = DevDirectoryInstall;

    /// <summary>Exit codes for the next <see cref="Run"/> calls, in order; afterwards every call succeeds.</summary>
    public FakeProcessRunner ThenExit(params int[] exitCodes)
    {
        foreach (var exitCode in exitCodes)
        {
            _exitCodes.Enqueue(exitCode);
        }

        return this;
    }

    public int Run(string workingDirectory, string fileName, IReadOnlyList<string> arguments)
    {
        Invocations.Add(new Invocation(workingDirectory, fileName, [.. arguments]));
        return _exitCodes.Count > 0 ? _exitCodes.Dequeue() : 0;
    }

    public ProcessCapture RunCapture(string workingDirectory, string fileName, IReadOnlyList<string> arguments)
    {
        Captures.Add(new Invocation(workingDirectory, fileName, [.. arguments]));
        return arguments is ["new", "uninstall"]
            ? new ProcessCapture(0, InstalledTemplates, string.Empty)
            : new ProcessCapture(0, string.Empty, string.Empty);
    }

    public IReadOnlyList<string> CommandLines => Invocations.Select(i => i.CommandLine).ToList();
}

internal sealed class TempDirectory : IDisposable
{
    public TempDirectory() => Directory.CreateDirectory(Path);

    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "jex-tests", Guid.NewGuid().ToString("N"));

    public string Combine(params string[] parts) => System.IO.Path.Combine([Path, .. parts]);

    public string Write(string relative, string content)
    {
        var file = Combine(relative.Split('/'));
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(file)!);
        File.WriteAllText(file, content);
        return file;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}

/// <summary>Writes a minimal generated solution: jex.json, a .slnx and empty project files.</summary>
internal static class TestProject
{
    public static string[] StandardProjects(string prefix, bool aspire = false)
    {
        List<string> projects =
        [
            $"src/{prefix}.Domain/{prefix}.Domain.csproj",
            $"src/{prefix}.Application/{prefix}.Application.csproj",
            $"src/{prefix}.Infrastructure/{prefix}.Infrastructure.csproj",
            $"src/{prefix}.ServiceDefaults/{prefix}.ServiceDefaults.csproj",
            $"src/{prefix}.WebAPI/{prefix}.WebAPI.csproj",
            $"tests/{prefix}.UnitTests/{prefix}.UnitTests.csproj",
            $"tests/{prefix}.IntegrationTests/{prefix}.IntegrationTests.csproj",
            $"tests/{prefix}.ArchitectureTests/{prefix}.ArchitectureTests.csproj",
        ];
        if (aspire)
        {
            projects.Add($"src/{prefix}.AppHost/{prefix}.AppHost.csproj");
        }

        return [.. projects];
    }

    public static void Create(TempDirectory directory, string solutionName, IEnumerable<string> projects, string? templateVersion = "0.0.0-dev")
    {
        var version = templateVersion is null ? "null" : $"\"{templateVersion}\"";
        directory.Write("jex.json", $$"""{ "schemaVersion": 1, "template": "jex", "templateVersion": {{version}} }""");
        var references = string.Join('\n', projects.Select(p => $"""    <Project Path="{p}" />"""));
        directory.Write($"{solutionName}.slnx", $"<Solution>\n  <Folder Name=\"/src/\">\n{references}\n  </Folder>\n</Solution>\n");
        foreach (var project in projects)
        {
            directory.Write(project, "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        }
    }
}

/// <summary>Runs the real command tree against a fake process runner.</summary>
internal sealed class CliHarness(string currentDirectory)
{
    public FakeProcessRunner Runner { get; } = new();

    public StringWriter Out { get; } = new();

    public StringWriter Error { get; } = new();

    public SemVer Version { get; init; } = SemVer.Dev;

    public CliLaunch Self { get; init; } = new("dotnet", ["/tools/Jex.Cli.dll"]);

    public string BaseDirectory { get; init; } = AppContext.BaseDirectory;

    public string NuGetPackagesDirectory { get; init; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "jex-tests-nuget");

    public CliContext Context => new()
    {
        Runner = Runner,
        Out = Out,
        Error = Error,
        CurrentDirectory = currentDirectory,
        Version = Version,
        Self = Self,
        BaseDirectory = BaseDirectory,
        NuGetPackagesDirectory = NuGetPackagesDirectory,
    };

    public int Run(params string[] args) => CliApp.Run(args, Context);
}
