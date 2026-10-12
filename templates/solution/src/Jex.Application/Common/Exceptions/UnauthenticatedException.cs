namespace Jex.Application.Common.Exceptions;

/// <summary>Thrown when the operation requires an authenticated caller (code 4010).</summary>
public sealed class UnauthenticatedException(string? message = null)
    : KnownException(ErrorCodes.Unauthenticated, message ?? "Authentication is required.");
