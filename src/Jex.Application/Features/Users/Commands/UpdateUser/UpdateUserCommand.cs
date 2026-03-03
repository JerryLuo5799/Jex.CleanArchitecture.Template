using MediatR;
using Jex.Domain.Enums;

namespace Jex.Application.Features.Users.Commands.UpdateUser;

/// <summary>
/// Command to update an existing user's details.
/// </summary>
public sealed record UpdateUserCommand(
    long Id,
    string FirstName,
    string LastName,
    string Email,
    UserStatus Status) : IRequest;
