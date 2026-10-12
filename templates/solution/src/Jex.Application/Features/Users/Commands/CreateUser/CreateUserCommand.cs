using Jex.Application.Common.Exceptions;
using Jex.Application.Common.Interfaces;

namespace Jex.Application.Features.Users.Commands.CreateUser;

/// <summary>Creates a new user and returns the new user's Id.</summary>
public sealed record CreateUserCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password) : IRequest<long>;

/// <summary>
/// Handles <see cref="CreateUserCommand"/>. Delegates creation (including password hashing)
/// to <see cref="IIdentityService"/> so ASP.NET Core Identity owns the credential lifecycle.
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
        {
            throw new ValidationException(nameof(request.Email), "Email address is already in use.");
        }

        return await identityService.CreateUserAsync(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Password,
            cancellationToken);
    }
}
