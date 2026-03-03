using Sannr;

namespace Jex.Application.Common.Exceptions;

/// <summary>
/// Thrown when a Sannr validation rule fails on a command or query.
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(IEnumerable<ValidationError> errors)
        : base("One or more validation failures occurred.")
    {
        Errors = errors
            .GroupBy(e => e.MemberName, e => e.Message)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }

    public ValidationException(string member, string message)
        : this([new ValidationError(member, message, Severity.Error)])
    {
    }

    public IDictionary<string, string[]> Errors { get; }
}
