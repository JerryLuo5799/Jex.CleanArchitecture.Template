using Jex.WebAPI.Models;

namespace Jex.WebAPI.Infrastructure;

/// <summary>
/// Applied to every endpoint group: fills <see cref="IApiResponse.RequestId"/> and records the
/// business code for logging. It does not write the response (the framework serializes the envelope).
/// </summary>
public sealed class ApiResponseEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var result = await next(context);

        if (result is IApiResponse response)
        {
            ApiResponseContext.Mark(context.HttpContext, response);
        }

        return result;
    }
}
