using Avatales.Shared.Models;

namespace Avatales.Application.Characters.DTOs;

/// <summary>
/// Basis-DTO für Charakter-Informationen
/// </summary>
public class CharacterDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AvatarImageUrl { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public Guid? OriginalCharacterId { get; set; }
    public CharacterSharingStatus SharingStatus { get; set; }
    public int Level { get; set; }
    public int ExperiencePoints { get; set; }
    public int StoriesExperienced { get; set; }
    public DateTime LastStoryAt { get; set; }
    public bool IsActive { get; set; }
    public bool IsAdopted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastInteractionAt { get; set; }
}

/// <summary>
/// Detaillierte Charakter-Informationen mit allen Daten
/// </summary>
public class CharacterDetailDto : CharacterDto
{
    public CharacterDNADto DNA { get; set; } = new();
    public List<CharacterTraitDto> Traits { get; set; } = new();
    public List<CharacterMemoryDto> RecentMemories { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public CharacterStatisticsDto Statistics { get; set; } = new();
    public List<CharacterEvolutionDto> RecentEvolution { get; set; } = new();
    public List<string> Achievements { get; set; } = new();
    public CharacterPersonalityDto Personality { get; set; } = new();
    public bool CanSharePublicly { get; set; }
    public bool CanAdoptNewCharacters { get; set; }
    public string DisplayInfo { get; set; } = string.Empty;
}

/// <summary>
/// DTO für Charakter-DNA
/// </summary>
public class CharacterDNADto
{
    public Guid DnaId { get; set; }
    public string Archetype { get; set; } = string.Empty;
    public Dictionary<string, int> BaseTraits { get; set; } = new();
    public List<string> CorePersonalityKeywords { get; set; } = new();
    public string PrimaryMotivation { get; set; } = string.Empty;
    public string CoreFear { get; set; } = string.Empty;
    public string LearningStyle { get; set; } = string.Empty;
    public string DefaultEmotion { get; set; } = string.Empty;
    public int AdaptabilityFactor { get; set; }
    public int EmotionalDepth { get; set; }
    public int SocialTendency { get; set; }
    public List<string> PreferredStoryGenres { get; set; } = new();
    public List<string> AvoidedTopics { get; set; } = new();
    public int ComplexityPreference { get; set; }
    public bool PrefersHappyEndings { get; set; }
    public int ChallengeAffinity { get; set; }
    public string Summary { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO für Charakter-Traits
/// </summary>
public class CharacterTraitDto
{
    public Guid Id { get; set; }
    public string TraitType { get; set; } = string.Empty;
    public string TraitName { get; set; } = string.Empty;
    public int CurrentValue { get; set; }
    public int BaseValue { get; set; }
    public int MaxValue { get; set; }
    public float ExperiencePoints { get; set; }
    public float ProgressToNextLevel { get; set; }
    public string LevelDescription { get; set; } = string.Empty;
    public int TimesReinforced { get; set; }
    public int TimesChallenged { get; set; }
    public float StabilityFactor { get; set; }
    public float GrowthRate { get; set; }
    public List<string> RecentExperiences { get; set; } = new();
    public Dictionary<string, float> InfluenceFactors { get; set; } = new();
    public bool IsReadyForRecognition { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastModified { get; set; }
}

/// <summary>
/// DTO für Charakter-Erinnerungen
/// </summary>
public class CharacterMemoryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string FullContent { get; set; } = string.Empty;
    public string MemoryType { get; set; } = string.Empty;
    public int Importance { get; set; }
    public string ImportanceLevel { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    public int AccessCount { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<string> AssociatedCharacters { get; set; } = new();
    public List<string> EmotionalContext { get; set; } = new();
    public Guid? StoryId { get; set; }
    public string? StoryTitle { get; set; }
    public bool IsConsolidated { get; set; }
    public int DecayResistance { get; set; }
    public List<Guid> LinkedMemoryIds { get; set; } = new();
}

/// <summary>
/// DTO für Charakter-Statistiken
/// </summary>
public class CharacterStatisticsDto
{
    public int Level { get; set; }
    public int ExperiencePoints { get; set; }
    public int StoriesExperienced { get; set; }
    public int TotalWordsLearned { get; set; }
    public int TotalLessonsCompleted { get; set; }
    public int TimesSortiesByOthers { get; set; }
    public int MemoriesCount { get; set; }
    public int ConsolidatedMemories { get; set; }
    public Dictionary<string, int> TraitEvolutions { get; set; } = new();
    public Dictionary<string, int> StoryGenreExperience { get; set; } = new();
    public Dictionary<string, int> LearningCategoryProgress { get; set; } = new();
    public List<string> Achievements { get; set; } = new();
    public DateTime LastActivityAt { get; set; }
    public TimeSpan TotalPlayTime { get; set; }
    public double AverageSessionLength { get; set; }
}

/// <summary>
/// DTO für Charakter-Evolution/Entwicklung
/// </summary>
public class CharacterEvolutionDto
{
    public DateTime Timestamp { get; set; }
    public string EvolutionType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Changes { get; set; } = new();
    public string? TriggerStoryId { get; set; }
    public string? TriggerStoryTitle { get; set; }
}

/// <summary>
/// DTO für Charakter-Persönlichkeit (Zusammenfassung)
/// </summary>
public class CharacterPersonalityDto
{
    public string Archetype { get; set; } = string.Empty;
    public List<string> StrongestTraits { get; set; } = new();
    public List<string> DevelopingTraits { get; set; } = new();
    public string PrimaryMotivation { get; set; } = string.Empty;
    public string LearningStyle { get; set; } = string.Empty;
    public string EmotionalTendency { get; set; } = string.Empty;
    public List<string> PreferredActivities { get; set; } = new();
    public List<string> RecentGrowthAreas { get; set; } = new();
    public string PersonalitySummary { get; set; } = string.Empty;
}

/// <summary>
/// DTO für Charakter-Erstellung
/// </summary>
public class CreateCharacterDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? AvatarImageUrl { get; set; }
    public string? PreferredArchetype { get; set; }
    public List<string>? EmphasizedTraits { get; set; }
    public Dictionary<string, int>? CustomTraits { get; set; }
    public List<string>? PersonalityKeywords { get; set; }
    public string? PrimaryMotivation { get; set; }
    public string? LearningStyle { get; set; }
    public int ChildAge { get; set; } = 7;
    public List<string>? PreferredGenres { get; set; }
    public bool UseRandomGeneration { get; set; } = true;
}

/// <summary>
/// DTO für Charakter-Update
/// </summary>
public class UpdateCharacterDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? AvatarImageUrl { get; set; }
    public List<string>? Tags { get; set; }
}

/// <summary>
/// DTO für Charakter-Adoption
/// </summary>
public class AdoptCharacterDto
{
    public Guid OriginalCharacterId { get; set; }
    public string NewName { get; set; } = string.Empty;
    public string? NewDescription { get; set; }
    public string AdoptionReason { get; set; } = string.Empty;
    public bool KeepOriginalTraits { get; set; } = true;
    public bool KeepOriginalMemories { get; set; } = false;
}

/// <summary>
/// DTO für Charakter-Sharing
/// </summary>
public class ShareCharacterDto
{
    public CharacterSharingStatus SharingStatus { get; set; }
    public string? ShareMessage { get; set; }
    public List<Guid>? ShareWithUserIds { get; set; }
    public List<string>? ShareCategories { get; set; }
}

/// <summary>
/// DTO für Charakter-Interaktion
/// </summary>
public class CharacterInteractionDto
{
    public Guid CharacterId { get; set; }
    public string InteractionType { get; set; } = string.Empty;
    public string InteractionData { get; set; } = string.Empty;
    public Dictionary<string, object>? Context { get; set; }
}

/// <summary>
/// DTO für Trait-Entwicklung-Ergebnis
/// </summary>
public class TraitDevelopmentResultDto
{
    public string TraitName { get; set; } = string.Empty;
    public int PreviousValue { get; set; }
    public int NewValue { get; set; }
    public float ExperienceGained { get; set; }
    public bool ValueChanged { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool LeveledUp { get; set; }
    public List<string> UnlockedFeatures { get; set; } = new();
}

/// <summary>
/// DTO für Charakter-Community-Feed
/// </summary>
public class CharacterCommunityDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AvatarImageUrl { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public int Level { get; set; }
    public List<string> StrongestTraits { get; set; } = new();
    public string Archetype { get; set; } = string.Empty;
    public int StoriesExperienced { get; set; }
    public CharacterSharingStatus SharingStatus { get; set; }
    public DateTime SharedAt { get; set; }
    public int AdoptionCount { get; set; }
    public double PopularityScore { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool CanAdopt { get; set; }
}

/// <summary>
/// DTO für Charakter-Suche
/// </summary>
public class CharacterSearchDto
{
    public string? NameQuery { get; set; }
    public string? Archetype { get; set; }
    public List<string>? Traits { get; set; }
    public int? MinLevel { get; set; }
    public int? MaxLevel { get; set; }
    public CharacterSharingStatus? SharingStatus { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public List<string>? Tags { get; set; }
    public string? SortBy { get; set; } // "popularity", "recent", "level", "name"
    public bool SortDescending { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// DTO für Charakter-Empfehlungen
/// </summary>
public class CharacterRecommendationDto
{
    public CharacterDto Character { get; set; } = new();
    public float CompatibilityScore { get; set; }
    public List<string> RecommendationReasons { get; set; } = new();
    public List<string> SharedTraits { get; set; } = new();
    public List<string> ComplementaryTraits { get; set; } = new();
    public bool IsFromSameArchetype { get; set; }
    public string RecommendationType { get; set; } = string.Empty; // "similar", "complementary", "popular"
}