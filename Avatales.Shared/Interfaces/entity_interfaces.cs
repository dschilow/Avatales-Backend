namespace Avatales.Shared.Interfaces;

/// <summary>
/// Basis-Interface für alle Entitäten
/// Definiert gemeinsame Identifikations-Eigenschaften
/// </summary>
public interface IEntity
{
    Guid Id { get; }
}

/// <summary>
/// Interface für auditierbare Entitäten
/// Verfolgt Erstellungs- und Änderungszeiten
/// </summary>
public interface IAuditable
{
    DateTime CreatedAt { get; }
    DateTime? UpdatedAt { get; }
}

/// <summary>
/// Interface für Soft-Delete-Funktionalität
/// Ermöglicht das logische Löschen anstatt physisches Entfernen
/// </summary>
public interface ISoftDelete
{
    bool IsDeleted { get; }
    DateTime? DeletedAt { get; }
    void SoftDelete(string reason = "");
    void Restore();
}

/// <summary>
/// Interface für Entitäten mit Versionierung
/// Unterstützt Optimistic Locking und Konfliktbehandlung
/// </summary>
public interface IVersionable
{
    int Version { get; }
    void IncrementVersion();
}

/// <summary>
/// Interface für Entitäten mit Aktivitätsstatus
/// Ermöglicht Aktivierung/Deaktivierung ohne Löschung
/// </summary>
public interface IActivatable
{
    bool IsActive { get; }
    DateTime? DeactivatedAt { get; }
    void Activate();
    void Deactivate(string reason = "");
}

/// <summary>
/// Interface für Entitäten mit Benutzer-Zuordnung
/// Verfolgt Ersteller und letzte Bearbeiter
/// </summary>
public interface IUserTrackable
{
    Guid? CreatedByUserId { get; }
    Guid? UpdatedByUserId { get; }
    void SetCreatedBy(Guid userId);
    void SetUpdatedBy(Guid userId);
}

/// <summary>
/// Interface für Entitäten mit Tenant-Isolation
/// Unterstützt Multi-Tenancy für SaaS-Anwendungen
/// </summary>
public interface ITenantIsolated
{
    Guid? TenantId { get; }
}

/// <summary>
/// Interface für Entitäten mit Tags/Labels
/// Ermöglicht flexible Kategorisierung und Filterung
/// </summary>
public interface ITaggable
{
    IReadOnlyCollection<string> Tags { get; }
    void AddTag(string tag);
    void RemoveTag(string tag);
    void ClearTags();
    bool HasTag(string tag);
}

/// <summary>
/// Interface für Entitäten mit Metadaten
/// Flexibler Key-Value-Store für zusätzliche Eigenschaften
/// </summary>
public interface IHasMetadata
{
    IReadOnlyDictionary<string, object> Metadata { get; }
    void SetMetadata(string key, object value);
    T? GetMetadata<T>(string key);
    void RemoveMetadata(string key);
    void ClearMetadata();
}