namespace Jex.Application.Common.Exceptions;

/// <summary>Thrown when the operation conflicts with current state, e.g. duplicate key or stale concurrency stamp (code 4090).</summary>
public sealed class ConflictException(string message)
    : KnownException(ErrorCodes.Conflict, message);
