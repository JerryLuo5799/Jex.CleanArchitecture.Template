namespace Jex.Cli.Process;

public sealed record ProcessCapture(int ExitCode, string StandardOutput, string StandardError);

/// <summary>Starts child processes without a shell; commands only build argument lists.</summary>
public interface IProcessRunner
{
    /// <summary>Runs with inherited stdio and returns the exit code.</summary>
    int Run(string workingDirectory, string fileName, IReadOnlyList<string> arguments);

    /// <summary>Runs with redirected output, English CLI messages and no logo, for parsing.</summary>
    ProcessCapture RunCapture(string workingDirectory, string fileName, IReadOnlyList<string> arguments);
}
