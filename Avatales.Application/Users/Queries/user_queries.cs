using System.ComponentModel.DataAnnotations;
using Avatales.Application.Common.Interfaces;
using Avatales.Application.Users.DTOs;
using Avatales.Shared.Models;

namespace Avatales.Application.Users.Queries;

/// <summary>
/// Query: Benutzer nach ID abrufen
/// Lädt vollständige Benutzer-Details
/// </summary>
public class GetUserByIdQuery : IQuery<UserDetailDto>
{
    [Required]
    public Guid UserId { get; set; }

    public bool IncludeChildren { get; set; } = false;
    public bool IncludePreferences { get; set; } = false;
    public bool IncludeStatistics { get; set; } = false;

    public GetUserByIdQuery(Guid userId)
    {
        UserId = userId;
    }
}

/// <summary>
/// Query: Benutzer nach E-Mail abrufen
/// Für Login und Validierung
/// </summary>
public class GetUserByEmailQuery : IQuery<UserDto>
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public bool IncludeDeleted { get; set; } = false;

    public GetUserByEmailQuery(string email)
    {
        Email = email;
    }
}

/// <summary>
/// Query: Aktueller Benutzer-Profile
/// Lädt Profile des authentifizierten Benutzers
/// </summary>
public class GetCurrentUserProfileQuery : IQuery<UserProfileDto>
{
    public bool IncludeSubscriptionInfo { get; set; } = true;
    public bool IncludeStatistics { get; set; } = true;
}

/// <summary>
/// Query: Liste aller Kinder eines Eltern-Accounts
/// Für Familien-Management
/// </summary>
public class GetUserChildrenQuery : IQuery<List<UserDto>>
{
    [Required]
    public Guid ParentUserId { get; set; }

    public bool IncludeInactive { get; set; } = false;

    public GetUserChildrenQuery(Guid parentUserId)
    {
        ParentUserId = parentUserId;
    }
}

/// <summary>
/// Query: Benutzer-Präferenzen abrufen
/// Lädt alle Einstellungen eines Benutzers
/// </summary>
public class GetUserPreferencesQuery : IQuery<Dictionary<string, string>>
{
    [Required]
    public Guid UserId { get; set; }

    public List<string>? SpecificKeys { get; set; }

    public GetUserPreferencesQuery(Guid userId)
    {
        UserId = userId;
    }
}

/// <summary>
/// Query: Benutzer-Statistiken abrufen
/// Lädt Aktivitäts- und Nutzungsstatistiken
/// </summary>
public class GetUserStatisticsQuery : IQuery<UserStatisticsDto>
{
    [Required]
    public Guid UserId { get; set; }

    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool IncludeChildrenStats { get; set; } = false;

    public GetUserStatisticsQuery(Guid userId)
    {
        UserId = userId;
    }
}

/// <summary>
/// Query: Benutzer-Dashboard-Daten
/// Sammelt alle wichtigen Informationen für das Dashboard
/// </summary>
public class GetUserDashboardQuery : IQuery<UserDashboardDto>
{
    [Required]
    public Guid UserId { get; set; }

    public int RecentActivityDays { get; set; } = 7;
    public int MaxRecentItems { get; set; } = 10;

    public GetUserDashboardQuery(Guid userId)
    {
        UserId = userId;
    }
}

/// <summary>
/// Query: Benutzer suchen
/// Für Admin-Funktionen und Support
/// </summary>
public class SearchUsersQuery : IQuery<PaginatedResponse<UserDto>>
{
    public string? SearchTerm { get; set; }
    public UserRole? Role { get; set; }
    public SubscriptionType? SubscriptionType { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsEmailVerified { get; set; }
    public DateTime? RegisteredAfter { get; set; }
    public DateTime? RegisteredBefore { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
}

/// <summary>
/// Query: Prüfung ob E-Mail bereits verwendet wird
/// Für Registrierungs-Validierung
/// </summary>
public class CheckEmailExistsQuery : IQuery<bool>
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public CheckEmailExistsQuery(string email)
    {
        Email = email;
    }
}

/// <summary>
/// Query: Prüfung ob Benutzer bestimmte Berechtigung hat
/// Für Autorisierung
/// </summary>
public class CheckUserPermissionQuery : IQuery<bool>
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string Permission { get; set; } = string.Empty;

    public Guid? ResourceId { get; set; }
    public string? ResourceType { get; set; }

    public CheckUserPermissionQuery(Guid userId, string permission)
    {
        UserId = userId;
        Permission = permission;
    }
}

/// <summary>
/// Query: Benutzer-Aktivitätsverlauf
/// Zeigt letzte Aktionen und Logins
/// </summary>
public class GetUserActivityQuery : IQuery<List<UserActivityDto>>
{
    [Required]
    public Guid UserId { get; set; }

    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int MaxRecords { get; set; } = 50;
    public List<string>? ActivityTypes { get; set; }

    public GetUserActivityQuery(Guid userId)
    {
        UserId = userId;
    }
}

/// <summary>
/// Query: Abonnement-Informationen abrufen
/// Lädt Details zum aktuellen Abonnement
/// </summary>
public class GetUserSubscriptionQuery : IQuery<SubscriptionDetailDto>
{
    [Required]
    public Guid UserId { get; set; }

    public bool IncludeUsageStatistics { get; set; } = true;
    public bool IncludeBillingHistory { get; set; } = false;

    public GetUserSubscriptionQuery(Guid userId)
    {
        UserId = userId;
    }
}

/// <summary>
/// Query: Verfügbare Abonnement-Optionen
/// Für Upgrade/Downgrade-Entscheidungen
/// </summary>
public class GetAvailableSubscriptionsQuery : IQuery<List<SubscriptionOptionDto>>
{
    public Guid? CurrentUserId { get; set; }
    public SubscriptionType? CurrentSubscription { get; set; }
    public bool IncludePricing { get; set; } = true;
    public string? CurrencyCode { get; set; } = "EUR";
}

/// <summary>
/// Query: Prüfung ob Benutzer Feature nutzen kann
/// Für Feature-Gating basierend auf Abonnement
/// </summary>
public class CheckUserFeatureAccessQuery : IQuery<bool>
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string FeatureName { get; set; } = string.Empty;

    public CheckUserFeatureAccessQuery(Guid userId, string featureName)
    {
        UserId = userId;
        FeatureName = featureName;
    }
}

/// <summary>
/// Query: Benutzer-Nutzungslimits abrufen
/// Zeigt verfügbare und genutzte Kontingente
/// </summary>
public class GetUserUsageLimitsQuery : IQuery<UserUsageLimitsDto>
{
    [Required]
    public Guid UserId { get; set; }

    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }

    public GetUserUsageLimitsQuery(Guid userId)
    {
        UserId = userId;
    }
}

/// <summary>
/// Query: Familien-Übersicht für Eltern
/// Zeigt Informationen über alle Familienmitglieder
/// </summary>
public class GetFamilyOverviewQuery : IQuery<FamilyOverviewDto>
{
    [Required]
    public Guid ParentUserId { get; set; }

    public bool IncludeActivity { get; set; } = true;
    public bool IncludeStatistics { get; set; } = true;
    public int ActivityDays { get; set; } = 7;

    public GetFamilyOverviewQuery(Guid parentUserId)
    {
        ParentUserId = parentUserId;
    }
}

/// <summary>
/// Query: Benutzer-Empfehlungen
/// Schlägt Aktionen oder Inhalte vor
/// </summary>
public class GetUserRecommendationsQuery : IQuery<List<RecommendationDto>>
{
    [Required]
    public Guid UserId { get; set; }

    public List<string>? RecommendationTypes { get; set; }
    public int MaxRecommendations { get; set; } = 10;

    public GetUserRecommendationsQuery(Guid userId)
    {
        UserId = userId;
    }
}

/// <summary>
/// Query: Export-Daten für DSGVO-Compliance
/// Sammelt alle Benutzerdaten für Export
/// </summary>
public class GetUserDataExportQuery : IQuery<UserDataExportDto>
{
    [Required]
    public Guid UserId { get; set; }

    public List<string>? DataTypes { get; set; }
    public bool IncludeDeletedData { get; set; } = false;
    public string Format { get; set; } = "JSON";

    public GetUserDataExportQuery(Guid userId)
    {
        UserId = userId;
    }
}

/// <summary>
/// Query: Benutzer-Benachrichtigungseinstellungen
/// Lädt alle Notification-Präferenzen
/// </summary>
public class GetUserNotificationSettingsQuery : IQuery<UserNotificationSettingsDto>
{
    [Required]
    public Guid UserId { get; set; }

    public GetUserNotificationSettingsQuery(Guid userId)
    {
        UserId = userId;
    }
}

/// <summary>
/// Query: Prüfung auf ausstehende Benachrichtigungen
/// Für Badge-Counts und Alerts
/// </summary>
public class GetPendingNotificationsQuery : IQuery<List<NotificationDto>>
{
    [Required]
    public Guid UserId { get; set; }

    public bool MarkAsRead { get; set; } = false;
    public int MaxNotifications { get; set; } = 50;
    public List<string>? NotificationTypes { get; set; }

    public GetPendingNotificationsQuery(Guid userId)
    {
        UserId = userId;
    }
}

/// <summary>
/// Query: Benutzer-Einstellungen für Kindersicherheit
/// Lädt Parental Control Einstellungen
/// </summary>
public class GetParentalControlSettingsQuery : IQuery<ParentalControlSettingsDto>
{
    [Required]
    public Guid ParentUserId { get; set; }

    public Guid? ChildUserId { get; set; }

    public GetParentalControlSettingsQuery(Guid parentUserId)
    {
        ParentUserId = parentUserId;
    }
}

/// <summary>
/// Query: Session-Informationen des Benutzers
/// Zeigt aktive Sessions und Geräte
/// </summary>
public class GetUserSessionsQuery : IQuery<List<UserSessionDto>>
{
    [Required]
    public Guid UserId { get; set; }

    public bool IncludeExpiredSessions { get; set; } = false;
    public int MaxSessions { get; set; } = 20;

    public GetUserSessionsQuery(Guid userId)
    {
        UserId = userId;
    }
}