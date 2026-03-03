using Sannr;
using Sannr.AspNetCore;
using System.Text.RegularExpressions;

namespace Jex.Application.Features.Auth.Commands.Login;

/// <summary>
/// Registers Sannr validation rules for <see cref="LoginCommand"/>.
/// </summary>
internal static class LoginCommandValidator
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    internal static void Register()
    {
        Sannr.AspNetCore.SannrValidatorRegistry.Register<LoginCommand>(cmd =>
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(cmd.Email))
                result.Add(nameof(cmd.Email), "Email is required.", Severity.Error);
            else if (!EmailRegex.IsMatch(cmd.Email))
                result.Add(nameof(cmd.Email), "A valid email address is required.", Severity.Error);

            if (string.IsNullOrWhiteSpace(cmd.Password))
                result.Add(nameof(cmd.Password), "Password is required.", Severity.Error);

            return Task.FromResult(result);
        });
    }
}
