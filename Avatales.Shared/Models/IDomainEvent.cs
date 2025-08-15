using MediatR;

namespace Avatales.Shared.Models;

/// <summary>
/// Basis-Interface für alle Domain Events
/// Ermöglicht Event-Driven Architecture und CQRS-Pattern
/// </summary>
public interface IDomainEvent : INotification
{
    Guid Id { get; }
    DateTime OccurredOn { get; }
    string EventType { get; }
    Guid AggregateId { get; }
    int Version { get; }
}

/// <summary>
/// Basis-Implementierung für Domain Events
/// </summary>
public abstract class DomainEvent : IDomainEvent
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public DateTime OccurredOn { get; private set; } = DateTime.UtcNow;
    public abstract string EventType { get; }
    public abstract Guid AggregateId { get; }
    public int Version { get; private set; } = 1;

    protected DomainEvent(Guid aggregateId, int version = 1)
    {
        AggregateId = aggregateId;
        Version = version;
    }
}