using Avatales.Shared.Models;

namespace Avatales.Domain.Users.Events;

/// <summary>
/// Event: Neuer Benutzer wurde registriert
/// </summary>
public class UserRegisteredEvent : DomainEvent
{
    public string Email { get; }
    public UserRole Role { get; }
    public bool IsChildAccount { get; }
    public DateTime RegistrationTimestamp { get; }

    public UserRegisteredEvent(Guid userId, string email, UserRole role, bool isChildAccount) 
        : base(userId)
    {
        Email = email;
        Role = role;
        IsChildAccount = isChildAccount;
        RegistrationTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "UserRegistered";
}

/// <summary>
/// Event: Kinder-Benutzer wurde erstellt
/// </summary>
public class ChildUserCreatedEvent : DomainEvent
{
    public Guid ParentUserId { get; }
    public int ChildAge { get; }
    public DateTime CreationTimestamp { get; }

    public ChildUserCreatedEvent(Guid childUserId, Guid parentUserId, int childAge) 
        : base(childUserId)
    {
        ParentUserId = parentUserId;
        ChildAge = childAge;
        CreationTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "ChildUserCreated";
}

/// <summary>
/// Event: E-Mail wurde verifiziert
/// </summary>
public class UserEmailVerifiedEvent : DomainEvent
{
    public string Email { get; }
    public DateTime VerificationTimestamp { get; }

    public UserEmailVerifiedEvent(Guid userId, string email) 
        : base(userId)
    {
        Email = email;
        VerificationTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "UserEmailVerified";
}

/// <summary>
/// Event: Benutzerprofil wurde aktualisiert
/// </summary>
public class UserProfileUpdatedEvent : DomainEvent
{
    public string OldName { get; }
    public string NewName { get; }
    public DateTime UpdateTimestamp { get; }

    public UserProfileUpdatedEvent(Guid userId, string oldName, string newName) 
        : base(userId)
    {
        OldName = oldName;
        NewName = newName;
        UpdateTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "UserProfileUpdated";
}

/// <summary>
/// Event: Passwort wurde geändert
/// </summary>
public class UserPasswordChangedEvent : DomainEvent
{
    public DateTime ChangeTimestamp { get; }

    public UserPasswordChangedEvent(Guid userId) 
        : base(userId)
    {
        ChangeTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "UserPasswordChanged";
}

/// <summary>
/// Event: Abonnement wurde geändert
/// </summary>
public class UserSubscriptionChangedEvent : DomainEvent
{
    public SubscriptionType OldSubscription { get; }
    public SubscriptionType NewSubscription { get; }
    public DateTime? ExpiresAt { get; }
    public DateTime ChangeTimestamp { get; }

    public UserSubscriptionChangedEvent(
        Guid userId, 
        SubscriptionType oldSubscription, 
        SubscriptionType newSubscription,
        DateTime? expiresAt) 
        : base(userId)
    {
        OldSubscription = oldSubscription;
        NewSubscription = newSubscription;
        ExpiresAt = expiresAt;
        ChangeTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "UserSubscriptionChanged";
}

/// <summary>
/// Event: Kinder-Benutzer wurde hinzugefügt
/// </summary>
public class ChildUserAddedEvent : DomainEvent
{
    public Guid ChildUserId { get; }
    public DateTime AddedTimestamp { get; }

    public ChildUserAddedEvent(Guid parentUserId, Guid childUserId) 
        : base(parentUserId)
    {
        ChildUserId = childUserId;
        AddedTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "ChildUserAdded";
}

/// <summary>
/// Event: Kinder-Benutzer wurde entfernt
/// </summary>
public class ChildUserRemovedEvent : DomainEvent
{
    public Guid ChildUserId { get; }
    public DateTime RemovedTimestamp { get; }

    public ChildUserRemovedEvent(Guid parentUserId, Guid childUserId) 
        : base(parentUserId)
    {
        ChildUserId = childUserId;
        RemovedTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "ChildUserRemoved";
}

/// <summary>
/// Event: Benutzer hat sich angemeldet
/// </summary>
public class UserLoggedInEvent : DomainEvent
{
    public string IpAddress { get; }
    public DateTime LoginTimestamp { get; }

    public UserLoggedInEvent(Guid userId, string ipAddress) 
        : base(userId)
    {
        IpAddress = ipAddress;
        LoginTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "UserLoggedIn";
}

/// <summary>
/// Event: Login-Versuch fehlgeschlagen
/// </summary>
public class UserLoginFailedEvent : DomainEvent
{
    public string IpAddress { get; }
    public int FailedAttempts { get; }
    public DateTime FailureTimestamp { get; }

    public UserLoginFailedEvent(Guid userId, string ipAddress, int failedAttempts) 
        : base(userId)
    {
        IpAddress = ipAddress;
        FailedAttempts = failedAttempts;
        FailureTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "UserLoginFailed";
}

/// <summary>
/// Event: Benutzer-Konto wurde gesperrt
/// </summary>
public class UserAccountLockedEvent : DomainEvent
{
    public int FailedAttempts { get; }
    public DateTime LockedTimestamp { get; }

    public UserAccountLockedEvent(Guid userId, int failedAttempts) 
        : base(userId)
    {
        FailedAttempts = failedAttempts;
        LockedTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "UserAccountLocked";
}

/// <summary>
/// Event: Benutzer-Konto wurde entsperrt
/// </summary>
public class UserAccountUnlockedEvent : DomainEvent
{
    public DateTime UnlockedTimestamp { get; }

    public UserAccountUnlockedEvent(Guid userId) 
        : base(userId)
    {
        UnlockedTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "UserAccountUnlocked";
}

/// <summary>
/// Event: Benutzer wurde aktiviert
/// </summary>
public class UserActivatedEvent : DomainEvent
{
    public DateTime ActivatedTimestamp { get; }

    public UserActivatedEvent(Guid userId) 
        : base(userId)
    {
        ActivatedTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "UserActivated";
}

/// <summary>
/// Event: Benutzer wurde deaktiviert
/// </summary>
public class UserDeactivatedEvent : DomainEvent
{
    public string Reason { get; }
    public DateTime DeactivatedTimestamp { get; }

    public UserDeactivatedEvent(Guid userId, string reason) 
        : base(userId)
    {
        Reason = reason;
        DeactivatedTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "UserDeactivated";
}

/// <summary>
/// Event: Benutzer hat tägliches Limit erreicht (für Kinder-Accounts)
/// </summary>
public class UserDailyLimitReachedEvent : DomainEvent
{
    public TimeSpan UsageTime { get; }
    public TimeSpan DailyLimit { get; }
    public DateTime LimitReachedTimestamp { get; }

    public UserDailyLimitReachedEvent(Guid userId, TimeSpan usageTime, TimeSpan dailyLimit) 
        : base(userId)
    {
        UsageTime = usageTime;
        DailyLimit = dailyLimit;
        LimitReachedTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "UserDailyLimitReached";
}

/// <summary>
/// Event: Benutzer hat monatliches Story-Limit erreicht
/// </summary>
public class UserMonthlyStoryLimitReachedEvent : DomainEvent
{
    public int StoriesGenerated { get; }
    public int MonthlyLimit { get; }
    public SubscriptionType SubscriptionType { get; }
    public DateTime LimitReachedTimestamp { get; }

    public UserMonthlyStoryLimitReachedEvent(
        Guid userId, 
        int storiesGenerated, 
        int monthlyLimit, 
        SubscriptionType subscriptionType) 
        : base(userId)
    {
        StoriesGenerated = storiesGenerated;
        MonthlyLimit = monthlyLimit;
        SubscriptionType = subscriptionType;
        LimitReachedTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "UserMonthlyStoryLimitReached";
}