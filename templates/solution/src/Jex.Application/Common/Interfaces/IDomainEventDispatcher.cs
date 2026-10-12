namespace Jex.Application.Common.Interfaces;

/// <summary>Publishes queued domain events (inside the current transaction) until none remain.</summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(CancellationToken cancellationToken = default);

    /// <summary>Drops pending events, e.g. when the transaction is rolled back.</summary>
    void Discard();
}
