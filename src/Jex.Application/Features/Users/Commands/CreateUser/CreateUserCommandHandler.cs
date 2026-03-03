using MediatR;
using Jex.Application.Common.Exceptions;
using Jex.Application.Common.Interfaces;

namespace Jex.Application.Features.Users.Commands.CreateUser;

/// <summary>
/// Handles <see cref="CreateUserCommand"/>.
/// Delegates user creation (including password hashing) to <see cref="IIdentityService"/>
/// so that ASP.NET Core Identity manages the credential lifecycle.
/// </summary>
public sealed class CreateUserCommandHandler(
    IUserRepository userRepository,
    IIdentityService identityService)
    : IRequestHandler<CreateUserCommand, long>
{
    public async Task<long> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var isEmailUnique = await userRepository.IsEmailUniqueAsync(request.Email, cancellationToken: cancellationToken);
        if (!isEmailUnique)
            throw new ValidationException(nameof(request.Email), "Email address is already in use.");

        var (userId, succeeded, errors) = await identityService.CreateUserAsync(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Password,
            cancellationToken);

        if (!succeeded)
            throw new ValidationException(nameof(request.Password), string.Join(" ", errors));

        return userId;
    }
}
