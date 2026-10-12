using System.Diagnostics;
using Jex.Application.Common.Interfaces;

namespace Jex.Application.Common.Behaviours;

/// <summary>Warns when a request takes longer than <see cref="ThresholdMilliseconds"/>.</summary>
public sealed class PerformanceBehaviour<TRequest, TResponse>(ILogger<TRequest> logger, ICurrentUser currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public const int ThresholdMilliseconds = 500;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var start = Stopwatch.GetTimestamp();

        var response = await next(cancellationToken);

        var elapsed = Stopwatch.GetElapsedTime(start);
        if (elapsed.TotalMilliseconds > ThresholdMilliseconds && logger.IsEnabled(LogLevel.Warning))
        {
            logger.LogWarning(
                "Long running request: {RequestName} took {ElapsedMs}ms for user {UserId}",
                typeof(TRequest).Name,
                (long)elapsed.TotalMilliseconds,
                currentUser.Id);
        }

        return response;
    }
}
