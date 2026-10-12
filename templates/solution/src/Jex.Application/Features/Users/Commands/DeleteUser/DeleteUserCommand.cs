using Jex.Application.Common.Interfaces;

namespace Jex.Application.Features.Users.Commands.DeleteUser;

/// <summary>Deletes a user by Id.</summary>
public sealed record DeleteUserCommand(long Id) : IRequest;

/// <summary>Handles <see cref="DeleteUserCommand"/>.</summary>
public sealed class DeleteUserCommandHandler(IIdentityService identityService)
    : IRequestHandler<DeleteUserCommand>
{
    public Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        => identityService.DeleteUserAsync(request.Id, cancellationToken);
}
