using MediatR;
using Avatales.Shared.Models;

namespace Avatales.Application.Common.Interfaces;

/// <summary>
/// Basis-Interface für alle Commands im CQRS-Pattern
/// Commands ändern den Zustand des Systems
/// </summary>
public interface ICommand : IRequest<ApiResponse>
{
}

/// <summary>
/// Basis-Interface für Commands mit Rückgabewert
/// </summary>
public interface ICommand<TResponse> : IRequest<ApiResponse<TResponse>>
{
}

/// <summary>
/// Basis-Interface für alle Queries im CQRS-Pattern
/// Queries lesen nur Daten und ändern den Zustand nicht
/// </summary>
public interface IQuery<TResponse> : IRequest<ApiResponse<TResponse>>
{
}

/// <summary>
/// Basis-Interface für Command Handlers
/// </summary>
public interface ICommandHandler<TCommand> : IRequestHandler<TCommand, ApiResponse>
    where TCommand : ICommand
{
}

/// <summary>
/// Basis-Interface für Command Handlers mit Rückgabewert
/// </summary>
public interface ICommandHandler<TCommand, TResponse> : IRequestHandler<TCommand, ApiResponse<TResponse>>
    where TCommand : ICommand<TResponse>
{
}

/// <summary>
/// Basis-Interface für Query Handlers
/// </summary>
public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, ApiResponse<TResponse>>
    where TQuery : IQuery<TResponse>
{
}

/// <summary>
/// Interface für Domain Event Handlers
/// </summary>
public interface IDomainEventHandler<TEvent> : INotificationHandler<TEvent>
    where TEvent : IDomainEvent
{
}

/// <summary>
/// Interface für Application Services
/// Koordiniert Geschäftslogik zwischen Entities
/// </summary>
public interface IApplicationService
{
}

/// <summary>
/// Interface für Repository Pattern
/// Abstrahiert Datenzugriff von der Geschäftslogik
/// </summary>
public interface IRepository<TEntity> where TEntity : BaseEntity
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface für Unit of Work Pattern
/// Verwaltet Transaktionen über mehrere Repositories
/// </summary>
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface für Domain Event Dispatcher
/// Koordiniert das Versenden von Domain Events
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchEventsAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
    Task DispatchEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface für Cache Service
/// Abstrahiert Caching-Implementierung
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task RemovePatternAsync(string pattern, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface für Current User Context
/// Bietet Zugriff auf den aktuell authentifizierten Benutzer
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    string? Name { get; }
    bool IsAuthenticated { get; }
    List<string> Roles { get; }
    bool IsInRole(string role);
    bool HasPermission(string permission);
    Task<bool> CanAccessResourceAsync(Guid resourceId, string resourceType, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface für DateTime Provider
/// Ermöglicht Testbarkeit von zeitabhängigen Operationen
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
    DateTime Now { get; }
    DateOnly Today { get; }
    TimeOnly TimeOfDay { get; }
}

/// <summary>
/// Interface für File Storage Service
/// Abstrahiert Dateispeicherung (lokale Dateien, Cloud Storage, etc.)
/// </summary>
public interface IFileStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<Stream> DownloadFileAsync(string fileUrl, CancellationToken cancellationToken = default);
    Task<bool> DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default);
    Task<string> GetFileUrlAsync(string fileName, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task<bool> FileExistsAsync(string fileUrl, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface für Notification Service
/// Sendet Benachrichtigungen an Benutzer (E-Mail, Push, SMS, etc.)
/// </summary>
public interface INotificationService
{
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default);
    Task SendPushNotificationAsync(Guid userId, string title, string message, Dictionary<string, object>? data = null, CancellationToken cancellationToken = default);
    Task SendBulkNotificationAsync(List<Guid> userIds, string title, string message, CancellationToken cancellationToken = default);
    Task<bool> IsNotificationEnabledAsync(Guid userId, string notificationType, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface für AI Services
/// Abstrahiert KI-Integration (OpenAI, Hugging Face, etc.)
/// </summary>
public interface IAIService
{
    Task<string> GenerateTextAsync(string prompt, int maxTokens = 1000, CancellationToken cancellationToken = default);
    Task<string> GenerateImageAsync(string prompt, string? style = null, CancellationToken cancellationToken = default);
    Task<bool> ModerateContentAsync(string content, CancellationToken cancellationToken = default);
    Task<List<string>> ExtractKeywordsAsync(string text, int maxKeywords = 10, CancellationToken cancellationToken = default);
    Task<double> CalculateSimilarityAsync(string text1, string text2, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface für Validation Service
/// Zentrale Validierungslogik für komplexe Geschäftsregeln
/// </summary>
public interface IValidationService
{
    Task<List<string>> ValidateEntityAsync<T>(T entity, CancellationToken cancellationToken = default) where T : BaseEntity;
    Task<bool> IsValidEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> IsContentChildFriendlyAsync(string content, CancellationToken cancellationToken = default);
    Task<List<string>> ValidateUserPermissionsAsync(Guid userId, string operation, Guid? resourceId = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface für Background Job Service
/// Führt asynchrone Aufgaben im Hintergrund aus
/// </summary>
public interface IBackgroundJobService
{
    Task<string> EnqueueJobAsync<T>(string methodName, object[] parameters, CancellationToken cancellationToken = default) where T : class;
    Task<string> ScheduleJobAsync<T>(string methodName, object[] parameters, TimeSpan delay, CancellationToken cancellationToken = default) where T : class;
    Task<string> ScheduleRecurringJobAsync<T>(string jobId, string methodName, object[] parameters, string cronExpression, CancellationToken cancellationToken = default) where T : class;
    Task<bool> DeleteJobAsync(string jobId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface für Analytics Service
/// Sammelt und verarbeitet Analytik-Daten
/// </summary>
public interface IAnalyticsService
{
    Task TrackEventAsync(string eventName, Guid? userId = null, Dictionary<string, object>? properties = null, CancellationToken cancellationToken = default);
    Task TrackUserActionAsync(Guid userId, string action, Dictionary<string, object>? context = null, CancellationToken cancellationToken = default);
    Task<Dictionary<string, object>> GetAnalyticsAsync(string metricType, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    Task<List<Dictionary<string, object>>> GetTopPerformingItemsAsync(string itemType, int count = 10, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface für Search Service
/// Bietet Such- und Filterfunktionalität
/// </summary>
public interface ISearchService
{
    Task<List<T>> SearchAsync<T>(string query, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default) where T : class;
    Task<List<T>> SearchAsync<T>(SearchFilter filter, CancellationToken cancellationToken = default) where T : class;
    Task IndexEntityAsync<T>(T entity, CancellationToken cancellationToken = default) where T : BaseEntity;
    Task RemoveFromIndexAsync<T>(Guid entityId, CancellationToken cancellationToken = default) where T : BaseEntity;
}

/// <summary>
/// Search Filter für erweiterte Suchanfragen
/// </summary>
public class SearchFilter
{
    public string Query { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public Dictionary<string, object> Filters { get; set; } = new();
    public List<SortOption> SortBy { get; set; } = new();
    public List<string> IncludeFields { get; set; } = new();
    public List<string> ExcludeFields { get; set; } = new();
}

/// <summary>
/// Sort Option für Suchergebnisse
/// </summary>
public class SortOption
{
    public string Field { get; set; } = string.Empty;
    public bool Descending { get; set; } = false;
}

/// <summary>
/// Interface für Rate Limiting
/// Kontrolliert API-Nutzung und verhindert Missbrauch
/// </summary>
public interface IRateLimitService
{
    Task<bool> IsAllowedAsync(string key, int maxRequests, TimeSpan window, CancellationToken cancellationToken = default);
    Task<RateLimitInfo> GetRateLimitInfoAsync(string key, CancellationToken cancellationToken = default);
    Task ResetRateLimitAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>
/// Rate Limit Information
/// </summary>
public class RateLimitInfo
{
    public int RequestsRemaining { get; set; }
    public DateTime WindowResetTime { get; set; }
    public bool IsLimitExceeded { get; set; }
}

/// <summary>
/// Interface für Feature Flags
/// Ermöglicht A/B-Tests und schrittweise Feature-Einführung
/// </summary>
public interface IFeatureFlagService
{
    Task<bool> IsEnabledAsync(string featureName, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<T> GetFeatureValueAsync<T>(string featureName, T defaultValue, Guid? userId = null, CancellationToken cancellationToken = default);
    Task SetFeatureFlagAsync(string featureName, bool enabled, Guid? userId = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface für Audit Service
/// Protokolliert wichtige Systemereignisse für Compliance
/// </summary>
public interface IAuditService
{
    Task LogActionAsync(string action, Guid? userId = null, Guid? resourceId = null, object? oldValue = null, object? newValue = null, CancellationToken cancellationToken = default);
    Task LogSecurityEventAsync(string eventType, Guid? userId = null, string? details = null, CancellationToken cancellationToken = default);
    Task<List<AuditEntry>> GetAuditTrailAsync(Guid? userId = null, Guid? resourceId = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Audit Entry für Protokollierung
/// </summary>
public class AuditEntry
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public Guid? ResourceId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime Timestamp { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}