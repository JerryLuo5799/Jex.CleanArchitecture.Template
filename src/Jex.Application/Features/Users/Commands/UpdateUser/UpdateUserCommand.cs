using MediatR;
using Sannr;
using System.Text.RegularExpressions;
using Jex.Application.Common;
using Jex.Application.Common.Exceptions;
using Jex.Domain.Entities;
using Jex.Domain.Enums;

namespace Jex.Application.Features.Users.Commands.UpdateUser;

/// <summary>
/// Command to update an existing user's details.
/// </summary>
public sealed partial record UpdateUserCommand(
    [property: Range(1, long.MaxValue, ErrorMessage = "Id must be a positive number.")]
    long Id,
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
    UserStatus Status) : IRequest;

/// <summary>
/// Handles <see cref="UpdateUserCommand"/>.
/// </summary>
public sealed class UpdateUserCommandHandler(IUserRepository userRepository)
    : IRequestHandler<UpdateUserCommand>
{
    public async Task Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.Id);

        var isEmailUnique = await userRepository.IsEmailUniqueAsync(request.Email, request.Id, cancellationToken);
        if (!isEmailUnique)
            throw new ValidationException(nameof(request.Email), "Email address is already in use.");

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Email = request.Email;
        user.Status = request.Status;

        await userRepository.UpdateAsync(user, cancellationToken);
    }
}

/// <summary>
/// Registers Sannr validation rules for <see cref="UpdateUserCommand"/>.
/// </summary>
internal static class UpdateUserCommandValidator
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    internal static void Register()
    {
        Sannr.AspNetCore.SannrValidatorRegistry.Register<UpdateUserCommand>(cmd =>
        {
            var result = new ValidationResult();

            if (cmd.Id <= 0)
                result.Add(nameof(cmd.Id), "Id must be a positive number.", Severity.Error);

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

            return Task.FromResult(result);
        });
    }
}
