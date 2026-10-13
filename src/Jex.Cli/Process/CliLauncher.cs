namespace Jex.Cli.Process;

/// <summary>How to start a jex CLI as a child process: the executable plus arguments that precede the command.</summary>
public sealed record CliLaunch(string Executable, IReadOnlyList<string> PrefixArguments);

public static class CliLauncher
{
    /// <summary>How to start the CLI that is currently running.</summary>
    public static CliLaunch Self() => Resolve(Environment.ProcessPath, typeof(CliLauncher).Assembly.Location);

    /// <summary>
    /// <c>Environment.ProcessPath</c> is the apphost (or the global-tool shim) when jex was started directly, but
    /// only the dotnet host when it was started as <c>dotnet jex.dll</c>; the DLL then has to be passed again.
    /// </summary>
    public static CliLaunch Resolve(string? processPath, string assemblyLocation)
    {
        if (string.IsNullOrEmpty(processPath))
        {
            return new CliLaunch("dotnet", [assemblyLocation]);
        }

        var host = Path.GetFileNameWithoutExtension(processPath);
        return string.Equals(host, "dotnet", StringComparison.OrdinalIgnoreCase)
            ? new CliLaunch(processPath, [assemblyLocation])
            : new CliLaunch(processPath, []);
    }
}
