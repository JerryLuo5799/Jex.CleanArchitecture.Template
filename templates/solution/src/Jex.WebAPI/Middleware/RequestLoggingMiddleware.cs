using System.Diagnostics;
using System.Security.Claims;
using Jex.WebAPI.Infrastructure;

namespace Jex.WebAPI.Middleware;

/// <summary>
/// Logs one line per request: method, path, HTTP status, business code, elapsed time and user id.
/// Registered as the outermost middleware so it observes the final response even when an exception
/// handler produced it. Request bodies are never logged (they may contain credentials).
/// </summary>
public sealed class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public const string ApiCodeItemKey = "Jex.ApiCode";

    public async Task InvokeAsync(HttpContext context)
    {
        var start = Stopwatch.GetTimestamp();
        var requestId = RequestIdentity.Get(context);

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[RequestIdentity.HeaderName] = requestId;
            return Task.CompletedTask;
        });

        try
        {
            await next(context);
        }
        finally
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                var elapsed = Stopwatch.GetElapsedTime(start);
                var apiCode = context.Items.TryGetValue(ApiCodeItemKey, out var code) ? code : null;
                var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

                logger.LogInformation(
                    "HTTP {Method} {Path} responded {StatusCode} code={ApiCode} in {ElapsedMs}ms requestId={RequestId} userId={UserId}",
                    context.Request.Method,
                    context.Request.Path.Value,
                    context.Response.StatusCode,
                    apiCode,
                    elapsed.TotalMilliseconds,
                    requestId,
                    userId);
            }
        }
    }
}
