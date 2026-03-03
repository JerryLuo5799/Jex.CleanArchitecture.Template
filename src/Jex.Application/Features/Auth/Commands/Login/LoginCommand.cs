using MediatR;

namespace Jex.Application.Features.Auth.Commands.Login;

/// <summary>
/// Command to authenticate a user and retrieve a JWT access token.
/// </summary>
public sealed record LoginCommand(string Email, string Password) : IRequest<string>;
