using Sannr;
using Sannr.AspNetCore;
using System.Text.RegularExpressions;

namespace Jex.Application.Features.Users.Commands.CreateUser;

/// <summary>
/// Registers Sannr validation rules for <see cref="CreateUserCommand"/>.
/// </summary>
internal static class CreateUserCommandValidator
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    internal static void Register()
    {
        Sannr.AspNetCore.SannrValidatorRegistry.Register<CreateUserCommand>(cmd =>
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(cmd.FirstName))
                result.Add(nameof(cmd.FirstName), "First name is required.", Severity.Error);
            else if (cmd.FirstName.Length > 100)
                result.Add(nameof(cmd.FirstName), "First name must not exceed 100 characters.", Severity.Error);

            if (string.IsNullOrWhiteSpace(cmd.LastName))
                result.Add(nameof(cmd.LastName), "Last name is required.", Severity.Error);
            else if (cmd.LastName.Length > 100)
                result.Add(nameof(cmd.LastName), "Last name must not exceed 100 characters.", Severity.Error);

            if (string.IsNullOrWhiteSpace(cmd.Email))
                result.Add(nameof(cmd.Email), "Email is required.", Severity.Error);
            else if (cmd.Email.Length > 200)
                result.Add(nameof(cmd.Email), "Email must not exceed 200 characters.", Severity.Error);
            else if (!EmailRegex.IsMatch(cmd.Email))
                result.Add(nameof(cmd.Email), "A valid email address is required.", Severity.Error);

            if (string.IsNullOrWhiteSpace(cmd.Password))
                result.Add(nameof(cmd.Password), "Password is required.", Severity.Error);
            else if (cmd.Password.Length < 8)
                result.Add(nameof(cmd.Password), "Password must be at least 8 characters.", Severity.Error);
            else if (cmd.Password.Length > 256)
                result.Add(nameof(cmd.Password), "Password must not exceed 256 characters.", Severity.Error);

            return Task.FromResult(result);
        });
    }
}
