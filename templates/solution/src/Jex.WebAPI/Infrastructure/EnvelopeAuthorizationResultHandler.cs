using Jex.Application.Common;

namespace Jex.WebAPI.Infrastructure;

/// <summary>
/// Converts authorization outcomes into the envelope for API routes, independent of the
/// authentication scheme: challenged → 4010, forbidden → 4030 (both HTTP 200).
/// Non-API routes keep the default behaviour (401/403).
/// </summary>
public sealed class EnvelopeAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (!ApiResponseWriter.IsApiRequest(context))
        {
            await _default.HandleAsync(next, context, policy, authorizeResult);
            return;
        }

        if (authorizeResult.Challenged)
        {
            await ApiResponseWriter.WriteAsync(context, ErrorCodes.Unauthenticated, "Authentication is required.");
            return;
        }

        if (authorizeResult.Forbidden)
        {
            await ApiResponseWriter.WriteAsync(context, ErrorCodes.Forbidden, "You do not have permission to perform this action.");
            return;
        }

        await next(context);
    }
}
