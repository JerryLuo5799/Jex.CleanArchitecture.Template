using Jex.Domain.Common;

namespace Jex.Application.Common.Events;

/// <summary>
/// Scoped collector of entities that may carry domain events. Repositories enqueue every entity
/// they add, update or delete; the unit of work drains the queue before committing.
/// </summary>
public sealed class PendingDomainEvents
{
    private readonly HashSet<BaseEntity> _entities = [];

    public void Enqueue(BaseEntity entity) => _entities.Add(entity);

    /// <summary>Removes and returns every pending event, clearing them from their entities.</summary>
    public IReadOnlyList<BaseEvent> Drain()
    {
        var events = _entities.SelectMany(e => e.DomainEvents).ToList();

        foreach (var entity in _entities)
        {
            entity.ClearDomainEvents();
        }

        _entities.Clear();
        return events;
    }

    public void Clear()
    {
        foreach (var entity in _entities)
        {
            entity.ClearDomainEvents();
        }

        _entities.Clear();
    }
}
