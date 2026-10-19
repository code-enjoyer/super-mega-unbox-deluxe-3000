using System;
using System.Collections.Generic;
using SuperMegaUnboxDeluxe.Domain.Events;

namespace SuperMegaUnboxDeluxe.Domain.Entities;

public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public Guid Id { get; private set; }
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected Entity()
    {
        Id = Guid.NewGuid();
    }

    protected Entity(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("An entity ID cannot be empty.", nameof(id));
        Id = id;
    }

    public IReadOnlyCollection<IDomainEvent> DequeueDomainEvents()
    {
        var events = _domainEvents.ToArray();
        _domainEvents.Clear();

        return events;
    }
}
