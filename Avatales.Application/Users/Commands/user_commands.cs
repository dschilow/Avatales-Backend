using System.ComponentModel.DataAnnotations;
using Avatales.Application.Common.Interfaces;
using Avatales.Application.Users.DTOs;
using Avatales.Shared.Models;

namespace Avatales.Application.Users.Commands;

/// <summary>
/// Command: Neuen Benutzer registrieren
/// </summary>
public class RegisterUserCommand : ICommand<UserDto>
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(ApplicationConstants.Authentication.MinPasswordLength)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string LastName { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }

    public UserRole Role { get; set; } = UserRole.Parent;

    [Required]
    public bool AcceptTerms { get; set; }

    [Required]
    public bool AcceptPrivacyPolicy { get; set; }

    public string? PreferredLanguage { get; set; }
    public string? ReferralCode { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public RegisterUserCommand(
        string email, 
        string password, 
        string firstName, 
        string lastName,
        bool acceptTerms,
        bool acceptPrivacyPolicy)
    {
        Email = email;
        Password = password;
        FirstName = firstName;
        LastName = lastName;
        AcceptTerms = acceptTerms;
        AcceptPrivacyPolicy = acceptPrivacyPolicy;
    }
}

/// <summary>
/// Command: Kinder-Benutzer erstellen
/// </summary>
public class CreateChildUserCommand : ICommand<UserDto>
{
    [Required]
    public Guid ParentUserId { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public DateTime DateOfBirth { get; set; }

    [EmailAddress]
    public string? Email { get; set; } // Optional für ältere Kinder

    public List<string> AllowedContentCategories { get; set; } = new();
    public List<string> RestrictedTopics { get; set; } = new();

    [Range(15, 300)] // 15 Minuten bis 5 Stunden täglich
    public int DailyUsageLimitMinutes { get; set; } = 60;

    public bool RequireParentalApproval { get; set; } = true;
    public string? ParentalControlSettings { get; set; }

    public CreateChildUserCommand(
        Guid parentUserId,
        string firstName,
        string lastName,
        DateTime dateOfBirth)
    {
        ParentUserId = parentUserId;
        FirstName = firstName;
        LastName = lastName;
        DateOfBirth = dateOfBirth;
    }
}

/// <summary>
/// Command: Benutzer-Profil aktualisieren
/// </summary>
public class UpdateUserProfileCommand : ICommand<UserProfileDto>
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? DisplayName { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public Dictionary<string, string>? Preferences { get; set; }

    public UpdateUserProfileCommand(Guid userId, string firstName, string lastName)
    {
        UserId = userId;
        FirstName = firstName;
        LastName = lastName;
    }
}

/// <summary>
/// Command: Benutzer-Avatar aktualisieren
/// </summary>
public class UpdateUserAvatarCommand : ICommand<string>
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Stream FileStream { get; set; } = null!;

    [Required]
    public string FileName { get; set; } = string.Empty;

    [Required]
    public string ContentType { get; set; } = string.Empty;

    [Range(1, ApplicationConstants.FileUpload.MaxImageSizeMB * 1024 * 1024)]
    public long FileSize { get; set; }

    public UpdateUserAvatarCommand(Guid userId, Stream fileStream, string fileName, string contentType, long fileSize)
    {
        UserId = userId;
        FileStream = fileStream;
        FileName = fileName;
        ContentType = contentType;
        FileSize = fileSize;
    }
}

/// <summary>
/// Command: Passwort ändern
/// </summary>
public class ChangePasswordCommand : ICommand
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(ApplicationConstants.Authentication.MinPasswordLength)]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; set; } = string.Empty;

    public ChangePasswordCommand(Guid userId, string currentPassword, string newPassword, string confirmPassword)
    {
        UserId = userId;
        CurrentPassword = currentPassword;
        NewPassword = newPassword;
        ConfirmPassword = confirmPassword;
    }
}

/// <summary>
/// Command: Passwort zurücksetzen anfordern
/// </summary>
public class RequestPasswordResetCommand : ICommand
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? ClientBaseUrl { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public RequestPasswordResetCommand(string email)
    {
        Email = email;
    }
}

/// <summary>
/// Command: Passwort mit Reset-Token zurücksetzen
/// </summary>
public class ResetPasswordCommand : ICommand
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string ResetToken { get; set; } = string.Empty;

    [Required]
    [MinLength(ApplicationConstants.Authentication.MinPasswordLength)]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; set; } = string.Empty;

    public ResetPasswordCommand(string email, string resetToken, string newPassword, string confirmPassword)
    {
        Email = email;
        ResetToken = resetToken;
        NewPassword = newPassword;
        ConfirmPassword = confirmPassword;
    }
}

/// <summary>
/// Command: E-Mail verifizieren
/// </summary>
public class VerifyEmailCommand : ICommand
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string VerificationToken { get; set; } = string.Empty;

    public VerifyEmailCommand(Guid userId, string verificationToken)
    {
        UserId = userId;
        VerificationToken = verificationToken;
    }
}

/// <summary>
/// Command: E-Mail-Verifikation erneut senden
/// </summary>
public class ResendEmailVerificationCommand : ICommand
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? ClientBaseUrl { get; set; }

    public ResendEmailVerificationCommand(string email)
    {
        Email = email;
    }
}

/// <summary>
/// Command: Abonnement aktualisieren
/// </summary>
public class UpdateSubscriptionCommand : ICommand<SubscriptionDetailDto>
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public SubscriptionType SubscriptionType { get; set; }

    public DateTime? ExpiresAt { get; set; }
    public bool AutoRenewal { get; set; } = true;
    public string? PaymentMethodId { get; set; }
    public decimal? CustomPrice { get; set; }
    public string Currency { get; set; } = "EUR";

    public UpdateSubscriptionCommand(Guid userId, SubscriptionType subscriptionType)
    {
        UserId = userId;
        SubscriptionType = subscriptionType;
    }
}

/// <summary>
/// Command: Benutzer deaktivieren
/// </summary>
public class DeactivateUserCommand : ICommand
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string Reason { get; set; } = string.Empty;

    public bool NotifyUser { get; set; } = true;
    public DateTime? ScheduledDate { get; set; } // Für geplante Deaktivierung

    public DeactivateUserCommand(Guid userId, string reason)
    {
        UserId = userId;
        Reason = reason;
    }
}

/// <summary>
/// Command: Benutzer aktivieren
/// </summary>
public class ActivateUserCommand : ICommand
{
    [Required]
    public Guid UserId { get; set; }

    public string? ActivationReason { get; set; }
    public bool NotifyUser { get; set; } = true;

    public ActivateUserCommand(Guid userId)
    {
        UserId = userId;
    }
}

/// <summary>
/// Command: Benutzer-Konto entsperren
/// </summary>
public class UnlockUserAccountCommand : ICommand
{
    [Required]
    public Guid UserId { get; set; }

    public string? UnlockReason { get; set; }
    public bool ResetFailedAttempts { get; set; } = true;
    public bool NotifyUser { get; set; } = true;

    public UnlockUserAccountCommand(Guid userId)
    {
        UserId = userId;
    }
}

/// <summary>
/// Command: Benutzer-Präferenzen aktualisieren
/// </summary>
public class UpdateUserPreferencesCommand : ICommand
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Dictionary<string, string> Preferences { get; set; } = new();

    public bool MergeWithExisting { get; set; } = true;
    public bool ValidatePreferences { get; set; } = true;

    public UpdateUserPreferencesCommand(Guid userId, Dictionary<string, string> preferences)
    {
        UserId = userId;
        Preferences = preferences;
    }
}

/// <summary>
/// Command: Kinder-Benutzer zu Eltern-Account hinzufügen
/// </summary>
public class AddChildToParentCommand : ICommand
{
    [Required]
    public Guid ParentUserId { get; set; }

    [Required]
    public Guid ChildUserId { get; set; }

    public string? AdditionReason { get; set; }
    public bool TransferOwnership { get; set; } = false; // Für bestehende Accounts

    public AddChildToParentCommand(Guid parentUserId, Guid childUserId)
    {
        ParentUserId = parentUserId;
        ChildUserId = childUserId;
    }
}

/// <summary>
/// Command: Kinder-Benutzer von Eltern-Account entfernen
/// </summary>
public class RemoveChildFromParentCommand : ICommand
{
    [Required]
    public Guid ParentUserId { get; set; }

    [Required]
    public Guid ChildUserId { get; set; }

    [Required]
    public string RemovalReason { get; set; } = string.Empty;

    public bool DeactivateChild { get; set; } = false;
    public bool NotifyParent { get; set; } = true;

    public RemoveChildFromParentCommand(Guid parentUserId, Guid childUserId, string removalReason)
    {
        ParentUserId = parentUserId;
        ChildUserId = childUserId;
        RemovalReason = removalReason;
    }
}

/// <summary>
/// Command: Benutzer-Statistiken aktualisieren
/// </summary>
public class UpdateUserStatisticsCommand : ICommand
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Dictionary<string, object> Statistics { get; set; } = new();

    public bool IncrementCounters { get; set; } = true;
    public DateTime? TimestampOverride { get; set; }

    public UpdateUserStatisticsCommand(Guid userId, Dictionary<string, object> statistics)
    {
        UserId = userId;
        Statistics = statistics;
    }
}

/// <summary>
/// Command: Benutzer-Daten exportieren (GDPR-Compliance)
/// </summary>
public class ExportUserDataCommand : ICommand<string>
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string Format { get; set; } = "json"; // json, xml, csv

    public bool IncludeChildren { get; set; } = true;
    public bool IncludeCharacters { get; set; } = true;
    public bool IncludeStories { get; set; } = true;
    public bool IncludeStatistics { get; set; } = true;
    public List<string> ExcludeFields { get; set; } = new();

    public ExportUserDataCommand(Guid userId, string format = "json")
    {
        UserId = userId;
        Format = format;
    }
}

/// <summary>
/// Command: Benutzer-Account löschen (GDPR Right to be Forgotten)
/// </summary>
public class DeleteUserAccountCommand : ICommand
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string DeletionReason { get; set; } = string.Empty;

    public bool HardDelete { get; set; } = false; // Soft delete by default
    public bool DeleteChildren { get; set; } = false;
    public bool AnonymizeData { get; set; } = true;
    public int RetentionDays { get; set; } = 30; // Für Soft Delete

    public DeleteUserAccountCommand(Guid userId, string deletionReason)
    {
        UserId = userId;
        DeletionReason = deletionReason;
    }
}