using SuperMegaUnboxDeluxe.Domain.Events;
using System;
using System.Collections.Generic;

namespace SuperMegaUnboxDeluxe.Domain.Entities;

public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public Guid Id { get; }
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected Entity() { }

    public IReadOnlyCollection<IDomainEvent> DequeueDomainEvents()
    {
        var events = _domainEvents.ToArray();
        _domainEvents.Clear();

        return events;
    }
}
