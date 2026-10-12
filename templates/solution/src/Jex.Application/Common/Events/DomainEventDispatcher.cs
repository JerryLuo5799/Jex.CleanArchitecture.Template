using Jex.Application.Common.Interfaces;

namespace Jex.Application.Common.Events;

/// <summary>
/// Drains the <see cref="PendingDomainEvents"/> and publishes each event through MediatR.
/// Handlers may raise further events (via repositories); the loop continues until the queue is empty.
/// </summary>
public sealed class DomainEventDispatcher(PendingDomainEvents queue, IPublisher publisher) : IDomainEventDispatcher
{
    private const int MaxRounds = 10;

    public async Task DispatchAsync(CancellationToken cancellationToken = default)
    {
        for (var round = 0; round < MaxRounds; round++)
        {
            var events = queue.Drain();
            if (events.Count == 0)
            {
                return;
            }

            foreach (var domainEvent in events)
            {
                await publisher.Publish(domainEvent, cancellationToken);
            }
        }

        throw new InvalidOperationException(
            $"Domain events kept producing new events for {MaxRounds} rounds; check for an event handler loop.");
    }

    public void Discard() => queue.Clear();
}
