using MediatR;
using Sannr;
using System.Text.RegularExpressions;
using Jex.Application.Common;
using Jex.Application.Common.Exceptions;

namespace Jex.Application.Features.Users.Commands.CreateUser;

/// <summary>
/// Command to create a new user. Returns the new user's Id.
/// </summary>
public sealed partial record CreateUserCommand(
    [property: Required(ErrorMessage = "First name is required.")]
    [property: StringLength(100)]
    string FirstName,
    [property: Required(ErrorMessage = "Last name is required.")]
    [property: StringLength(100)]
    string LastName,
    [property: Required(ErrorMessage = "Email is required.")]
    [property: EmailAddress(ErrorMessage = "A valid email address is required.")]
    [property: StringLength(200)]
    string Email,
    [property: Required(ErrorMessage = "Password is required.")]
    [property: StringLength(256, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 256 characters.")]
    string Password) : IRequest<long>;

/// <summary>
/// Handles <see cref="CreateUserCommand"/>.
/// Delegates user creation (including password hashing) to <see cref="IIdentityService"/>
/// so that ASP.NET Core Identity manages the credential lifecycle.
/// </summary>
public sealed class CreateUserCommandHandler(
    IUserRepository userRepository,
    IIdentityService identityService)
    : IRequestHandler<CreateUserCommand, long>
{
    public async Task<long> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var isEmailUnique = await userRepository.IsEmailUniqueAsync(request.Email, cancellationToken: cancellationToken);
        if (!isEmailUnique)
            throw new ValidationException(nameof(request.Email), "Email address is already in use.");

        var (userId, succeeded, errors) = await identityService.CreateUserAsync(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Password,
            cancellationToken);

        if (!succeeded)
            throw new ValidationException(nameof(request.Password), string.Join(" ", errors));

        return userId;
    }
}

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
