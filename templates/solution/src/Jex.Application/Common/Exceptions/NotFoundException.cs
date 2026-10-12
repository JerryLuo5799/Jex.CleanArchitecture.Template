namespace Jex.Application.Common.Exceptions;

/// <summary>Thrown when a requested entity does not exist (code 4040).</summary>
public sealed class NotFoundException(string name, object key)
    : KnownException(ErrorCodes.NotFound, $"Entity '{name}' with key '{key}' was not found.");
