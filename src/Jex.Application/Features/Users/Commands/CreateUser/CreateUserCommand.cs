using MediatR;
using Sannr;

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
