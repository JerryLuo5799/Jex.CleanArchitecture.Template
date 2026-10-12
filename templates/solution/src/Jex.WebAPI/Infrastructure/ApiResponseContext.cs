using Jex.WebAPI.Models;

namespace Jex.WebAPI.Infrastructure;

/// <summary>
/// Records the business code of the current response on the <see cref="HttpContext"/> so that
/// request logging and metrics can report it. Marking never writes to the response: normal
/// endpoints are serialized by the framework, error paths use <see cref="ApiResponseWriter"/>.
/// </summary>
public static class ApiResponseContext
{
    public const string CodeItemKey = "Jex.ApiCode";

    /// <summary>Stores <paramref name="code"/> for logging and returns the request id.</summary>
    public static string Mark(HttpContext context, int code)
    {
        context.Items[CodeItemKey] = code;
        return RequestIdentity.Get(context);
    }

    /// <summary>Fills the request id and records the code of an envelope produced by an endpoint.</summary>
    public static void Mark(HttpContext context, IApiResponse response)
    {
        if (string.IsNullOrEmpty(response.RequestId))
        {
            response.RequestId = RequestIdentity.Get(context);
        }

        context.Items[CodeItemKey] = response.Code;
    }

    public static int? GetCode(HttpContext context)
        => context.Items.TryGetValue(CodeItemKey, out var code) ? code as int? : null;
}
