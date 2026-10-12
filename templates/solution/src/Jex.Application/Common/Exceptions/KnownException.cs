namespace Jex.Application.Common.Exceptions;

/// <summary>
/// Base class for expected business failures. The API layer maps a <see cref="KnownException"/>
/// to an envelope with <see cref="Code"/> and <see cref="Exception.Message"/>; any other exception
/// becomes <see cref="ErrorCodes.Unexpected"/> with details hidden outside Development.
/// </summary>
public class KnownException : Exception
{
    public KnownException(int code, string message)
        : base(message)
    {
        Code = code;
    }

    public KnownException(int code, string message, Exception? innerException)
        : base(message, innerException)
    {
        Code = code;
    }

    /// <summary>Business code from <see cref="ErrorCodes"/>.</summary>
    public int Code { get; }
}
