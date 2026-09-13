using System;

namespace SuperMegaUnboxDeluxe.Domain.Events;

public interface IDomainEvent
{
    DateTimeOffset OccuredAt { get; }
}
