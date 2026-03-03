using MediatR;
using Sannr;
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
