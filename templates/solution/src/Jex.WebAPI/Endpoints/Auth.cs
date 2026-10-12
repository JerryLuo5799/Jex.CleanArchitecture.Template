using Jex.Application.Features.Auth.Commands.Login;
using Jex.WebAPI.Infrastructure;
using Jex.WebAPI.Models;
using Jex.WebAPI.Options;

namespace Jex.WebAPI.Endpoints;

/// <summary>Authentication: login and token issuance. Mapped under <c>/api/auth</c>.</summary>
public sealed class Auth : IEndpointGroup
{
    public static string RoutePrefix => "/auth";

    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost(Login, "login")
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitingSettings.AuthPolicyName);
    }

    /// <summary>Authenticates a user and returns a JWT access token.</summary>
    private static async Task<ApiResponse<LoginResponse>> Login(ISender sender, LoginCommand command, CancellationToken cancellationToken)
    {
        var token = await sender.Send(command, cancellationToken);
        return ApiResponse.Success(new LoginResponse(token));
    }
}
