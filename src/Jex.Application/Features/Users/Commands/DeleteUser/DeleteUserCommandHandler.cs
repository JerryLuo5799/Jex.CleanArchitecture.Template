using MediatR;
using Jex.Application.Common.Exceptions;
using Jex.Application.Common.Interfaces;
using Jex.Domain.Entities;

namespace Jex.Application.Features.Users.Commands.DeleteUser;

/// <summary>
/// Handles <see cref="DeleteUserCommand"/>.
/// </summary>
public sealed class DeleteUserCommandHandler(IUserRepository userRepository)
    : IRequestHandler<DeleteUserCommand>
{
    public async Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var exists = await userRepository.ExistsAsync(u => u.Id == request.Id, cancellationToken);
        if (!exists)
            throw new NotFoundException(nameof(User), request.Id);

        await userRepository.DeleteAsync(request.Id, cancellationToken);
    }
}
