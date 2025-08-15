namespace Avatales.Shared.Constants;

/// <summary>
/// Zentrale Anwendungskonstanten für das Avatales-System
/// </summary>
public static class ApplicationConstants
{
    /// <summary>
    /// Authentifizierung und Sicherheit
    /// </summary>
    public static class Authentication
    {
        public const int AccessTokenExpirationMinutes = 60;
        public const int RefreshTokenExpirationDays = 30;
        public const int EmailVerificationTokenExpirationHours = 24;
        public const int PasswordResetTokenExpirationHours = 2;
        public const int MaxLoginAttempts = 5;
        public const int LoginLockoutMinutes = 15;
        public const int MinPasswordLength = 8;
        public const string JwtSecretKey = "AvatalesSecretKey2024!";
        public const string JwtIssuer = "Avatales";
        public const string JwtAudience = "AvatalesUsers";
    }

    /// <summary>
    /// Charaktere und Stories
    /// </summary>
    public static class Characters
    {
        public const int MaxCharactersPerUser = 10;
        public const int MaxCharacterNameLength = 50;
        public const int MaxCharacterDescriptionLength = 500;
        public const int MinCharacterNameLength = 2;
        public const int MaxMemoriesPerCharacter = 100;
        public const int MaxTraitsPerCharacter = 10;
        public const int DefaultTraitValue = 5;
        public const int MaxTraitValue = 10;
        public const int MinTraitValue = 1;
    }

    /// <summary>
    /// Geschichten
    /// </summary>
    public static class Stories
    {
        public const int MaxStoryTitleLength = 100;
        public const int MaxStoryContentLength = 5000;
        public const int MaxScenesPerStory = 10;
        public const int MaxLearningGoalsPerStory = 5;
        public const int MaxWordsPerScene = 300;
        public const int MinWordsPerScene = 10;
        public const int MaxImageUrlsPerStory = 10;
        public const int MaxTagsPerStory = 15;
        public const int DefaultReadingTimeMinutes = 5;
    }

    /// <summary>
    /// AI-Service Konfiguration
    /// </summary>
    public static class AIService
    {
        public const string DefaultModel = "gpt-4";
        public const int MaxTokensPerRequest = 4000;
        public const int MaxRetryAttempts = 3;
        public const decimal CostPerInputToken = 0.00005m;  // $0.05 per 1K tokens
        public const decimal CostPerOutputToken = 0.0004m;  // $0.40 per 1K tokens
        public const int TimeoutSeconds = 60;
        public const int MaxConcurrentRequests = 10;
    }

    /// <summary>
    /// Datei-Upload Konfiguration
    /// </summary>
    public static class FileUpload
    {
        public const int MaxImageSizeMB = 5;
        public const int MaxAudioSizeMB = 10;
        public const string AllowedImageTypes = ".jpg,.jpeg,.png,.gif,.webp";
        public const string AllowedAudioTypes = ".mp3,.wav,.ogg";
        public const string AvatarImagePath = "avatars";
        public const string StoryImagePath = "stories";
        public const string CharacterImagePath = "characters";
    }

    /// <summary>
    /// Abonnement-Limits
    /// </summary>
    public static class SubscriptionLimits
    {
        public static class Free
        {
            public const int StoriesPerDay = 3;
            public const int MaxCharacters = 2;
            public const int MaxSharedCharacters = 0;
            public const bool CanUseLearningMode = false;
            public const bool CanSharePublicly = false;
            public const bool HasImageGeneration = false;
        }

        public static class Starter
        {
            public const int StoriesPerDay = 10;
            public const int MaxCharacters = 5;
            public const int MaxSharedCharacters = 2;
            public const bool CanUseLearningMode = true;
            public const bool CanSharePublicly = true;
            public const bool HasImageGeneration = false;
            public const decimal MonthlyPrice = 4.99m;
        }

        public static class Family
        {
            public const int StoriesPerDay = 30;
            public const int MaxCharacters = 15;
            public const int MaxSharedCharacters = 10;
            public const bool CanUseLearningMode = true;
            public const bool CanSharePublicly = true;
            public const bool HasImageGeneration = true;
            public const decimal MonthlyPrice = 12.99m;
            public const int MaxChildren = 5;
        }

        public static class Premium
        {
            public const int StoriesPerDay = -1; // Unlimited
            public const int MaxCharacters = -1; // Unlimited
            public const int MaxSharedCharacters = -1; // Unlimited
            public const bool CanUseLearningMode = true;
            public const bool CanSharePublicly = true;
            public const bool HasImageGeneration = true;
            public const decimal MonthlyPrice = 24.99m;
            public const bool HasAdvancedFeatures = true;
        }

        public static class PremiumAdult
        {
            public const int StoriesPerDay = -1; // Unlimited
            public const int MaxCharacters = -1; // Unlimited
            public const bool HasAdultContent = true;
            public const bool HasTherapeuticStories = true;
            public const decimal MonthlyPrice = 19.99m;
        }
    }

    /// <summary>
    /// Cache-Konfiguration
    /// </summary>
    public static class Cache
    {
        public const int DefaultExpirationMinutes = 15;
        public const int UserProfileExpirationMinutes = 30;
        public const int CharacterDataExpirationMinutes = 60;
        public const int StoryContentExpirationMinutes = 120;
        public const int AnalyticsExpirationHours = 24;
        public const string UserProfileKeyPrefix = "user_profile_";
        public const string CharacterKeyPrefix = "character_";
        public const string StoryKeyPrefix = "story_";
    }

    /// <summary>
    /// API Rate Limiting
    /// </summary>
    public static class RateLimit
    {
        public const int RequestsPerMinute = 60;
        public const int AIRequestsPerHour = 100;
        public const int LoginAttemptsPerHour = 10;
        public const int RegistrationAttemptsPerDay = 3;
        public const int PasswordResetAttemptsPerDay = 5;
        public const int StoryGenerationRequestsPerHour = 20;
    }

    /// <summary>
    /// Validierung
    /// </summary>
    public static class Validation
    {
        public const int MinUserAge = 3;
        public const int MaxUserAge = 16;
        public const int MaxEmailLength = 255;
        public const int MaxNameLength = 100;
        public const int MinNameLength = 2;
        public const string EmailRegexPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        public const string PhoneRegexPattern = @"^\+?[1-9]\d{1,14}$";
    }

    /// <summary>
    /// Content Moderation
    /// </summary>
    public static class ContentModeration
    {
        public const int AutoApprovalThreshold = 90; // Confidence score 0-100
        public const int AutoRejectionThreshold = 20;
        public const int ManualReviewThreshold = 50;
        public const int MaxFlagsBeforeReview = 3;
        public const int MaxContentAnalysisRetries = 2;
    }

    /// <summary>
    /// Fehler-Nachrichten
    /// </summary>
    public static class ErrorMessages
    {
        public const string ValidationFailed = "Validierungsfehler sind aufgetreten";
        public const string Unauthorized = "Nicht autorisiert";
        public const string Forbidden = "Zugriff verweigert";
        public const string NotFound = "Ressource nicht gefunden";
        public const string InternalServerError = "Ein interner Serverfehler ist aufgetreten";
        public const string ServiceUnavailable = "Service temporär nicht verfügbar";
        public const string TooManyRequests = "Zu viele Anfragen";
        public const string BadRequest = "Ungültige Anfrage";
        public const string Conflict = "Konflikt - Ressource bereits vorhanden";
        public const string InvalidCredentials = "Ungültige Anmeldedaten";
        public const string EmailAlreadyExists = "E-Mail-Adresse bereits registriert";
        public const string InvalidToken = "Ungültiger oder abgelaufener Token";
        public const string SubscriptionLimitExceeded = "Abonnement-Limit erreicht";
        public const string ContentNotChildFriendly = "Inhalt nicht kinderfreundlich";
        public const string AIServiceError = "Fehler beim AI-Service";
        public const string FileUploadError = "Fehler beim Datei-Upload";
    }

    /// <summary>
    /// Erfolgs-Nachrichten
    /// </summary>
    public static class SuccessMessages
    {
        public const string Created = "Erfolgreich erstellt";
        public const string Updated = "Erfolgreich aktualisiert";
        public const string Deleted = "Erfolgreich gelöscht";
        public const string Authenticated = "Erfolgreich angemeldet";
        public const string EmailVerified = "E-Mail erfolgreich verifiziert";
        public const string PasswordReset = "Passwort erfolgreich zurückgesetzt";
        public const string ProfileUpdated = "Profil erfolgreich aktualisiert";
        public const string CharacterCreated = "Charakter erfolgreich erstellt";
        public const string StoryGenerated = "Geschichte erfolgreich generiert";
        public const string CharacterShared = "Charakter erfolgreich geteilt";
        public const string DataExported = "Daten erfolgreich exportiert";
    }

    /// <summary>
    /// Event-Namen für Domain Events
    /// </summary>
    public static class EventNames
    {
        // User Events
        public const string UserCreated = "UserCreated";
        public const string UserUpdated = "UserUpdated";
        public const string UserDeleted = "UserDeleted";
        public const string EmailVerified = "EmailVerified";
        public const string SubscriptionUpdated = "SubscriptionUpdated";

        // Character Events
        public const string CharacterCreated = "CharacterCreated";
        public const string CharacterUpdated = "CharacterUpdated";
        public const string CharacterShared = "CharacterShared";
        public const string CharacterLeveledUp = "CharacterLeveledUp";
        public const string TraitChanged = "TraitChanged";

        // Story Events
        public const string StoryCreated = "StoryCreated";
        public const string StoryGenerated = "StoryGenerated";
        public const string StoryPublished = "StoryPublished";
        public const string StoryViewed = "StoryViewed";
        public const string StoryRated = "StoryRated";
    }

    /// <summary>
    /// Background Job-Namen
    /// </summary>
    public static class BackgroundJobs
    {
        public const string StoryGeneration = "story-generation";
        public const string ImageGeneration = "image-generation";
        public const string ContentModeration = "content-moderation";
        public const string UserStatisticsUpdate = "user-statistics-update";
        public const string CharacterMemoryConsolidation = "character-memory-consolidation";
        public const string EmailNotification = "email-notification";
        public const string DataCleanup = "data-cleanup";
        public const string AnalyticsAggregation = "analytics-aggregation";
    }

    /// <summary>
    /// Paginierungs-Defaults
    /// </summary>
    public static class Pagination
    {
        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 100;
        public const int MinPageSize = 1;
        public const int DefaultPageNumber = 1;
    }

    /// <summary>
    /// Feature Flags
    /// </summary>
    public static class FeatureFlags
    {
        public const string AdvancedAI = "advanced-ai";
        public const string BetaFeatures = "beta-features";
        public const string SocialSharing = "social-sharing";
        public const string VoiceFeatures = "voice-features";
        public const string ARIntegration = "ar-integration";
        public const string AdultMode = "adult-mode";
    }

    /// <summary>
    /// Analytics und Metriken
    /// </summary>
    public static class Analytics
    {
        public const string StoryViewEvent = "story_view";
        public const string CharacterCreatedEvent = "character_created";
        public const string UserRegisteredEvent = "user_registered";
        public const string StoryGeneratedEvent = "story_generated";
        public const string CharacterSharedEvent = "character_shared";
        public const string SubscriptionUpgradeEvent = "subscription_upgrade";
    }
}