using FluentValidation.Results;

namespace Jex.Application.Common.Exceptions;

/// <summary>
/// Thrown when a command or query fails validation (code 4000).
/// <see cref="Errors"/> groups messages by member name and is returned to the client.
/// </summary>
public sealed class ValidationException : KnownException
{
    private const string DefaultMessage = "One or more validation failures occurred.";

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : base(ErrorCodes.Validation, DefaultMessage)
    {
        Errors = failures
            .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }

    public ValidationException(string member, string message)
        : base(ErrorCodes.Validation, message)
    {
        Errors = new Dictionary<string, string[]> { [member] = [message] };
    }

    public ValidationException(IDictionary<string, string[]> errors)
        : base(ErrorCodes.Validation, DefaultMessage)
    {
        Errors = errors;
    }

    public IDictionary<string, string[]> Errors { get; }
}
