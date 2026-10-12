namespace Jex.Application.Common.Exceptions;

/// <summary>Thrown when the caller is authenticated but not permitted (code 4030).</summary>
public sealed class ForbiddenAccessException(string? message = null)
    : KnownException(ErrorCodes.Forbidden, message ?? "You do not have permission to perform this action.");
