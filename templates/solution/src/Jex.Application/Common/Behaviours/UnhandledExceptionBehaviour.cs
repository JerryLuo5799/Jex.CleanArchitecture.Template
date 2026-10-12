using Jex.Application.Common.Exceptions;

namespace Jex.Application.Common.Behaviours;

/// <summary>
/// Logs unexpected exceptions with the request type and rethrows. Expected business failures
/// (<see cref="KnownException"/>) pass through untouched; the API layer maps them to envelopes.
/// </summary>
public sealed class UnhandledExceptionBehaviour<TRequest, TResponse>(ILogger<TRequest> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next(cancellationToken);
        }
        catch (Exception ex) when (ex is not KnownException and not OperationCanceledException)
        {
            logger.LogError(ex, "Unhandled exception for request {RequestName}", typeof(TRequest).Name);
            throw;
        }
    }
}
