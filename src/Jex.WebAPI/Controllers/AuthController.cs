using MediatR;
using Microsoft.AspNetCore.Mvc;
using Jex.Application.Features.Auth.Commands.Login;
using Jex.WebAPI.Models;

namespace Jex.WebAPI.Controllers;

/// <summary>
/// Handles authentication operations (login / token issuance).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Authenticates a user and returns a JWT access token.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken)
    {
        var token = await sender.Send(command, cancellationToken);
        return Ok(ApiResponse<object>.Success(HttpContext.TraceIdentifier, new { token }));
    }
}
