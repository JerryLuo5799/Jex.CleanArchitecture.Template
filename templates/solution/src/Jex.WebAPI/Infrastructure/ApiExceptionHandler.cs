using Jex.Application.Common;
using Jex.Application.Common.Exceptions;

namespace Jex.WebAPI.Infrastructure;

/// <summary>
/// Maps exceptions to the response envelope (always HTTP 200 for API routes):
/// <list type="bullet">
///   <item><see cref="ValidationException"/> → 4000 with <c>errors</c></item>
///   <item>any other <see cref="KnownException"/> → its <see cref="KnownException.Code"/></item>
///   <item><see cref="BadHttpRequestException"/> (malformed JSON, missing body, bad content type) → 4000</item>
///   <item>anything else → 5000, logged as an error; details only shown in Development</item>
/// </list>
/// </summary>
public sealed class ApiExceptionHandler(IHostEnvironment environment, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            // Client went away; nothing to write.
            return true;
        }

        var (code, message, errors) = exception switch
        {
            ValidationException ve => (ve.Code, ve.Message, ve.Errors),
            KnownException ke => (ke.Code, ke.Message, null),
            BadHttpRequestException bad => (ErrorCodes.Validation, SanitizeBadRequest(bad), null),
            _ => (ErrorCodes.Unexpected, environment.IsDevelopment() ? exception.Message : "An unexpected error occurred.", null),
        };

        if (code == ErrorCodes.Unexpected)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Request failed with code {ApiCode}: {Message}", code, message);
        }

        await ApiResponseWriter.WriteAsync(httpContext, code, message, errors, cancellationToken: cancellationToken);
        return true;
    }

    private static string SanitizeBadRequest(BadHttpRequestException exception) =>
        exception.StatusCode switch
        {
            StatusCodes.Status415UnsupportedMediaType => "Unsupported content type. Send 'application/json'.",
            _ => "The request body is malformed or missing required fields.",
        };
}
