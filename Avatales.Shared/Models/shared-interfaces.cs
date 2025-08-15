using MediatR;

namespace Avatales.Shared.Models;

/// <summary>
/// Basis-Interface für alle Entitäten
/// </summary>
public interface IEntity
{
    Guid Id { get; }
    DateTime CreatedAt { get; }
}

/// <summary>
/// Interface für auditierbare Entitäten
/// </summary>
public interface IAuditable
{
    DateTime CreatedAt { get; }
    DateTime? UpdatedAt { get; }
}

/// <summary>
/// Interface für Soft-Delete Funktionalität
/// </summary>
public interface ISoftDelete
{
    bool IsDeleted { get; }
    DateTime? DeletedAt { get; }
    void SoftDelete(string reason = "");
    void Restore();
}

/// <summary>
/// Basis-Interface für Domain Events
/// </summary>
public interface IDomainEvent : INotification
{
    Guid Id { get; }
    Guid AggregateId { get; }
    DateTime OccurredAt { get; }
    string EventType { get; }
    Dictionary<string, object> GetEventData();
}

/// <summary>
/// Basis-Implementierung für Domain Events
/// </summary>
public abstract class DomainEvent : IDomainEvent
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid AggregateId { get; private set; }
    public DateTime OccurredAt { get; private set; } = DateTime.UtcNow;
    public abstract string EventType { get; }

    protected DomainEvent(Guid aggregateId)
    {
        AggregateId = aggregateId;
    }

    public virtual Dictionary<string, object> GetEventData()
    {
        return new Dictionary<string, object>
        {
            { "EventId", Id },
            { "AggregateId", AggregateId },
            { "EventType", EventType },
            { "OccurredAt", OccurredAt }
        };
    }
}

/// <summary>
/// Interface für paginierte Antworten
/// </summary>
public class PaginatedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public PaginatedResponse(List<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}