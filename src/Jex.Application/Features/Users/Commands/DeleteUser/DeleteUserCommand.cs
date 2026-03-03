using MediatR;

namespace Jex.Application.Features.Users.Commands.DeleteUser;

/// <summary>
/// Command to delete a user by their Id.
/// </summary>
public sealed record DeleteUserCommand(long Id) : IRequest;
