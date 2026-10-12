using Jex.Application.Common.Exceptions;
using Jex.Application.Common.Interfaces;
using Jex.Domain.Enums;

namespace Jex.Application.Features.Users.Commands.UpdateUser;

/// <summary>Updates an existing user's profile, email and status.</summary>
public sealed record UpdateUserCommand(
    long Id,
    string FirstName,
    string LastName,
    string Email,
    UserStatus Status) : IRequest;

/// <summary>
/// Handles <see cref="UpdateUserCommand"/>. Identity fields (email/user name) are changed through
/// <see cref="IIdentityService"/> so normalized columns and security stamps stay consistent.
/// </summary>
public sealed class UpdateUserCommandHandler(
    IUserRepository userRepository,
    IIdentityService identityService)
    : IRequestHandler<UpdateUserCommand>
{
    public async Task Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var isEmailUnique = await userRepository.IsEmailUniqueAsync(request.Email, request.Id, cancellationToken);
        if (!isEmailUnique)
        {
            throw new ValidationException(nameof(request.Email), "Email address is already in use.");
        }

        await identityService.UpdateUserAsync(
            request.Id,
            request.FirstName,
            request.LastName,
            request.Email,
            request.Status,
            cancellationToken);
    }
}
