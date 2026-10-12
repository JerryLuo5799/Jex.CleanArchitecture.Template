using Jex.Application.Common.Exceptions;
using Jex.Application.Common.Interfaces;

namespace Jex.Application.Features.Auth.Commands.Login;

/// <summary>Authenticates a user and returns a JWT access token.</summary>
public sealed record LoginCommand(string Email, string Password) : IRequest<string>;

/// <summary>
/// Handles <see cref="LoginCommand"/>. Credential, lockout and status checks live in
/// <see cref="IIdentityService"/>; a generic error is returned so callers cannot enumerate accounts.
/// </summary>
public sealed class LoginCommandHandler(IIdentityService identityService)
    : IRequestHandler<LoginCommand, string>
{
    public async Task<string> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var token = await identityService.GetAuthTokenAsync(request.Email, request.Password, cancellationToken);

        return token ?? throw new UnauthenticatedException("Invalid email or password.");
    }
}
