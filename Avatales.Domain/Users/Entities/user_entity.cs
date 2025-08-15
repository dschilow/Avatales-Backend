using Avatales.Domain.Users.Events;
using Avatales.Shared.Models;
using Avatales.Shared.Extensions;
using Avatales.Shared;
using Avatales.Shared.Interfaces;

namespace Avatales.Domain.Users.Entities;

/// <summary>
/// User Entity - Repräsentiert einen Benutzer im Avatales-System
/// Unterstützt Familien-Accounts, Kinder-Profile und Abonnement-Management
/// </summary>
public class User : BaseEntity, IUserTrackable, IActivatable
{
    private readonly List<Guid> _childUserIds = new();
    private readonly List<string> _roles = new();
    private readonly Dictionary<string, string> _preferences = new();
    private readonly Dictionary<string, object> _statistics = new();

    // Basis-Informationen
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? DisplayName { get; private set; }
    public string? AvatarUrl { get; private set; }
    public DateTime? DateOfBirth { get; private set; }
    public int? Age => DateOfBirth?.CalculateAge() ?? null;

    // Account-Status
    public bool IsEmailVerified { get; private set; } = false;
    public DateTime? EmailVerifiedAt { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime? DeactivatedAt { get; private set; }
    public string? DeactivationReason { get; private set; }

    // Rollen und Berechtigungen
    public UserRole PrimaryRole { get; private set; } = UserRole.FamilyMember;
    public IReadOnlyCollection<string> Roles => _roles.AsReadOnly();
    public Guid? ParentUserId { get; private set; } // Für Kinder-Accounts
    public IReadOnlyCollection<Guid> ChildUserIds => _childUserIds.AsReadOnly();

    // Abonnement und Limits
    public SubscriptionType SubscriptionType { get; private set; } = SubscriptionType.Free;
    public DateTime? SubscriptionExpiresAt { get; private set; }
    public bool IsSubscriptionActive => SubscriptionExpiresAt?.Date >= DateTime.UtcNow.Date;
    public int CharactersCreated { get; private set; } = 0;
    public int StoriesGenerated { get; private set; } = 0;
    public int MonthlyStoryCount { get; private set; } = 0;
    public DateTime MonthlyCountResetDate { get; private set; } = DateTime.UtcNow.AddMonths(1);

    // Sicherheit
    public int FailedLoginAttempts { get; private set; } = 0;
    public DateTime? LockedUntil { get; private set; }
    public bool IsLocked => LockedUntil.HasValue && LockedUntil > DateTime.UtcNow;
    public DateTime? LastLoginAt { get; private set; }
    public string? LastLoginIpAddress { get; private set; }

    // Präferenzen und Statistiken
    public IReadOnlyDictionary<string, string> Preferences => _preferences.AsReadOnly();
    public IReadOnlyDictionary<string, object> Statistics => _statistics.AsReadOnly();

    // Tracking
    public Guid? CreatedByUserId { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }

    // Kinder-spezifische Eigenschaften
    public bool IsChildAccount => ParentUserId.HasValue;
    public string? ParentalControlSettings { get; private set; }
    public List<string> AllowedContentCategories { get; private set; } = new();
    public List<string> RestrictedTopics { get; private set; } = new();
    public TimeSpan? DailyUsageLimit { get; private set; }
    public TimeSpan TotalUsageToday { get; private set; } = TimeSpan.Zero;
    public DateTime UsageTrackingDate { get; private set; } = DateTime.UtcNow.Date;

    protected User() { } // For EF Core

    /// <summary>
    /// Erstellt einen neuen Erwachsenen-Benutzer
    /// </summary>
    public User(
        string email,
        string passwordHash,
        string firstName,
        string lastName,
        UserRole role = UserRole.Parent)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        if (!IsValidEmail(email))
            throw new ArgumentException("Invalid email format", nameof(email));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty", nameof(passwordHash));

        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be empty", nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name cannot be empty", nameof(lastName));

        Email = email.Trim().ToLower();
        PasswordHash = passwordHash;
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        PrimaryRole = role;
        DisplayName = $"{FirstName} {LastName}";

        _roles.Add(role.ToString());

        // Initialisiere Standard-Präferenzen
        InitializeDefaultPreferences();

        AddDomainEvent(new UserRegisteredEvent(Id, Email, role, IsChildAccount));
    }

    /// <summary>
    /// Erstellt einen neuen Kinder-Benutzer
    /// </summary>
    public static User CreateChildUser(
        string email,
        string firstName,
        string lastName,
        Guid parentUserId,
        DateTime dateOfBirth,
        string? parentalControlSettings = null)
    {
        var childUser = new User(email, "CHILD_ACCOUNT", firstName, lastName, UserRole.FamilyMember)
        {
            ParentUserId = parentUserId,
            DateOfBirth = dateOfBirth,
            ParentalControlSettings = parentalControlSettings
        };

        childUser.SetupChildAccountDefaults();
        childUser.AddDomainEvent(new ChildUserCreatedEvent(childUser.Id, parentUserId, childUser.Age ?? 0));

        return childUser;
    }

    /// <summary>
    /// Verifiziert die E-Mail-Adresse
    /// </summary>
    public void VerifyEmail()
    {
        if (!IsEmailVerified)
        {
            IsEmailVerified = true;
            EmailVerifiedAt = DateTime.UtcNow;
            MarkAsUpdated();
            AddDomainEvent(new UserEmailVerifiedEvent(Id, Email));
        }
    }

    /// <summary>
    /// Aktualisiert das Profil
    /// </summary>
    public void UpdateProfile(
        string firstName,
        string lastName,
        string? displayName = null,
        DateTime? dateOfBirth = null)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be empty", nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name cannot be empty", nameof(lastName));

        var oldName = $"{FirstName} {LastName}";
        
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        DisplayName = displayName?.Trim() ?? $"{FirstName} {LastName}";
        
        if (dateOfBirth.HasValue && (!DateOfBirth.HasValue || DateOfBirth != dateOfBirth))
        {
            if (IsChildAccount && dateOfBirth.Value.CalculateAge() >= 18)
            {
                throw new InvalidOperationException("Child account cannot have age 18 or older");
            }
            DateOfBirth = dateOfBirth;
        }

        MarkAsUpdated();

        var newName = $"{FirstName} {LastName}";
        if (oldName != newName)
        {
            AddDomainEvent(new UserProfileUpdatedEvent(Id, oldName, newName));
        }
    }

    /// <summary>
    /// Ändert das Passwort
    /// </summary>
    public void ChangePassword(string newPasswordHash, string? oldPasswordHash = null)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("New password hash cannot be empty", nameof(newPasswordHash));

        // Für Kinder-Accounts ist kein altes Passwort erforderlich (Eltern können es ändern)
        if (!IsChildAccount && !string.IsNullOrWhiteSpace(PasswordHash))
        {
            if (string.IsNullOrWhiteSpace(oldPasswordHash))
                throw new ArgumentException("Old password hash required for adult accounts", nameof(oldPasswordHash));
        }

        PasswordHash = newPasswordHash;
        MarkAsUpdated();
        AddDomainEvent(new UserPasswordChangedEvent(Id));
    }

    /// <summary>
    /// Aktualisiert das Abonnement
    /// </summary>
    public void UpdateSubscription(SubscriptionType subscriptionType, DateTime? expiresAt = null)
    {
        var oldSubscription = SubscriptionType;
        SubscriptionType = subscriptionType;
        SubscriptionExpiresAt = expiresAt;

        // Reset monatliche Limits basierend auf neuem Abonnement
        ResetMonthlyLimits();

        MarkAsUpdated();
        AddDomainEvent(new UserSubscriptionChangedEvent(Id, oldSubscription, subscriptionType, expiresAt));
    }

    /// <summary>
    /// Fügt einen Kinder-Benutzer hinzu
    /// </summary>
    public void AddChildUser(Guid childUserId)
    {
        if (IsChildAccount)
            throw new InvalidOperationException("Child accounts cannot have child users");

        if (!_childUserIds.Contains(childUserId))
        {
            _childUserIds.Add(childUserId);
            MarkAsUpdated();
            AddDomainEvent(new ChildUserAddedEvent(Id, childUserId));
        }
    }

    /// <summary>
    /// Entfernt einen Kinder-Benutzer
    /// </summary>
    public void RemoveChildUser(Guid childUserId)
    {
        if (_childUserIds.Remove(childUserId))
        {
            MarkAsUpdated();
            AddDomainEvent(new ChildUserRemovedEvent(Id, childUserId));
        }
    }

    /// <summary>
    /// Versucht einen Login
    /// </summary>
    public LoginResult AttemptLogin(string ipAddress)
    {
        if (IsLocked)
        {
            return new LoginResult(false, "Account is locked", LockedUntil);
        }

        if (!IsActive)
        {
            return new LoginResult(false, "Account is deactivated", null);
        }

        if (!IsEmailVerified && !IsChildAccount)
        {
            return new LoginResult(false, "Email not verified", null);
        }

        // Erfolgreicher Login
        LastLoginAt = DateTime.UtcNow;
        LastLoginIpAddress = ipAddress;
        FailedLoginAttempts = 0;
        LockedUntil = null;

        MarkAsUpdated();
        AddDomainEvent(new UserLoggedInEvent(Id, ipAddress));

        return new LoginResult(true, "Login successful", null);
    }

    /// <summary>
    /// Registriert einen fehlgeschlagenen Login-Versuch
    /// </summary>
    public void RegisterFailedLogin(string ipAddress)
    {
        FailedLoginAttempts++;
        
        if (FailedLoginAttempts >= ApplicationConstants.Authentication.MaxLoginAttempts)
        {
            LockedUntil = DateTime.UtcNow.AddMinutes(ApplicationConstants.Authentication.LoginLockoutMinutes);
            AddDomainEvent(new UserAccountLockedEvent(Id, FailedLoginAttempts));
        }

        MarkAsUpdated();
        AddDomainEvent(new UserLoginFailedEvent(Id, ipAddress, FailedLoginAttempts));
    }

    /// <summary>
    /// Entsperrt das Konto
    /// </summary>
    public void UnlockAccount()
    {
        if (IsLocked)
        {
            LockedUntil = null;
            FailedLoginAttempts = 0;
            MarkAsUpdated();
            AddDomainEvent(new UserAccountUnlockedEvent(Id));
        }
    }

    /// <summary>
    /// Setzt oder aktualisiert eine Präferenz
    /// </summary>
    public void SetPreference(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Preference key cannot be empty", nameof(key));

        _preferences[key] = value ?? string.Empty;
        MarkAsUpdated();
    }

    /// <summary>
    /// Holt eine Präferenz
    /// </summary>
    public string? GetPreference(string key)
    {
        return _preferences.GetValueOrDefault(key);
    }

    /// <summary>
    /// Aktualisiert eine Statistik
    /// </summary>
    public void UpdateStatistic(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Statistic key cannot be empty", nameof(key));

        _statistics[key] = value;
        MarkAsUpdated();
    }

    /// <summary>
    /// Erhöht die Anzahl erstellter Charaktere
    /// </summary>
    public void IncrementCharactersCreated()
    {
        CharactersCreated++;
        UpdateStatistic("characters_created", CharactersCreated);
        MarkAsUpdated();
    }

    /// <summary>
    /// Erhöht die Anzahl generierter Geschichten
    /// </summary>
    public void IncrementStoriesGenerated()
    {
        StoriesGenerated++;
        
        // Prüfe ob monatlicher Reset erforderlich ist
        if (DateTime.UtcNow >= MonthlyCountResetDate)
        {
            ResetMonthlyLimits();
        }
        
        MonthlyStoryCount++;
        UpdateStatistic("stories_generated", StoriesGenerated);
        UpdateStatistic("monthly_stories", MonthlyStoryCount);
        MarkAsUpdated();
    }

    /// <summary>
    /// Prüft ob mehr Geschichten generiert werden können
    /// </summary>
    public bool CanGenerateMoreStories()
    {
        var monthlyLimit = GetMonthlyStoryLimit();
        return monthlyLimit == -1 || MonthlyStoryCount < monthlyLimit;
    }

    /// <summary>
    /// Trackt tägliche Nutzung für Kinder-Accounts
    /// </summary>
    public void TrackUsage(TimeSpan sessionDuration)
    {
        if (!IsChildAccount) return;

        // Reset wenn neuer Tag
        if (UsageTrackingDate.Date != DateTime.UtcNow.Date)
        {
            TotalUsageToday = TimeSpan.Zero;
            UsageTrackingDate = DateTime.UtcNow.Date;
        }

        TotalUsageToday = TotalUsageToday.Add(sessionDuration);
        MarkAsUpdated();
    }

    /// <summary>
    /// Prüft ob tägliches Nutzungslimit erreicht wurde
    /// </summary>
    public bool HasReachedDailyLimit()
    {
        if (!IsChildAccount || !DailyUsageLimit.HasValue) return false;
        return TotalUsageToday >= DailyUsageLimit.Value;
    }

    /// <summary>
    /// Aktiviert das Konto
    /// </summary>
    public void Activate()
    {
        if (!IsActive)
        {
            IsActive = true;
            DeactivatedAt = null;
            DeactivationReason = null;
            MarkAsUpdated();
            AddDomainEvent(new UserActivatedEvent(Id));
        }
    }

    /// <summary>
    /// Deaktiviert das Konto
    /// </summary>
    public void Deactivate(string reason = "")
    {
        if (IsActive)
        {
            IsActive = false;
            DeactivatedAt = DateTime.UtcNow;
            DeactivationReason = reason;
            MarkAsUpdated();
            AddDomainEvent(new UserDeactivatedEvent(Id, reason));
        }
    }

    /// <summary>
    /// Setzt Tracking-Informationen
    /// </summary>
    public void SetCreatedBy(Guid userId)
    {
        CreatedByUserId = userId;
    }

    public void SetUpdatedBy(Guid userId)
    {
        UpdatedByUserId = userId;
        MarkAsUpdated();
    }

    /// <summary>
    /// Berechnet Abonnement-Limits
    /// </summary>
    public UserLimits GetCurrentLimits()
    {
        return SubscriptionType switch
        {
            SubscriptionType.Free => new UserLimits(
                MaxCharacters: 1,
                MonthlyStories: 5,
                HasAdvancedFeatures: false,
                HasImageGeneration: false),
            
            SubscriptionType.Starter => new UserLimits(
                MaxCharacters: 3,
                MonthlyStories: 25,
                HasAdvancedFeatures: false,
                HasImageGeneration: true),
            
            SubscriptionType.Family => new UserLimits(
                MaxCharacters: 8,
                MonthlyStories: 100,
                HasAdvancedFeatures: true,
                HasImageGeneration: true),
            
            SubscriptionType.Premium => new UserLimits(
                MaxCharacters: 20,
                MonthlyStories: -1, // Unlimited
                HasAdvancedFeatures: true,
                HasImageGeneration: true),
            
            _ => new UserLimits(1, 5, false, false)
        };
    }

    private void InitializeDefaultPreferences()
    {
        _preferences["language"] = "de-DE";
        _preferences["theme"] = "light";
        _preferences["notifications_email"] = "true";
        _preferences["story_generation_style"] = "balanced";
        _preferences["image_generation"] = "true";
        
        if (IsChildAccount)
        {
            _preferences["content_filter"] = "strict";
            _preferences["educational_focus"] = "true";
        }
    }

    private void SetupChildAccountDefaults()
    {
        if (Age.HasValue)
        {
            // Altersgerechte Standardeinstellungen
            if (Age <= 6)
            {
                DailyUsageLimit = TimeSpan.FromMinutes(30);
                AllowedContentCategories.AddRange(new[] { "Education", "Friendship", "Family", "Nature" });
                RestrictedTopics.AddRange(new[] { "Violence", "Scary", "Complex Problems" });
            }
            else if (Age <= 10)
            {
                DailyUsageLimit = TimeSpan.FromHours(1);
                AllowedContentCategories.AddRange(new[] { "Adventure", "Mystery", "Science", "History" });
                RestrictedTopics.AddRange(new[] { "Violence", "Adult Themes" });
            }
            else
            {
                DailyUsageLimit = TimeSpan.FromHours(2);
                // Weniger Einschränkungen für ältere Kinder
            }
        }
    }

    private void ResetMonthlyLimits()
    {
        MonthlyStoryCount = 0;
        MonthlyCountResetDate = DateTime.UtcNow.AddMonths(1);
        UpdateStatistic("monthly_reset_date", MonthlyCountResetDate);
    }

    private int GetMonthlyStoryLimit()
    {
        return GetCurrentLimits().MonthlyStories;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Ergebnis eines Login-Versuchs
/// </summary>
public record LoginResult(
    bool IsSuccess,
    string Message,
    DateTime? LockedUntil
);

/// <summary>
/// Benutzer-Limits basierend auf Abonnement
/// </summary>
public record UserLimits(
    int MaxCharacters,
    int MonthlyStories, // -1 für unbegrenzt
    bool HasAdvancedFeatures,
    bool HasImageGeneration
);

/// <summary>
/// Extension für Altersberechnung
/// </summary>
public static class DateTimeExtensionsForAge
{
    public static int CalculateAge(this DateTime birthDate)
    {
        var today = DateTime.UtcNow.Date;
        var age = today.Year - birthDate.Year;
        
        if (birthDate.Date > today.AddYears(-age))
            age--;
            
        return age;
    }
}