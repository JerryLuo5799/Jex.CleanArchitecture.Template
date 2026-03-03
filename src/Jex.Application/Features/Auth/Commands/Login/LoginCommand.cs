using MediatR;
using Sannr;
using System.Text.RegularExpressions;
using Jex.Application.Common;
using Jex.Application.Common.Exceptions;

namespace Jex.Application.Features.Auth.Commands.Login;

/// <summary>
/// Command to authenticate a user and retrieve a JWT access token.
/// </summary>
public sealed record LoginCommand(string Email, string Password) : IRequest<string>;

/// <summary>
/// Handles <see cref="LoginCommand"/>.
/// Validates credentials via <see cref="IIdentityService"/> and returns a JWT token.
/// </summary>
public sealed class LoginCommandHandler(IIdentityService identityService)
    : IRequestHandler<LoginCommand, string>
{
    public async Task<string> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var token = await identityService.GetAuthTokenAsync(request.Email, request.Password, cancellationToken);

        if (token is null)
            throw new ValidationException(nameof(request.Email), "Invalid email or password.");

        return token;
    }
}

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
