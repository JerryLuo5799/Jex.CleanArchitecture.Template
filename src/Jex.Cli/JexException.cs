namespace Jex.Cli;

/// <summary>A user-facing failure: the message goes to stderr and the exit code is returned by jex.</summary>
public sealed class JexException : Exception
{
    public JexException()
        : this("jex failed.")
    {
    }

    public JexException(string message)
        : this(message, 1)
    {
    }

    public JexException(string message, Exception innerException)
        : base(message, innerException) => ExitCode = 1;

    public JexException(string message, int exitCode)
        : base(message) => ExitCode = exitCode == 0 ? 1 : exitCode;

    public int ExitCode { get; }
}
