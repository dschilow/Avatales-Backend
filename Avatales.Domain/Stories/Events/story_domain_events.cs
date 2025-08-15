using Avatales.Shared.Models;

namespace Avatales.Domain.Stories.Events;

/// <summary>
/// Event: Neue Story-Generierung wurde gestartet
/// </summary>
public class StoryGenerationStartedEvent : DomainEvent
{
    public Guid AuthorUserId { get; }
    public Guid MainCharacterId { get; }
    public string UserPrompt { get; }
    public StoryGenre Genre { get; }
    public List<string> RequestedLearningGoals { get; }
    public DateTime GenerationStartedTimestamp { get; }

    public StoryGenerationStartedEvent(
        Guid storyId,
        Guid authorUserId,
        Guid mainCharacterId,
        string userPrompt,
        StoryGenre genre,
        List<string>? requestedLearningGoals = null) 
        : base(storyId)
    {
        AuthorUserId = authorUserId;
        MainCharacterId = mainCharacterId;
        UserPrompt = userPrompt;
        Genre = genre;
        RequestedLearningGoals = requestedLearningGoals ?? new List<string>();
        GenerationStartedTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "StoryGenerationStarted";
}

/// <summary>
/// Event: Story-Generierung wurde abgeschlossen
/// </summary>
public class StoryGenerationCompletedEvent : DomainEvent
{
    public Guid AuthorUserId { get; }
    public Guid MainCharacterId { get; }
    public string StoryTitle { get; }
    public int WordCount { get; }
    public int SceneCount { get; }
    public int ReadingTimeMinutes { get; }
    public List<string> LearningGoalsAchieved { get; }
    public TimeSpan GenerationDuration { get; }
    public DateTime CompletedTimestamp { get; }

    public StoryGenerationCompletedEvent(
        Guid storyId,
        Guid authorUserId,
        Guid mainCharacterId,
        string storyTitle,
        int wordCount,
        int sceneCount,
        int readingTimeMinutes,
        List<string> learningGoalsAchieved,
        TimeSpan generationDuration) 
        : base(storyId)
    {
        AuthorUserId = authorUserId;
        MainCharacterId = mainCharacterId;
        StoryTitle = storyTitle;
        WordCount = wordCount;
        SceneCount = sceneCount;
        ReadingTimeMinutes = readingTimeMinutes;
        LearningGoalsAchieved = learningGoalsAchieved;
        GenerationDuration = generationDuration;
        CompletedTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "StoryGenerationCompleted";
}

/// <summary>
/// Event: Story-Generierung ist fehlgeschlagen
/// </summary>
public class StoryGenerationFailedEvent : DomainEvent
{
    public Guid AuthorUserId { get; }
    public Guid MainCharacterId { get; }
    public string FailureReason { get; }
    public string? ErrorDetails { get; }
    public TimeSpan AttemptDuration { get; }
    public DateTime FailedTimestamp { get; }

    public StoryGenerationFailedEvent(
        Guid storyId,
        Guid authorUserId,
        Guid mainCharacterId,
        string failureReason,
        string? errorDetails = null,
        TimeSpan attemptDuration = default) 
        : base(storyId)
    {
        AuthorUserId = authorUserId;
        MainCharacterId = mainCharacterId;
        FailureReason = failureReason;
        ErrorDetails = errorDetails;
        AttemptDuration = attemptDuration;
        FailedTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "StoryGenerationFailed";
}

/// <summary>
/// Event: Story wurde von einem Benutzer gelesen
/// </summary>
public class StoryReadByUserEvent : DomainEvent
{
    public Guid ReaderId { get; }
    public Guid CharacterUsedId { get; }
    public TimeSpan ReadingDuration { get; }
    public bool CompletedReading { get; }
    public int ScenesRead { get; }
    public List<string> InteractionsPerformed { get; }
    public DateTime ReadTimestamp { get; }

    public StoryReadByUserEvent(
        Guid storyId,
        Guid readerId,
        Guid characterUsedId,
        TimeSpan readingDuration,
        bool completedReading,
        int scenesRead,
        List<string>? interactionsPerformed = null) 
        : base(storyId)
    {
        ReaderId = readerId;
        CharacterUsedId = characterUsedId;
        ReadingDuration = readingDuration;
        CompletedReading = completedReading;
        ScenesRead = scenesRead;
        InteractionsPerformed = interactionsPerformed ?? new List<string>();
        ReadTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "StoryReadByUser";
}

/// <summary>
/// Event: Story wurde bewertet
/// </summary>
public class StoryRatedEvent : DomainEvent
{
    public Guid RatedByUserId { get; }
    public int Rating { get; } // 1-5 Sterne
    public string? ReviewComment { get; }
    public List<string> PositiveAspects { get; }
    public List<string> NegativeAspects { get; }
    public DateTime RatedTimestamp { get; }

    public StoryRatedEvent(
        Guid storyId,
        Guid ratedByUserId,
        int rating,
        string? reviewComment = null,
        List<string>? positiveAspects = null,
        List<string>? negativeAspects = null) 
        : base(storyId)
    {
        RatedByUserId = ratedByUserId;
        Rating = rating;
        ReviewComment = reviewComment;
        PositiveAspects = positiveAspects ?? new List<string>();
        NegativeAspects = negativeAspects ?? new List<string>();
        RatedTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "StoryRated";
}

/// <summary>
/// Event: Story wurde geteilt
/// </summary>
public class StorySharedEvent : DomainEvent
{
    public Guid SharedByUserId { get; }
    public string ShareMethod { get; } // "Community", "Direct", "Social Media"
    public List<Guid> SharedWithUserIds { get; }
    public string? ShareMessage { get; }
    public DateTime SharedTimestamp { get; }

    public StorySharedEvent(
        Guid storyId,
        Guid sharedByUserId,
        string shareMethod,
        List<Guid>? sharedWithUserIds = null,
        string? shareMessage = null) 
        : base(storyId)
    {
        SharedByUserId = sharedByUserId;
        ShareMethod = shareMethod;
        SharedWithUserIds = sharedWithUserIds ?? new List<Guid>();
        ShareMessage = shareMessage;
        SharedTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "StoryShared";
}

/// <summary>
/// Event: Story wurde zur Community hinzugefügt
/// </summary>
public class StoryAddedToCommunityEvent : DomainEvent
{
    public Guid AuthorUserId { get; }
    public string StoryTitle { get; }
    public StoryGenre Genre { get; }
    public int RecommendedAge { get; }
    public List<string> Tags { get; }
    public bool IsPublic { get; }
    public DateTime AddedTimestamp { get; }

    public StoryAddedToCommunityEvent(
        Guid storyId,
        Guid authorUserId,
        string storyTitle,
        StoryGenre genre,
        int recommendedAge,
        List<string> tags,
        bool isPublic) 
        : base(storyId)
    {
        AuthorUserId = authorUserId;
        StoryTitle = storyTitle;
        Genre = genre;
        RecommendedAge = recommendedAge;
        Tags = tags;
        IsPublic = isPublic;
        AddedTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "StoryAddedToCommunity";
}

/// <summary>
/// Event: Story Content wurde moderiert
/// </summary>
public class StoryContentModeratedEvent : DomainEvent
{
    public Guid ModeratedByUserId { get; }
    public ContentModerationStatus OldStatus { get; }
    public ContentModerationStatus NewStatus { get; }
    public string ModerationReason { get; }
    public List<string> FlagsRaised { get; }
    public bool RequiresAuthorAction { get; }
    public DateTime ModeratedTimestamp { get; }

    public StoryContentModeratedEvent(
        Guid storyId,
        Guid moderatedByUserId,
        ContentModerationStatus oldStatus,
        ContentModerationStatus newStatus,
        string moderationReason,
        List<string>? flagsRaised = null,
        bool requiresAuthorAction = false) 
        : base(storyId)
    {
        ModeratedByUserId = moderatedByUserId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        ModerationReason = moderationReason;
        FlagsRaised = flagsRaised ?? new List<string>();
        RequiresAuthorAction = requiresAuthorAction;
        ModeratedTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "StoryContentModerated";
}

/// <summary>
/// Event: Lernziel in Story wurde erreicht
/// </summary>
public class StoryLearningGoalAchievedEvent : DomainEvent
{
    public Guid LearningGoalId { get; }
    public Guid AchievedByUserId { get; }
    public Guid CharacterUsedId { get; }
    public string LearningGoalTitle { get; }
    public LearningGoalCategory Category { get; }
    public string EvidenceOfLearning { get; }
    public int AttemptsNeeded { get; }
    public DateTime AchievedTimestamp { get; }

    public StoryLearningGoalAchievedEvent(
        Guid storyId,
        Guid learningGoalId,
        Guid achievedByUserId,
        Guid characterUsedId,
        string learningGoalTitle,
        LearningGoalCategory category,
        string evidenceOfLearning,
        int attemptsNeeded) 
        : base(storyId)
    {
        LearningGoalId = learningGoalId;
        AchievedByUserId = achievedByUserId;
        CharacterUsedId = characterUsedId;
        LearningGoalTitle = learningGoalTitle;
        Category = category;
        EvidenceOfLearning = evidenceOfLearning;
        AttemptsNeeded = attemptsNeeded;
        AchievedTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "StoryLearningGoalAchieved";
}

/// <summary>
/// Event: Story Images wurden generiert
/// </summary>
public class StoryImagesGeneratedEvent : DomainEvent
{
    public List<string> ImageUrls { get; }
    public List<string> ImagePrompts { get; }
    public string GenerationMethod { get; } // "AI", "Selected", "Uploaded"
    public TimeSpan GenerationDuration { get; }
    public int TotalImagesGenerated { get; }
    public DateTime ImagesGeneratedTimestamp { get; }

    public StoryImagesGeneratedEvent(
        Guid storyId,
        List<string> imageUrls,
        List<string> imagePrompts,
        string generationMethod,
        TimeSpan generationDuration,
        int totalImagesGenerated) 
        : base(storyId)
    {
        ImageUrls = imageUrls;
        ImagePrompts = imagePrompts;
        GenerationMethod = generationMethod;
        GenerationDuration = generationDuration;
        TotalImagesGenerated = totalImagesGenerated;
        ImagesGeneratedTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "StoryImagesGenerated";
}

/// <summary>
/// Event: Story wurde archiviert
/// </summary>
public class StoryArchivedEvent : DomainEvent
{
    public Guid ArchivedByUserId { get; }
    public string ArchiveReason { get; }
    public bool IsPubliclyVisible { get; }
    public DateTime ArchivedTimestamp { get; }

    public StoryArchivedEvent(
        Guid storyId,
        Guid archivedByUserId,
        string archiveReason,
        bool isPubliclyVisible) 
        : base(storyId)
    {
        ArchivedByUserId = archivedByUserId;
        ArchiveReason = archiveReason;
        IsPubliclyVisible = isPubliclyVisible;
        ArchivedTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "StoryArchived";
}

/// <summary>
/// Event: Story wurde aus dem Archiv wiederhergestellt
/// </summary>
public class StoryRestoredEvent : DomainEvent
{
    public Guid RestoredByUserId { get; }
    public string RestoreReason { get; }
    public DateTime RestoredTimestamp { get; }

    public StoryRestoredEvent(
        Guid storyId,
        Guid restoredByUserId,
        string restoreReason) 
        : base(storyId)
    {
        RestoredByUserId = restoredByUserId;
        RestoreReason = restoreReason;
        RestoredTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "StoryRestored";
}

/// <summary>
/// Event: Story wurde als Feature/Highlight ausgewählt
/// </summary>
public class StoryFeaturedEvent : DomainEvent
{
    public Guid FeaturedByUserId { get; }
    public string FeatureReason { get; }
    public DateTime FeaturedUntil { get; }
    public List<string> FeatureCategories { get; }
    public DateTime FeaturedTimestamp { get; }

    public StoryFeaturedEvent(
        Guid storyId,
        Guid featuredByUserId,
        string featureReason,
        DateTime featuredUntil,
        List<string>? featureCategories = null) 
        : base(storyId)
    {
        FeaturedByUserId = featuredByUserId;
        FeatureReason = featureReason;
        FeaturedUntil = featuredUntil;
        FeatureCategories = featureCategories ?? new List<string>();
        FeaturedTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "StoryFeatured";
}

/// <summary>
/// Event: Story Performance-Statistiken wurden aktualisiert
/// </summary>
public class StoryStatisticsUpdatedEvent : DomainEvent
{
    public int TotalReads { get; }
    public int UniqueReaders { get; }
    public double AverageRating { get; }
    public int ShareCount { get; }
    public TimeSpan AverageReadingTime { get; }
    public List<string> PopularWithAgeGroups { get; }
    public DateTime StatisticsUpdatedTimestamp { get; }

    public StoryStatisticsUpdatedEvent(
        Guid storyId,
        int totalReads,
        int uniqueReaders,
        double averageRating,
        int shareCount,
        TimeSpan averageReadingTime,
        List<string> popularWithAgeGroups) 
        : base(storyId)
    {
        TotalReads = totalReads;
        UniqueReaders = uniqueReaders;
        AverageRating = averageRating;
        ShareCount = shareCount;
        AverageReadingTime = averageReadingTime;
        PopularWithAgeGroups = popularWithAgeGroups;
        StatisticsUpdatedTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "StoryStatisticsUpdated";
}