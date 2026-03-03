using MediatR;
using Jex.Application.Common.Exceptions;
using Jex.Application.Common.Interfaces;
using Jex.Domain.Entities;
using Jex.Domain.Enums;

namespace Jex.Application.Features.Users.Commands.CreateUser;

/// <summary>
/// Handles <see cref="CreateUserCommand"/>.
/// Validates uniqueness, hashes the password, and persists via the repository.
/// </summary>
public sealed class CreateUserCommandHandler(IUserRepository userRepository)
    : IRequestHandler<CreateUserCommand, long>
{
    public async Task<long> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var isEmailUnique = await userRepository.IsEmailUniqueAsync(request.Email, cancellationToken: cancellationToken);
        if (!isEmailUnique)
            throw new ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(request.Email), "Email address is already in use.")
            ]);

        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Status = UserStatus.Active
        };

        var created = await userRepository.AddAsync(user, cancellationToken);
        return created.Id;
    }
}
