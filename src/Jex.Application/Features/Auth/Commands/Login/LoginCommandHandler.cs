using MediatR;
using Jex.Application.Common.Exceptions;
using Jex.Application.Common.Interfaces;

namespace Jex.Application.Features.Auth.Commands.Login;

/// <summary>
/// Handles <see cref="LoginCommand"/>.
/// Validates credentials via <see cref="IIdentityService"/> and returns a JWT token.
/// </summary>
public sealed class LoginCommandHandler(IIdentityService identityService)
    : IRequestHandler<LoginCommand, string>
{
    public async Task<string> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var token = await identityService.GetAuthTokenAsync(request.Email, request.Password, cancellationToken);

        if (token is null)
            throw new ValidationException(nameof(request.Email), "Invalid email or password.");

        return token;
    }
}
