using System.ComponentModel.DataAnnotations.Schema;

namespace Jex.Domain.Common;

/// <summary>
/// Base class for every persisted entity: a <c>long</c> identity key plus a domain event queue.
/// Events are dispatched by the unit of work right before the command's transaction commits.
/// </summary>
public abstract class BaseEntity
{
    private readonly List<BaseEvent> _domainEvents = [];

    public long Id { get; set; }

    [NotMapped]
    public IReadOnlyCollection<BaseEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(BaseEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void RemoveDomainEvent(BaseEvent domainEvent) => _domainEvents.Remove(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
