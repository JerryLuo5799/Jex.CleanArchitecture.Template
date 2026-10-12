using Jex.WebAPI.Models;

namespace Jex.WebAPI.Infrastructure;

/// <summary>
/// Writes an error envelope from middleware paths (exception handler, authorization result handler,
/// status code pages, rate limiter). Business endpoints never call this: they return
/// <see cref="ApiResponse{T}"/> and let the framework serialize it.
/// </summary>
public static class ApiResponseWriter
{
    /// <summary>Returns true for routes that use the envelope contract.</summary>
    public static bool IsApiRequest(HttpContext context)
        => context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase);

    public static async Task WriteAsync(
        HttpContext context,
        int code,
        string message,
        IDictionary<string, string[]>? errors = null,
        int statusCode = StatusCodes.Status200OK,
        CancellationToken cancellationToken = default)
    {
        var requestId = ApiResponseContext.Mark(context, code);

        if (context.Response.HasStarted)
        {
            return;
        }

        // Do not Clear(): middleware such as the rate limiter may already have set headers (Retry-After).
        context.Response.StatusCode = statusCode;
        context.Response.Headers[RequestIdentity.HeaderName] = requestId;

        await context.Response.WriteAsJsonAsync(ApiResponse.Fail(requestId, code, message, errors), cancellationToken);
    }
}
