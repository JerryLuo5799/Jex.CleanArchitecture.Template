using System.Diagnostics;

namespace Jex.WebAPI.Infrastructure;

/// <summary>
/// Single source of the request correlation id used by the envelope (<c>requestId</c>),
/// the <c>X-Request-Id</c> response header and log scopes: the W3C trace id when available,
/// otherwise the ASP.NET Core <see cref="HttpContext.TraceIdentifier"/>.
/// </summary>
public static class RequestIdentity
{
    public const string HeaderName = "X-Request-Id";

    public static string Get(HttpContext context)
        => Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
}
