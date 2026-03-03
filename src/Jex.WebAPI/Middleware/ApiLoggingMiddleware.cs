using System.Diagnostics;
using System.Text;

namespace Jex.WebAPI.Middleware;

/// <summary>
/// Middleware that logs API request and response details using NLog.
/// Logs: request ID, URL, POST body, response status code, and elapsed time.
/// </summary>
public sealed class ApiLoggingMiddleware(RequestDelegate next, ILogger<ApiLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();

        string? requestBody = null;
        if (context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(
                context.Request.Body,
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 4096,
                leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        await next(context);

        sw.Stop();

        var traceId = context.TraceIdentifier;
        var url     = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}";
        var method  = context.Request.Method;
        var status  = context.Response.StatusCode;
        var elapsed = sw.ElapsedMilliseconds;

        if (requestBody is not null)
        {
            logger.LogInformation(
                "TraceId={TraceId} Method={Method} Url={Url} Body={Body} Status={Status} Elapsed={Elapsed}ms",
                traceId, method, url, requestBody, status, elapsed);
        }
        else
        {
            logger.LogInformation(
                "TraceId={TraceId} Method={Method} Url={Url} Status={Status} Elapsed={Elapsed}ms",
                traceId, method, url, status, elapsed);
        }
    }
}
