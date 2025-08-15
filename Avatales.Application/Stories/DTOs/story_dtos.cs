using Avatales.Shared.Models;

namespace Avatales.Application.Stories.DTOs;

/// <summary>
/// Basis-DTO für Story-Informationen
/// </summary>
public class StoryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public Guid MainCharacterId { get; set; }
    public string MainCharacterName { get; set; } = string.Empty;
    public Guid AuthorUserId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public string UserPrompt { get; set; } = string.Empty;
    public string GenerationStatus { get; set; } = string.Empty;
    public string ModerationStatus { get; set; } = string.Empty;
    public int ReadingTimeMinutes { get; set; }
    public int WordCount { get; set; }
    public int RecommendedAge { get; set; }
    public bool IsPublic { get; set; }
    public bool HasImages { get; set; }
    public bool HasLearningMode { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? LastReadAt { get; set; }
}

/// <summary>
/// Detaillierte Story-Informationen mit allen Daten
/// </summary>
public class StoryDetailDto : StoryDto
{
    public string Content { get; set; } = string.Empty;
    public List<StorySceneDto> Scenes { get; set; } = new();
    public List<LearningGoalDto> LearningGoals { get; set; } = new();
    public List<string> ImageUrls { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public StoryStatisticsDto Statistics { get; set; } = new();
    public List<StoryInteractionDto> Interactions { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public bool CanEdit { get; set; }
    public bool CanShare { get; set; }
    public bool CanDelete { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO für Story-Szenen
/// </summary>
public class StorySceneDto
{
    public Guid Id { get; set; }
    public int SceneNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string ImagePrompt { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int WordCount { get; set; }
    public int EstimatedReadingTimeSeconds { get; set; }
    public string PrimaryEmotion { get; set; } = string.Empty;
    public string EmotionalTone { get; set; } = string.Empty;
    public List<string> KeyWords { get; set; } = new();
    public List<string> LearningMoments { get; set; } = new();
    public string DifficultyLevel { get; set; } = string.Empty;
    public List<SceneChoiceDto> Choices { get; set; } = new();
    public Dictionary<string, object> InteractiveElements { get; set; } = new();
    public bool HasInteractiveElement { get; set; }
    public Dictionary<string, float> TraitInfluences { get; set; } = new();
    public List<string> CharacterActions { get; set; } = new();
    public List<string> CharacterLearnings { get; set; } = new();
    public bool IsClimax { get; set; }
    public bool RequiresParentalGuidance { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO für Szenen-Wahlmöglichkeiten
/// </summary>
public class SceneChoiceDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, float> TraitInfluences { get; set; } = new();
    public string? NextSceneId { get; set; }
    public bool IsOptimal { get; set; }
}

/// <summary>
/// DTO für Lernziele
/// </summary>
public class LearningGoalDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public int TargetAge { get; set; }
    public int Priority { get; set; }
    public string Status { get; set; } = string.Empty;
    public float ProgressPercentage { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int AttemptsCount { get; set; }
    public List<string> SuccessCriteria { get; set; } = new();
    public Dictionary<string, object> ProgressMetrics { get; set; } = new();
    public List<string> EvidenceOfLearning { get; set; } = new();
    public List<string> RelatedTraits { get; set; } = new();
    public List<string> KeyConcepts { get; set; } = new();
    public List<string> VocabularyWords { get; set; } = new();
    public bool RequiresReflection { get; set; }
    public bool RequiresDiscussion { get; set; }
}

/// <summary>
/// DTO für Story-Statistiken
/// </summary>
public class StoryStatisticsDto
{
    public int TotalReads { get; set; }
    public int UniqueReaders { get; set; }
    public double AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public int ShareCount { get; set; }
    public TimeSpan AverageReadingTime { get; set; }
    public Dictionary<string, int> PopularWithAgeGroups { get; set; } = new();
    public Dictionary<string, int> GenrePopularity { get; set; } = new();
    public int LearningGoalsCompleted { get; set; }
    public int CharacterEvolutionsTriggered { get; set; }
    public DateTime LastReadAt { get; set; }
    public DateTime StatisticsUpdatedAt { get; set; }
}

/// <summary>
/// DTO für Story-Interaktionen
/// </summary>
public class StoryInteractionDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string InteractionType { get; set; } = string.Empty; // "read", "rating", "comment", "share"
    public string? InteractionData { get; set; }
    public DateTime InteractionTimestamp { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
}

/// <summary>
/// DTO für Story-Generierung-Anfrage
/// </summary>
public class GenerateStoryRequestDto
{
    public Guid MainCharacterId { get; set; }
    public string UserPrompt { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public List<string>? RequestedLearningGoals { get; set; }
    public int? TargetLength { get; set; } // In Wörtern
    public bool IncludeImages { get; set; } = true;
    public bool EnableInteractiveElements { get; set; } = false;
    public string? EmotionalTone { get; set; }
    public List<string>? AvoidTopics { get; set; }
    public int? DifficultyLevel { get; set; } // 1-5
    public bool PrefersHappyEnding { get; set; } = true;
    public Dictionary<string, object>? AdditionalContext { get; set; }
}

/// <summary>
/// DTO für Story-Generierung-Antwort
/// </summary>
public class GenerateStoryResponseDto
{
    public bool IsSuccess { get; set; }
    public Guid? StoryId { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan GenerationDuration { get; set; }
    public StoryGenerationProgressDto? Progress { get; set; }
    public List<string> WarningsOrSuggestions { get; set; } = new();
}

/// <summary>
/// DTO für Story-Generierung-Fortschritt
/// </summary>
public class StoryGenerationProgressDto
{
    public int ProgressPercentage { get; set; }
    public string CurrentStage { get; set; } = string.Empty;
    public string StageDescription { get; set; } = string.Empty;
    public TimeSpan EstimatedTimeRemaining { get; set; }
    public List<string> CompletedStages { get; set; } = new();
    public bool IsComplete { get; set; }
}

/// <summary>
/// DTO für Story-Update
/// </summary>
public class UpdateStoryDto
{
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<string>? Tags { get; set; }
    public bool IsPublic { get; set; }
    public List<LearningGoalDto>? AdditionalLearningGoals { get; set; }
}

/// <summary>
/// DTO für Story-Bewertung
/// </summary>
public class RateStoryDto
{
    public int Rating { get; set; } // 1-5 Sterne
    public string? ReviewComment { get; set; }
    public List<string>? PositiveAspects { get; set; }
    public List<string>? NegativeAspects { get; set; }
    public bool RecommendToOthers { get; set; } = true;
}

/// <summary>
/// DTO für Story-Sharing
/// </summary>
public class ShareStoryDto
{
    public string ShareMethod { get; set; } = string.Empty; // "Community", "Direct", "Social"
    public List<Guid>? ShareWithUserIds { get; set; }
    public string? ShareMessage { get; set; }
    public List<string>? ShareCategories { get; set; }
    public bool MakePublic { get; set; } = false;
}

/// <summary>
/// DTO für Story-Suche
/// </summary>
public class StorySearchDto
{
    public string? TitleQuery { get; set; }
    public string? ContentQuery { get; set; }
    public List<string>? Genres { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public int? MinReadingTime { get; set; }
    public int? MaxReadingTime { get; set; }
    public bool? HasImages { get; set; }
    public bool? HasLearningMode { get; set; }
    public List<string>? Tags { get; set; }
    public List<string>? LearningGoalCategories { get; set; }
    public Guid? AuthorUserId { get; set; }
    public string? SortBy { get; set; } // "popularity", "recent", "rating", "title"
    public bool SortDescending { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// DTO für Story-Empfehlungen
/// </summary>
public class StoryRecommendationDto
{
    public StoryDto Story { get; set; } = new();
    public float RecommendationScore { get; set; }
    public List<string> RecommendationReasons { get; set; } = new();
    public List<string> MatchingLearningGoals { get; set; } = new();
    public List<string> CharacterCompatibility { get; set; } = new();
    public string RecommendationType { get; set; } = string.Empty; // "character_match", "learning_goals", "similar_stories", "popular"
    public bool IsFromFollowedAuthor { get; set; }
}

/// <summary>
/// DTO für Story-Lesefortschritt
/// </summary>
public class StoryReadingProgressDto
{
    public Guid StoryId { get; set; }
    public Guid UserId { get; set; }
    public Guid CharacterUsedId { get; set; }
    public int CurrentScene { get; set; }
    public int TotalScenes { get; set; }
    public float ProgressPercentage { get; set; }
    public TimeSpan ReadingTimeElapsed { get; set; }
    public TimeSpan EstimatedTimeRemaining { get; set; }
    public List<string> CompletedInteractions { get; set; } = new();
    public List<LearningGoalProgressDto> LearningGoalProgress { get; set; } = new();
    public DateTime LastReadAt { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// DTO für Lernziel-Fortschritt
/// </summary>
public class LearningGoalProgressDto
{
    public Guid LearningGoalId { get; set; }
    public string Title { get; set; } = string.Empty;
    public float ProgressPercentage { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<string> EvidenceCollected { get; set; } = new();
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// DTO für Story-Community-Feed
/// </summary>
public class StoryCommunityDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string MainCharacterName { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public int RecommendedAge { get; set; }
    public int ReadingTimeMinutes { get; set; }
    public bool HasImages { get; set; }
    public double AverageRating { get; set; }
    public int TotalReads { get; set; }
    public List<string> Tags { get; set; } = new();
    public string? CoverImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsFeatured { get; set; }
    public bool CanRead { get; set; }
}

/// <summary>
/// DTO für Story-Analyse
/// </summary>
public class StoryAnalysisDto
{
    public Guid StoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int WordCount { get; set; }
    public int SentenceCount { get; set; }
    public double ReadabilityScore { get; set; }
    public string ReadabilityLevel { get; set; } = string.Empty;
    public Dictionary<string, int> VocabularyAnalysis { get; set; } = new();
    public Dictionary<string, float> EmotionalToneAnalysis { get; set; } = new();
    public List<string> KeyThemes { get; set; } = new();
    public List<string> EducationalElements { get; set; } = new();
    public List<string> CharacterDevelopmentAspects { get; set; } = new();
    public Dictionary<string, int> TraitInfluenceAnalysis { get; set; } = new();
    public List<string> SafetyFlags { get; set; } = new();
    public bool IsAgeAppropriate { get; set; }
    public List<string> SuggestedImprovements { get; set; } = new();
    public DateTime AnalyzedAt { get; set; }
}

/// <summary>
/// DTO für Story-Template
/// </summary>
public class StoryTemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<string> SupportedGenres { get; set; } = new();
    public int RecommendedAge { get; set; }
    public string Structure { get; set; } = string.Empty;
    public List<string> RequiredElements { get; set; } = new();
    public List<LearningGoalDto> DefaultLearningGoals { get; set; } = new();
    public Dictionary<string, object> TemplateParameters { get; set; } = new();
    public bool IsActive { get; set; }
    public bool IsPublic { get; set; }
    public int UsageCount { get; set; }
    public double Rating { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO für Story-Export
/// </summary>
public class StoryExportDto
{
    public string Format { get; set; } = string.Empty; // "pdf", "epub", "html", "docx"
    public bool IncludeImages { get; set; } = true;
    public bool IncludeCharacterProfile { get; set; } = false;
    public bool IncludeLearningGoals { get; set; } = false;
    public string? CustomCoverImage { get; set; }
    public Dictionary<string, object> FormatOptions { get; set; } = new();
}

/// <summary>
/// DTO für Batch-Story-Operationen
/// </summary>
public class BatchStoryOperationDto
{
    public List<Guid> StoryIds { get; set; } = new();
    public string Operation { get; set; } = string.Empty; // "delete", "archive", "share", "export"
    public Dictionary<string, object>? OperationParameters { get; set; }
}