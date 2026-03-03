using MediatR;

namespace Jex.Application.Features.Users.Commands.CreateUser;

/// <summary>
/// Command to create a new user. Returns the new user's Id.
/// </summary>
public sealed record CreateUserCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password) : IRequest<long>;
