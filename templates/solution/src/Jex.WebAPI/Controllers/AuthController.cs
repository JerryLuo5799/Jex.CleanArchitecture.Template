using Jex.Application.Features.Auth.Commands.Login;
using Jex.WebAPI.Infrastructure;
using Jex.WebAPI.Models;

namespace Jex.WebAPI.Controllers;

/// <summary>Authentication: login and token issuance.</summary>
[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    /// <summary>Authenticates a user and returns a JWT access token.</summary>
    [HttpPost("login")]
    public async Task<ApiResponse<LoginResponse>> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
    {
        var token = await sender.Send(command, cancellationToken);
        return ApiResponse.Success(RequestIdentity.Get(HttpContext), new LoginResponse(token));
    }
}

public sealed record LoginResponse(string Token);
