using Avatales.Shared.Models;

namespace Avatales.Application.Users.DTOs;

/// <summary>
/// Basis-DTO für Benutzerinformationen
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public int? Age { get; set; }
    public UserRole PrimaryRole { get; set; }
    public SubscriptionType SubscriptionType { get; set; }
    public bool IsActive { get; set; }
    public bool IsEmailVerified { get; set; }
    public bool IsChildAccount { get; set; }
    public Guid? ParentUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

/// <summary>
/// Detaillierte Benutzerinformationen mit allen Daten
/// </summary>
public class UserDetailDto : UserDto
{
    public List<string> Roles { get; set; } = new();
    public List<UserDto> ChildUsers { get; set; } = new();
    public Dictionary<string, string> Preferences { get; set; } = new();
    public UserStatisticsDto Statistics { get; set; } = new();
    public UserLimitsDto CurrentLimits { get; set; } = new();
    public SubscriptionDetailDto? SubscriptionDetails { get; set; }
    public List<string> AllowedContentCategories { get; set; } = new();
    public List<string> RestrictedTopics { get; set; } = new();
    public TimeSpan? DailyUsageLimit { get; set; }
    public TimeSpan TotalUsageToday { get; set; }
    public bool HasReachedDailyLimit { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Benutzerprofil-DTO für Profile-Updates
/// </summary>
public class UserProfileDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public int? Age { get; set; }
    public UserRole PrimaryRole { get; set; }
    public SubscriptionType SubscriptionType { get; set; }
    public bool IsSubscriptionActive { get; set; }
    public DateTime? SubscriptionExpiresAt { get; set; }
    public Dictionary<string, string> Preferences { get; set; } = new();
    public UserStatisticsDto Statistics { get; set; } = new();
    public UserLimitsDto CurrentLimits { get; set; } = new();
    public bool IsChildAccount { get; set; }
    public List<UserDto> ChildUsers { get; set; } = new();
}

/// <summary>
/// Benutzer-Statistiken
/// </summary>
public class UserStatisticsDto
{
    public int CharactersCreated { get; set; }
    public int StoriesGenerated { get; set; }
    public int MonthlyStoryCount { get; set; }
    public DateTime MonthlyCountResetDate { get; set; }
    public int TotalReadingTimeMinutes { get; set; }
    public int LearningGoalsCompleted { get; set; }
    public int StoryShares { get; set; }
    public int CharacterShares { get; set; }
    public double AverageStoryRating { get; set; }
    public int DaysActive { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public Dictionary<string, int> GenrePreferences { get; set; } = new();
    public Dictionary<string, object> CustomStatistics { get; set; } = new();
}

/// <summary>
/// Benutzer-Limits basierend auf Abonnement
/// </summary>
public class UserLimitsDto
{
    public int MaxCharacters { get; set; }
    public int MonthlyStories { get; set; } // -1 für unbegrenzt
    public int StoriesRemaining { get; set; }
    public bool HasAdvancedFeatures { get; set; }
    public bool HasImageGeneration { get; set; }
    public bool HasPrioritySupport { get; set; }
    public bool CanShareCharacters { get; set; }
    public bool CanAccessCommunity { get; set; }
    public bool CanCreatePrivateStories { get; set; }
    public int MaxDailyUsageMinutes { get; set; } // Für Kinder-Accounts
}

/// <summary>
/// Abonnement-Details
/// </summary>
public class SubscriptionDetailDto
{
    public SubscriptionType Type { get; set; }
    public bool IsActive { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastBillingDate { get; set; }
    public DateTime? NextBillingDate { get; set; }
    public decimal MonthlyPrice { get; set; }
    public string Currency { get; set; } = "EUR";
    public bool AutoRenewal { get; set; }
    public List<string> IncludedFeatures { get; set; } = new();
    public bool IsTrialPeriod { get; set; }
    public DateTime? TrialEndsAt { get; set; }
}

/// <summary>
/// DTO für Benutzer-Registrierung
/// </summary>
public class UserRegistrationDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public UserRole Role { get; set; } = UserRole.Parent;
    public bool AcceptTerms { get; set; }
    public bool AcceptPrivacyPolicy { get; set; }
    public string? PreferredLanguage { get; set; }
    public string? ReferralCode { get; set; }
}

/// <summary>
/// DTO für Kinder-Benutzer-Erstellung
/// </summary>
public class CreateChildUserDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string? Email { get; set; } // Optional für ältere Kinder
    public List<string> AllowedContentCategories { get; set; } = new();
    public List<string> RestrictedTopics { get; set; } = new();
    public int DailyUsageLimitMinutes { get; set; } = 60;
    public bool RequireParentalApproval { get; set; } = true;
    public string? ParentalControlSettings { get; set; }
}

/// <summary>
/// DTO für Login-Anfrage
/// </summary>
public class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; } = false;
    public string? TwoFactorCode { get; set; }
}

/// <summary>
/// DTO für Login-Antwort
/// </summary>
public class LoginResponseDto
{
    public bool IsSuccess { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
    public UserDto? User { get; set; }
    public string? ErrorMessage { get; set; }
    public bool RequiresTwoFactor { get; set; }
    public bool RequiresEmailVerification { get; set; }
    public bool IsAccountLocked { get; set; }
    public DateTime? LockedUntil { get; set; }
}

/// <summary>
/// DTO für Passwort-Reset-Anfrage
/// </summary>
public class PasswordResetRequestDto
{
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// DTO für Passwort-Reset
/// </summary>
public class PasswordResetDto
{
    public string Email { get; set; } = string.Empty;
    public string ResetToken { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// DTO für Passwort-Änderung
/// </summary>
public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// DTO für Profil-Update
/// </summary>
public class UpdateUserProfileDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public Dictionary<string, string>? Preferences { get; set; }
}

/// <summary>
/// DTO für Avatar-Update
/// </summary>
public class UpdateUserAvatarDto
{
    public string AvatarUrl { get; set; } = string.Empty;
}

/// <summary>
/// DTO für Abonnement-Update
/// </summary>
public class UpdateSubscriptionDto
{
    public SubscriptionType SubscriptionType { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool AutoRenewal { get; set; } = true;
}

/// <summary>
/// DTO für Benutzer-Präferenzen
/// </summary>
public class UserPreferencesDto
{
    public Dictionary<string, string> Preferences { get; set; } = new();
    public string Language { get; set; } = "de-DE";
    public string Theme { get; set; } = "light";
    public bool EmailNotifications { get; set; } = true;
    public bool PushNotifications { get; set; } = true;
    public string StoryGenerationStyle { get; set; } = "balanced";
    public bool ImageGeneration { get; set; } = true;
    public string ContentFilter { get; set; } = "moderate";
    public bool EducationalFocus { get; set; } = true;
}

/// <summary>
/// DTO für Familienübersicht
/// </summary>
public class FamilyOverviewDto
{
    public UserDto ParentUser { get; set; } = new();
    public List<UserDto> ChildUsers { get; set; } = new();
    public FamilyStatisticsDto Statistics { get; set; } = new();
    public List<RecentActivityDto> RecentActivities { get; set; } = new();
    public UserLimitsDto FamilyLimits { get; set; } = new();
}

/// <summary>
/// DTO für Familien-Statistiken
/// </summary>
public class FamilyStatisticsDto
{
    public int TotalCharacters { get; set; }
    public int TotalStories { get; set; }
    public int TotalReadingTimeMinutes { get; set; }
    public int TotalLearningGoalsCompleted { get; set; }
    public Dictionary<string, int> PopularGenres { get; set; } = new();
    public Dictionary<string, int> LearningProgress { get; set; } = new();
    public DateTime LastActivityAt { get; set; }
}

/// <summary>
/// DTO für kürzliche Aktivitäten
/// </summary>
public class RecentActivityDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string ActivityType { get; set; } = string.Empty;
    public string ActivityDescription { get; set; } = string.Empty;
    public DateTime ActivityTimestamp { get; set; }
    public Dictionary<string, object> ActivityData { get; set; } = new();
}