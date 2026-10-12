using System.ComponentModel;
using System.Diagnostics;

namespace Jex.Cli.Process;

public sealed class ProcessRunner(TextWriter echo) : IProcessRunner
{
    public int Run(string workingDirectory, string fileName, IReadOnlyList<string> arguments)
    {
        echo.WriteLine($"> {Format(fileName, arguments)}");
        using var process = Start(Create(workingDirectory, fileName, arguments), fileName);
        process.WaitForExit();
        return process.ExitCode;
    }

    public ProcessCapture RunCapture(string workingDirectory, string fileName, IReadOnlyList<string> arguments)
    {
        var info = Create(workingDirectory, fileName, arguments);
        info.RedirectStandardOutput = true;
        info.RedirectStandardError = true;
        info.Environment["DOTNET_CLI_UI_LANGUAGE"] = "en";
        info.Environment["DOTNET_NOLOGO"] = "1";

        using var process = Start(info, fileName);
        var standardError = process.StandardError.ReadToEndAsync();
        var standardOutput = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return new ProcessCapture(process.ExitCode, standardOutput, standardError.GetAwaiter().GetResult());
    }

    public static string Format(string fileName, IEnumerable<string> arguments) =>
        string.Join(' ', new[] { fileName }.Concat(arguments).Select(a => a.Length == 0 || a.Contains(' ', StringComparison.Ordinal) ? $"\"{a}\"" : a));

    private static ProcessStartInfo Create(string workingDirectory, string fileName, IReadOnlyList<string> arguments)
    {
        var info = new ProcessStartInfo(fileName) { WorkingDirectory = workingDirectory, UseShellExecute = false };
        foreach (var argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        return info;
    }

    private static System.Diagnostics.Process Start(ProcessStartInfo info, string fileName)
    {
        try
        {
            return System.Diagnostics.Process.Start(info) ?? throw new JexException($"Could not start '{fileName}'.");
        }
        catch (Win32Exception ex)
        {
            throw new JexException($"Could not start '{fileName}': {ex.Message}", ex);
        }
    }
}
