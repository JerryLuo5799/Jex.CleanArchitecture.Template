using MediatR;
using Jex.Application.Common.Exceptions;
using Jex.Application.Common.Interfaces;
using Jex.Domain.Entities;

namespace Jex.Application.Features.Users.Commands.UpdateUser;

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
