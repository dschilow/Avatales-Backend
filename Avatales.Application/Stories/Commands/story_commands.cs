using System.ComponentModel.DataAnnotations;
using Avatales.Application.Common.Interfaces;
using Avatales.Application.Stories.DTOs;
using Avatales.Shared.Models;

namespace Avatales.Application.Stories.Commands;

/// <summary>
/// Command: Neue Story generieren
/// Kernfunktion der Avatales-App - AI-gestützte Story-Generierung
/// </summary>
public class GenerateStoryCommand : ICommand<GenerateStoryResponseDto>
{
    [Required]
    public Guid MainCharacterId { get; set; }

    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string UserPrompt { get; set; } = string.Empty;

    [Required]
    public StoryGenre Genre { get; set; }

    public List<string>? RequestedLearningGoals { get; set; }

    [Range(50, 2000)]
    public int? TargetWordCount { get; set; }

    [Range(1, 15)]
    public int? TargetSceneCount { get; set; }

    public bool IncludeImages { get; set; } = true;
    public bool EnableInteractiveElements { get; set; } = false;
    public bool EnableLearningMode { get; set; } = true;

    [StringLength(100)]
    public string? EmotionalTone { get; set; } // "happy", "exciting", "mysterious", "calm"

    public List<string>? AvoidTopics { get; set; }

    [Range(1, 5)]
    public int DifficultyLevel { get; set; } = 3; // 1=sehr einfach, 5=herausfordernd

    public bool PrefersHappyEnding { get; set; } = true;

    [Range(1, 10)]
    public int CreativityLevel { get; set; } = 5; // Wie kreativ/unvorhersagbar soll die Story sein

    public Dictionary<string, object>? AdditionalContext { get; set; }

    // Generation Settings
    public bool UseCharacterMemories { get; set; } = true;
    public bool AdaptToCharacterTraits { get; set; } = true;
    public bool IncludeCharacterGrowth { get; set; } = true;

    public GenerateStoryCommand(Guid mainCharacterId, string userPrompt, StoryGenre genre)
    {
        MainCharacterId = mainCharacterId;
        UserPrompt = userPrompt;
        Genre = genre;
    }
}

/// <summary>
/// Command: Story-Generierung abbrechen
/// </summary>
public class CancelStoryGenerationCommand : ICommand
{
    [Required]
    public Guid StoryId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [StringLength(200)]
    public string? CancellationReason { get; set; }

    public CancelStoryGenerationCommand(Guid storyId, Guid userId)
    {
        StoryId = storyId;
        UserId = userId;
    }
}

/// <summary>
/// Command: Story aktualisieren/bearbeiten
/// </summary>
public class UpdateStoryCommand : ICommand<StoryDetailDto>
{
    [Required]
    public Guid StoryId { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string Summary { get; set; } = string.Empty;

    public List<string>? Tags { get; set; }
    public bool IsPublic { get; set; }
    public List<LearningGoalDto>? AdditionalLearningGoals { get; set; }

    [Range(3, 18)]
    public int? RecommendedAge { get; set; }

    public UpdateStoryCommand(Guid storyId, string title, string summary)
    {
        StoryId = storyId;
        Title = title;
        Summary = summary;
    }
}

/// <summary>
/// Command: Story-Szene aktualisieren
/// </summary>
public class UpdateStorySceneCommand : ICommand<StorySceneDto>
{
    [Required]
    public Guid StoryId { get; set; }

    [Required]
    public Guid SceneId { get; set; }

    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(2000, MinimumLength = 10)]
    public string Content { get; set; } = string.Empty;

    [StringLength(200)]
    public string? ImagePrompt { get; set; }

    public List<SceneChoiceDto>? Choices { get; set; }
    public Dictionary<string, object>? InteractiveElements { get; set; }

    public UpdateStorySceneCommand(Guid storyId, Guid sceneId, string title, string content)
    {
        StoryId = storyId;
        SceneId = sceneId;
        Title = title;
        Content = content;
    }
}

/// <summary>
/// Command: Story bewerten
/// </summary>
public class RateStoryCommand : ICommand
{
    [Required]
    public Guid StoryId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }

    [StringLength(500)]
    public string? ReviewComment { get; set; }

    public List<string>? PositiveAspects { get; set; }
    public List<string>? NegativeAspects { get; set; }
    public bool RecommendToOthers { get; set; } = true;

    public RateStoryCommand(Guid storyId, Guid userId, int rating)
    {
        StoryId = storyId;
        UserId = userId;
        Rating = rating;
    }
}

/// <summary>
/// Command: Story teilen
/// </summary>
public class ShareStoryCommand : ICommand
{
    [Required]
    public Guid StoryId { get; set; }

    [Required]
    public Guid SharedByUserId { get; set; }

    [Required]
    public string ShareMethod { get; set; } = string.Empty; // "Community", "Direct", "Social", "Export"

    public List<Guid>? ShareWithUserIds { get; set; }

    [StringLength(200)]
    public string? ShareMessage { get; set; }

    public List<string>? ShareCategories { get; set; }
    public bool MakePublic { get; set; } = false;

    // Export-spezifische Parameter
    public string? ExportFormat { get; set; } // "pdf", "epub", "html"
    public bool IncludeImages { get; set; } = true;

    public ShareStoryCommand(Guid storyId, Guid sharedByUserId, string shareMethod)
    {
        StoryId = storyId;
        SharedByUserId = sharedByUserId;
        ShareMethod = shareMethod;
    }
}

/// <summary>
/// Command: Story lesen/Progress tracken
/// </summary>
public class ReadStoryCommand : ICommand<StoryReadingProgressDto>
{
    [Required]
    public Guid StoryId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid CharacterUsedId { get; set; }

    [Range(1, 100)]
    public int CurrentScene { get; set; } = 1;

    public TimeSpan SessionDuration { get; set; }
    public bool CompletedReading { get; set; } = false;
    public List<string>? InteractionsPerformed { get; set; }
    public Dictionary<string, object>? ReadingContext { get; set; }

    public ReadStoryCommand(Guid storyId, Guid userId, Guid characterUsedId)
    {
        StoryId = storyId;
        UserId = userId;
        CharacterUsedId = characterUsedId;
    }
}

/// <summary>
/// Command: Story-Images generieren
/// </summary>
public class GenerateStoryImagesCommand : ICommand<List<string>>
{
    [Required]
    public Guid StoryId { get; set; }

    public List<string>? SpecificSceneIds { get; set; } // Nur für bestimmte Szenen generieren

    public string ImageStyle { get; set; } = "cartoon"; // "cartoon", "realistic", "watercolor", "sketch"
    public string ColorPalette { get; set; } = "bright"; // "bright", "pastel", "vibrant", "muted"

    [Range(1, 4)]
    public int ImagesPerScene { get; set; } = 1;

    public bool RegenerateExisting { get; set; } = false;
    public Dictionary<string, object>? StyleParameters { get; set; }

    public GenerateStoryImagesCommand(Guid storyId)
    {
        StoryId = storyId;
    }
}

/// <summary>
/// Command: Story zur Community hinzufügen
/// </summary>
public class AddStoryToCommunityCommand : ICommand
{
    [Required]
    public Guid StoryId { get; set; }

    [Required]
    public Guid AuthorUserId { get; set; }

    public List<string>? CommunityTags { get; set; }
    public List<string>? TargetAgeGroups { get; set; }

    [StringLength(300)]
    public string? CommunityDescription { get; set; }

    public bool AllowRemixing { get; set; } = false;
    public bool AllowRating { get; set; } = true;
    public bool AllowComments { get; set; } = true;

    public AddStoryToCommunityCommand(Guid storyId, Guid authorUserId)
    {
        StoryId = storyId;
        AuthorUserId = authorUserId;
    }
}

/// <summary>
/// Command: Story aus Community entfernen
/// </summary>
public class RemoveStoryFromCommunityCommand : ICommand
{
    [Required]
    public Guid StoryId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [StringLength(200)]
    public string RemovalReason { get; set; } = string.Empty;

    public bool NotifyAuthor { get; set; } = true;

    public RemoveStoryFromCommunityCommand(Guid storyId, Guid userId, string removalReason)
    {
        StoryId = storyId;
        UserId = userId;
        RemovalReason = removalReason;
    }
}

/// <summary>
/// Command: Story-Content moderieren
/// </summary>
public class ModerateStoryContentCommand : ICommand
{
    [Required]
    public Guid StoryId { get; set; }

    [Required]
    public Guid ModeratedByUserId { get; set; }

    [Required]
    public ContentModerationStatus NewStatus { get; set; }

    [Required]
    [StringLength(500)]
    public string ModerationReason { get; set; } = string.Empty;

    public List<string>? FlagsRaised { get; set; }
    public bool RequiresAuthorAction { get; set; } = false;
    public bool NotifyAuthor { get; set; } = true;

    [StringLength(500)]
    public string? FeedbackForAuthor { get; set; }

    public ModerateStoryContentCommand(Guid storyId, Guid moderatedByUserId, ContentModerationStatus newStatus, string moderationReason)
    {
        StoryId = storyId;
        ModeratedByUserId = moderatedByUserId;
        NewStatus = newStatus;
        ModerationReason = moderationReason;
    }
}

/// <summary>
/// Command: Story archivieren
/// </summary>
public class ArchiveStoryCommand : ICommand
{
    [Required]
    public Guid StoryId { get; set; }

    [Required]
    public Guid ArchivedByUserId { get; set; }

    [Required]
    [StringLength(200)]
    public string ArchiveReason { get; set; } = string.Empty;

    public bool IsPubliclyVisible { get; set; } = false;
    public bool PreserveForAnalytics { get; set; } = true;

    public ArchiveStoryCommand(Guid storyId, Guid archivedByUserId, string archiveReason)
    {
        StoryId = storyId;
        ArchivedByUserId = archivedByUserId;
        ArchiveReason = archiveReason;
    }
}

/// <summary>
/// Command: Story wiederherstellen
/// </summary>
public class RestoreStoryCommand : ICommand
{
    [Required]
    public Guid StoryId { get; set; }

    [Required]
    public Guid RestoredByUserId { get; set; }

    [StringLength(200)]
    public string? RestoreReason { get; set; }

    public bool MakePublicAgain { get; set; } = false;

    public RestoreStoryCommand(Guid storyId, Guid restoredByUserId)
    {
        StoryId = storyId;
        RestoredByUserId = restoredByUserId;
    }
}

/// <summary>
/// Command: Story als Featured markieren
/// </summary>
public class FeatureStoryCommand : ICommand
{
    [Required]
    public Guid StoryId { get; set; }

    [Required]
    public Guid FeaturedByUserId { get; set; }

    [Required]
    [StringLength(200)]
    public string FeatureReason { get; set; } = string.Empty;

    [Required]
    public DateTime FeaturedUntil { get; set; }

    public List<string>? FeatureCategories { get; set; }
    public int DisplayPriority { get; set; } = 1;

    public FeatureStoryCommand(Guid storyId, Guid featuredByUserId, string featureReason, DateTime featuredUntil)
    {
        StoryId = storyId;
        FeaturedByUserId = featuredByUserId;
        FeatureReason = featureReason;
        FeaturedUntil = featuredUntil;
    }
}

/// <summary>
/// Command: Story-Lernziel-Fortschritt aktualisieren
/// </summary>
public class UpdateLearningGoalProgressCommand : ICommand
{
    [Required]
    public Guid StoryId { get; set; }

    [Required]
    public Guid LearningGoalId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Range(0, 100)]
    public float ProgressPercentage { get; set; }

    public LearningGoalStatus Status { get; set; }

    [StringLength(300)]
    public string? EvidenceOfLearning { get; set; }

    public Dictionary<string, object>? ProgressMetrics { get; set; }

    public UpdateLearningGoalProgressCommand(Guid storyId, Guid learningGoalId, Guid userId, float progressPercentage)
    {
        StoryId = storyId;
        LearningGoalId = learningGoalId;
        UserId = userId;
        ProgressPercentage = progressPercentage;
    }
}

/// <summary>
/// Command: Story-Statistiken aktualisieren
/// </summary>
public class UpdateStoryStatisticsCommand : ICommand
{
    [Required]
    public Guid StoryId { get; set; }

    public int? ViewIncrement { get; set; }
    public int? ShareIncrement { get; set; }
    public TimeSpan? AdditionalReadingTime { get; set; }
    public Dictionary<string, int>? AgeGroupEngagement { get; set; }
    public Dictionary<string, object>? CustomMetrics { get; set; }

    public UpdateStoryStatisticsCommand(Guid storyId)
    {
        StoryId = storyId;
    }
}

/// <summary>
/// Command: Story löschen
/// </summary>
public class DeleteStoryCommand : ICommand
{
    [Required]
    public Guid StoryId { get; set; }

    [Required]
    public Guid DeletedByUserId { get; set; }

    [Required]
    [StringLength(200)]
    public string DeletionReason { get; set; } = string.Empty;

    public bool HardDelete { get; set; } = false; // Soft delete by default
    public bool DeleteImages { get; set; } = true;
    public bool NotifyAuthor { get; set; } = true;
    public int RetentionDays { get; set; } = 30; // Für Soft Delete

    public DeleteStoryCommand(Guid storyId, Guid deletedByUserId, string deletionReason)
    {
        StoryId = storyId;
        DeletedByUserId = deletedByUserId;
        DeletionReason = deletionReason;
    }
}

/// <summary>
/// Command: Story exportieren
/// </summary>
public class ExportStoryCommand : ICommand<string>
{
    [Required]
    public Guid StoryId { get; set; }

    [Required]
    public Guid RequestedByUserId { get; set; }

    [Required]
    public string Format { get; set; } = "pdf"; // "pdf", "epub", "html", "docx", "json"

    public bool IncludeImages { get; set; } = true;
    public bool IncludeCharacterProfile { get; set; } = false;
    public bool IncludeLearningGoals { get; set; } = false;
    public string? CustomCoverImage { get; set; }
    public Dictionary<string, object>? FormatOptions { get; set; }

    // PDF-spezifische Optionen
    public string? PdfTemplate { get; set; } // "children_book", "simple", "illustrated"
    public bool IncludeTableOfContents { get; set; } = true;

    public ExportStoryCommand(Guid storyId, Guid requestedByUserId, string format = "pdf")
    {
        StoryId = storyId;
        RequestedByUserId = requestedByUserId;
        Format = format;
    }
}

/// <summary>
/// Command: Batch-Story-Operationen
/// </summary>
public class BatchStoryOperationCommand : ICommand<List<StoryDto>>
{
    [Required]
    [MinLength(1)]
    public List<Guid> StoryIds { get; set; } = new();

    [Required]
    public string Operation { get; set; } = string.Empty; // "archive", "delete", "share", "export", "moderate"

    [Required]
    public Guid PerformedByUserId { get; set; }

    public Dictionary<string, object>? OperationParameters { get; set; }

    [StringLength(200)]
    public string? Reason { get; set; }

    public BatchStoryOperationCommand(List<Guid> storyIds, string operation, Guid performedByUserId)
    {
        StoryIds = storyIds;
        Operation = operation;
        PerformedByUserId = performedByUserId;
    }
}

/// <summary>
/// Command: Story-Template erstellen
/// </summary>
public class CreateStoryTemplateCommand : ICommand<StoryTemplateDto>
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string Category { get; set; } = string.Empty;

    [Required]
    public Guid CreatedByUserId { get; set; }

    public List<StoryGenre> SupportedGenres { get; set; } = new();

    [Range(3, 18)]
    public int RecommendedAge { get; set; } = 7;

    [Required]
    public string Structure { get; set; } = string.Empty; // JSON-Template

    public List<string> RequiredElements { get; set; } = new();
    public List<LearningGoalDto> DefaultLearningGoals { get; set; } = new();
    public Dictionary<string, object> TemplateParameters { get; set; } = new();
    public bool IsPublic { get; set; } = false;

    public CreateStoryTemplateCommand(string name, string description, string category, Guid createdByUserId, string structure)
    {
        Name = name;
        Description = description;
        Category = category;
        CreatedByUserId = createdByUserId;
        Structure = structure;
    }
}

/// <summary>
/// Command: Story aus Template generieren
/// </summary>
public class GenerateStoryFromTemplateCommand : ICommand<GenerateStoryResponseDto>
{
    [Required]
    public Guid TemplateId { get; set; }

    [Required]
    public Guid MainCharacterId { get; set; }

    public Dictionary<string, object> TemplateVariables { get; set; } = new();

    [StringLength(500)]
    public string? AdditionalPrompt { get; set; }

    public bool CustomizeForCharacter { get; set; } = true;

    public GenerateStoryFromTemplateCommand(Guid templateId, Guid mainCharacterId)
    {
        TemplateId = templateId;
        MainCharacterId = mainCharacterId;
    }
}

/// <summary>
/// Command: Story-Remix erstellen (basierend auf existierender Story)
/// </summary>
public class RemixStoryCommand : ICommand<GenerateStoryResponseDto>
{
    [Required]
    public Guid OriginalStoryId { get; set; }

    [Required]
    public Guid MainCharacterId { get; set; }

    [Required]
    [StringLength(300)]
    public string RemixPrompt { get; set; } = string.Empty;

    public List<string>? ElementsToKeep { get; set; } // "plot", "characters", "setting", "themes"
    public List<string>? ElementsToChange { get; set; }

    [Range(1, 5)]
    public int RemixIntensity { get; set; } = 3; // Wie stark soll geändert werden

    public bool CreditOriginalAuthor { get; set; } = true;

    public RemixStoryCommand(Guid originalStoryId, Guid mainCharacterId, string remixPrompt)
    {
        OriginalStoryId = originalStoryId;
        MainCharacterId = mainCharacterId;
        RemixPrompt = remixPrompt;
    }
}