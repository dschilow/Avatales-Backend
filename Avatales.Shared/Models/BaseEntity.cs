using Avatales.Shared.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Avatales.Shared.Models;

/// <summary>
/// Basis-Entität für alle Domain-Entitäten im Avatales-System
/// Implementiert gemeinsame Eigenschaften, Domain Events und Standard-Verhalten
/// </summary>
public abstract class BaseEntity : IEntity, IAuditable, ISoftDelete, IVersionable
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }
    public bool IsDeleted { get; protected set; } = false;
    public DateTime? DeletedAt { get; protected set; }
    public int Version { get; protected set; } = 1;

    // Domain Events Management
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Fügt ein Domain Event hinzu
    /// </summary>
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        if (domainEvent == null)
            throw new ArgumentNullException(nameof(domainEvent));

        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Entfernt alle Domain Events (nach Verarbeitung)
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    /// <summary>
    /// Markiert die Entität als aktualisiert und erhöht die Version
    /// </summary>
    protected void MarkAsUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();
    }

    /// <summary>
    /// Erhöht die Versionsnummer für Optimistic Locking
    /// </summary>
    public void IncrementVersion()
    {
        Version++;
    }

    /// <summary>
    /// Führt Soft Delete durch (logisches Löschen)
    /// </summary>
    public virtual void SoftDelete(string reason = "")
    {
        if (IsDeleted)
            return; // Bereits gelöscht

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        MarkAsUpdated();

        // Domain Event für Soft Delete
        AddDomainEvent(new EntitySoftDeletedEvent(Id, GetType().Name, reason));
    }

    /// <summary>
    /// Stellt eine soft-gelöschte Entität wieder her
    /// </summary>
    public virtual void Restore()
    {
        if (!IsDeleted)
            return; // Nicht gelöscht

        IsDeleted = false;
        DeletedAt = null;
        MarkAsUpdated();

        // Domain Event für Restore
        AddDomainEvent(new EntityRestoredEvent(Id, GetType().Name));
    }

    /// <summary>
    /// Prüft ob die Entität aktiv ist (nicht gelöscht)
    /// </summary>
    public bool IsActive => !IsDeleted;

    /// <summary>
    /// Validiert die Entität-Geschäftsregeln
    /// </summary>
    public virtual ValidationResult Validate()
    {
        var result = new ValidationResult();

        // Basis-Validierungen
        if (Id == Guid.Empty)
            result.AddError(nameof(Id), "ID cannot be empty");

        if (CreatedAt == default)
            result.AddError(nameof(CreatedAt), "CreatedAt must be set");

        if (Version < 1)
            result.AddError(nameof(Version), "Version must be at least 1");

        return result;
    }

    /// <summary>
    /// Führt Geschäftslogik-Validierung vor Änderungen durch
    /// </summary>
    protected virtual void ValidateInvariant()
    {
        var validationResult = Validate();
        if (!validationResult.IsValid)
        {
            throw new InvalidOperationException(
                $"Entity validation failed: {string.Join(", ", validationResult.Errors)}");
        }
    }

    // Equality-Vergleich basierend auf ID
    protected bool Equals(BaseEntity other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        // Entitäten mit leerer ID sind nie gleich (außer sich selbst)
        if (Id == Guid.Empty || other.Id == Guid.Empty)
            return false;

        return Id.Equals(other.Id);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((BaseEntity)obj);
    }

    public override int GetHashCode()
    {
        // Nutze ID für Hash-Code, aber handle Empty-Guid
        return Id == Guid.Empty ? base.GetHashCode() : Id.GetHashCode();
    }

    public static bool operator ==(BaseEntity? left, BaseEntity? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(BaseEntity? left, BaseEntity? right)
    {
        return !Equals(left, right);
    }

    public override string ToString()
    {
        return $"{GetType().Name} [Id: {Id}, Created: {CreatedAt:yyyy-MM-dd HH:mm}, Version: {Version}]";
    }
}

/// <summary>
/// Domain Event für Soft Delete
/// </summary>
public class EntitySoftDeletedEvent : DomainEvent
{
    public string EntityType { get; }
    public string Reason { get; }

    public EntitySoftDeletedEvent(Guid entityId, string entityType, string reason)
        : base(entityId)
    {
        EntityType = entityType;
        Reason = reason;
    }

    public override string EventType => "EntitySoftDeleted";
}

/// <summary>
/// Domain Event für Restore
/// </summary>
public class EntityRestoredEvent : DomainEvent
{
    public string EntityType { get; }

    public EntityRestoredEvent(Guid entityId, string entityType)
        : base(entityId)
    {
        EntityType = entityType;
    }

    public override string EventType => "EntityRestored";
}

/// <summary>
/// Aggregate Root Basis-Klasse für DDD
/// Fügt zusätzliche Funktionalität für Aggregates hinzu
/// </summary>
public abstract class AggregateRoot : BaseEntity
{
    /// <summary>
    /// Geschützte Methode für das Hinzufügen von Child-Entities
    /// </summary>
    protected void AddChildEntity<T>(T entity, List<T> collection) where T : BaseEntity
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        if (collection.Contains(entity))
            return; // Bereits vorhanden

        collection.Add(entity);
        MarkAsUpdated();
    }

    /// <summary>
    /// Geschützte Methode für das Entfernen von Child-Entities
    /// </summary>
    protected bool RemoveChildEntity<T>(T entity, List<T> collection) where T : BaseEntity
    {
        if (entity == null)
            return false;

        var removed = collection.Remove(entity);
        if (removed)
        {
            MarkAsUpdated();
        }

        return removed;
    }

    /// <summary>
    /// Geschützte Methode für das Soft-Delete von Child-Entities
    /// </summary>
    protected void SoftDeleteChildEntity<T>(T entity, string reason = "") where T : BaseEntity
    {
        if (entity == null)
            return;

        entity.SoftDelete(reason);
        MarkAsUpdated();
    }
}

/// <summary>
/// Extension-Methoden für BaseEntity
/// </summary>
public static class BaseEntityExtensions
{
    /// <summary>
    /// Prüft ob die Entität in den letzten X Tagen erstellt wurde
    /// </summary>
    public static bool IsCreatedWithinDays(this BaseEntity entity, int days)
    {
        return entity.CreatedAt >= DateTime.UtcNow.AddDays(-days);
    }

    /// <summary>
    /// Prüft ob die Entität in den letzten X Tagen aktualisiert wurde
    /// </summary>
    public static bool IsUpdatedWithinDays(this BaseEntity entity, int days)
    {
        return entity.UpdatedAt >= DateTime.UtcNow.AddDays(-days);
    }

    /// <summary>
    /// Gibt das Alter der Entität in Tagen zurück
    /// </summary>
    public static int GetAgeInDays(this BaseEntity entity)
    {
        return (int)(DateTime.UtcNow - entity.CreatedAt).TotalDays;
    }
}