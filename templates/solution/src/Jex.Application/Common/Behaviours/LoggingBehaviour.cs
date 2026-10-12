using Jex.Application.Common.Interfaces;
using MediatR.Pipeline;

namespace Jex.Application.Common.Behaviours;

/// <summary>
/// Logs the request type and caller before every handler. The payload is intentionally not logged
/// (it may contain credentials or personal data).
/// </summary>
public sealed class LoggingBehaviour<TRequest>(ILogger<TRequest> logger, ICurrentUser currentUser)
    : IRequestPreProcessor<TRequest>
    where TRequest : notnull
{
    public Task Process(TRequest request, CancellationToken cancellationToken)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Handling {RequestName} for user {UserId}", typeof(TRequest).Name, currentUser.Id);
        }

        return Task.CompletedTask;
    }
}
