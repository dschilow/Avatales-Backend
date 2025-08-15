using System.ComponentModel.DataAnnotations;
using Avatales.Application.Common.Interfaces;
using Avatales.Application.Characters.DTOs;
using Avatales.Shared.Models;

namespace Avatales.Application.Characters.Commands;

/// <summary>
/// Command: Neuen Charakter erstellen
/// </summary>
public class CreateCharacterCommand : ICommand<CharacterDetailDto>
{
    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public Guid OwnerId { get; set; }

    [Url]
    public string? AvatarImageUrl { get; set; }

    // DNA-Konfiguration
    public string? PreferredArchetype { get; set; }
    public List<string>? EmphasizedTraits { get; set; }
    public Dictionary<string, int>? CustomTraits { get; set; }
    public List<string>? PersonalityKeywords { get; set; }
    public string? PrimaryMotivation { get; set; }
    public string? LearningStyle { get; set; }

    // Altersgerechte Anpassungen
    [Range(3, 18)]
    public int ChildAge { get; set; } = 7;

    public List<string>? PreferredGenres { get; set; }
    public List<string>? AvoidedTopics { get; set; }

    // Generierungs-Optionen
    public bool UseRandomGeneration { get; set; } = true;
    public bool GenerateAvatar { get; set; } = false;

    // Adoptions-Informationen (für adoptierte Charaktere)
    public Guid? OriginalCharacterId { get; set; }
    public bool KeepOriginalTraits { get; set; } = true;
    public bool KeepOriginalMemories { get; set; } = false;

    public CreateCharacterCommand(string name, Guid ownerId, int childAge = 7)
    {
        Name = name;
        OwnerId = ownerId;
        ChildAge = childAge;
    }
}

/// <summary>
/// Command: Charakter adoptieren von Community
/// </summary>
public class AdoptCharacterCommand : ICommand<CharacterDetailDto>
{
    [Required]
    public Guid OriginalCharacterId { get; set; }

    [Required]
    public Guid NewOwnerId { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string NewName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? NewDescription { get; set; }

    [Required]
    [StringLength(200)]
    public string AdoptionReason { get; set; } = string.Empty;

    public bool KeepOriginalTraits { get; set; } = true;
    public bool KeepOriginalMemories { get; set; } = false;
    public bool KeepOriginalAppearance { get; set; } = true;

    public AdoptCharacterCommand(Guid originalCharacterId, Guid newOwnerId, string newName, string adoptionReason)
    {
        OriginalCharacterId = originalCharacterId;
        NewOwnerId = newOwnerId;
        NewName = newName;
        AdoptionReason = adoptionReason;
    }
}

/// <summary>
/// Command: Charakter-Grundinformationen aktualisieren
/// </summary>
public class UpdateCharacterCommand : ICommand<CharacterDetailDto>
{
    [Required]
    public Guid CharacterId { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Url]
    public string? AvatarImageUrl { get; set; }

    public List<string>? Tags { get; set; }

    public UpdateCharacterCommand(Guid characterId, string name, string description)
    {
        CharacterId = characterId;
        Name = name;
        Description = description;
    }
}

/// <summary>
/// Command: Charakter-Avatar aktualisieren
/// </summary>
public class UpdateCharacterAvatarCommand : ICommand<string>
{
    [Required]
    public Guid CharacterId { get; set; }

    [Required]
    public Stream ImageStream { get; set; } = null!;

    [Required]
    public string FileName { get; set; } = string.Empty;

    [Required]
    public string ContentType { get; set; } = string.Empty;

    [Range(1, ApplicationConstants.FileUpload.MaxImageSizeMB * 1024 * 1024)]
    public long FileSize { get; set; }

    public bool GenerateVariations { get; set; } = false;
    public string? StylePreferences { get; set; }

    public UpdateCharacterAvatarCommand(Guid characterId, Stream imageStream, string fileName, string contentType, long fileSize)
    {
        CharacterId = characterId;
        ImageStream = imageStream;
        FileName = fileName;
        ContentType = contentType;
        FileSize = fileSize;
    }
}

/// <summary>
/// Command: Charakter-Sharing-Status ändern
/// </summary>
public class UpdateCharacterSharingCommand : ICommand
{
    [Required]
    public Guid CharacterId { get; set; }

    [Required]
    public CharacterSharingStatus SharingStatus { get; set; }

    [StringLength(200)]
    public string? ShareMessage { get; set; }

    public List<Guid>? ShareWithUserIds { get; set; }
    public List<string>? ShareCategories { get; set; }

    public UpdateCharacterSharingCommand(Guid characterId, CharacterSharingStatus sharingStatus)
    {
        CharacterId = characterId;
        SharingStatus = sharingStatus;
    }
}

/// <summary>
/// Command: Charakter-Erfahrung aus Story hinzufügen
/// </summary>
public class AddCharacterExperienceCommand : ICommand<List<TraitDevelopmentResultDto>>
{
    [Required]
    public Guid CharacterId { get; set; }

    [Required]
    public Guid StoryId { get; set; }

    [Range(1, 1000)]
    public int ExperienceGained { get; set; }

    public List<string> NewWords { get; set; } = new();
    public List<string> LearningMoments { get; set; } = new();
    public Dictionary<string, float> TraitInfluences { get; set; } = new();
    public List<string> EmotionalExperiences { get; set; } = new();
    public string? StoryContext { get; set; }

    public AddCharacterExperienceCommand(Guid characterId, Guid storyId, int experienceGained)
    {
        CharacterId = characterId;
        StoryId = storyId;
        ExperienceGained = experienceGained;
    }
}

/// <summary>
/// Command: Charakter-Memory hinzufügen
/// </summary>
public class AddCharacterMemoryCommand : ICommand<CharacterMemoryDto>
{
    [Required]
    public Guid CharacterId { get; set; }

    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Summary { get; set; } = string.Empty;

    [StringLength(2000)]
    public string FullContent { get; set; } = string.Empty;

    [Required]
    public MemoryType MemoryType { get; set; }

    [Range(1, 10)]
    public int Importance { get; set; } = 5;

    public DateTime? OccurredAt { get; set; }
    public Guid? StoryId { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<string> AssociatedCharacters { get; set; } = new();
    public List<string> EmotionalContext { get; set; } = new();

    public AddCharacterMemoryCommand(Guid characterId, string title, string summary, MemoryType memoryType)
    {
        CharacterId = characterId;
        Title = title;
        Summary = summary;
        MemoryType = memoryType;
    }
}

/// <summary>
/// Command: Charakter-Trait verstärken
/// </summary>
public class ReinforceCharacterTraitCommand : ICommand<TraitDevelopmentResultDto>
{
    [Required]
    public Guid CharacterId { get; set; }

    [Required]
    public CharacterTraitType TraitType { get; set; }

    [Required]
    [StringLength(200)]
    public string ReinforcementDescription { get; set; } = string.Empty;

    [Range(0.1f, 3.0f)]
    public float Multiplier { get; set; } = 1.5f;

    public Dictionary<string, object>? Context { get; set; }

    public ReinforceCharacterTraitCommand(Guid characterId, CharacterTraitType traitType, string reinforcementDescription)
    {
        CharacterId = characterId;
        TraitType = traitType;
        ReinforcementDescription = reinforcementDescription;
    }
}

/// <summary>
/// Command: Charakter-Memories konsolidieren
/// </summary>
public class ConsolidateCharacterMemoriesCommand : ICommand<CharacterMemoryDto>
{
    [Required]
    public Guid CharacterId { get; set; }

    [Required]
    [MinLength(2)]
    public List<Guid> MemoryIds { get; set; } = new();

    [Required]
    [StringLength(100)]
    public string ConsolidatedTitle { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string ConsolidatedSummary { get; set; } = string.Empty;

    [Required]
    public string ConsolidationReason { get; set; } = string.Empty;

    public bool PreserveOriginalMemories { get; set; } = false;

    public ConsolidateCharacterMemoriesCommand(Guid characterId, List<Guid> memoryIds, string consolidatedTitle, string consolidatedSummary, string consolidationReason)
    {
        CharacterId = characterId;
        MemoryIds = memoryIds;
        ConsolidatedTitle = consolidatedTitle;
        ConsolidatedSummary = consolidatedSummary;
        ConsolidationReason = consolidationReason;
    }
}

/// <summary>
/// Command: Charakter deaktivieren
/// </summary>
public class DeactivateCharacterCommand : ICommand
{
    [Required]
    public Guid CharacterId { get; set; }

    [Required]
    [StringLength(200)]
    public string DeactivationReason { get; set; } = string.Empty;

    public bool PreserveMemories { get; set; } = true;
    public bool NotifyOwner { get; set; } = true;

    public DeactivateCharacterCommand(Guid characterId, string deactivationReason)
    {
        CharacterId = characterId;
        DeactivationReason = deactivationReason;
    }
}

/// <summary>
/// Command: Charakter reaktivieren
/// </summary>
public class ReactivateCharacterCommand : ICommand
{
    [Required]
    public Guid CharacterId { get; set; }

    [StringLength(200)]
    public string? ReactivationReason { get; set; }

    public bool RestoreMemories { get; set; } = true;
    public bool NotifyOwner { get; set; } = true;

    public ReactivateCharacterCommand(Guid characterId)
    {
        CharacterId = characterId;
    }
}

/// <summary>
/// Command: Charakter-Interaktion verarbeiten
/// </summary>
public class ProcessCharacterInteractionCommand : ICommand<List<TraitDevelopmentResultDto>>
{
    [Required]
    public Guid CharacterId { get; set; }

    [Required]
    public string InteractionType { get; set; } = string.Empty; // "story_choice", "user_feedback", "social_interaction"

    [Required]
    public Dictionary<string, object> InteractionData { get; set; } = new();

    public Dictionary<string, object>? Context { get; set; }
    public DateTime? InteractionTimestamp { get; set; }

    public ProcessCharacterInteractionCommand(Guid characterId, string interactionType, Dictionary<string, object> interactionData)
    {
        CharacterId = characterId;
        InteractionType = interactionType;
        InteractionData = interactionData;
    }
}

/// <summary>
/// Command: Charakter-Level-Up verarbeiten
/// </summary>
public class ProcessCharacterLevelUpCommand : ICommand<CharacterDetailDto>
{
    [Required]
    public Guid CharacterId { get; set; }

    public List<string>? UnlockedFeatures { get; set; }
    public List<string>? Achievements { get; set; }
    public Dictionary<string, object>? LevelUpRewards { get; set; }

    public ProcessCharacterLevelUpCommand(Guid characterId)
    {
        CharacterId = characterId;
    }
}

/// <summary>
/// Command: Charakter-Statistiken aktualisieren
/// </summary>
public class UpdateCharacterStatisticsCommand : ICommand
{
    [Required]
    public Guid CharacterId { get; set; }

    [Required]
    public Dictionary<string, object> Statistics { get; set; } = new();

    public bool IncrementCounters { get; set; } = true;
    public DateTime? TimestampOverride { get; set; }

    public UpdateCharacterStatisticsCommand(Guid characterId, Dictionary<string, object> statistics)
    {
        CharacterId = characterId;
        Statistics = statistics;
    }
}

/// <summary>
/// Command: Charakter-Daten exportieren
/// </summary>
public class ExportCharacterDataCommand : ICommand<string>
{
    [Required]
    public Guid CharacterId { get; set; }

    [Required]
    public string Format { get; set; } = "json"; // json, xml, pdf

    public bool IncludeMemories { get; set; } = true;
    public bool IncludeTraitHistory { get; set; } = true;
    public bool IncludeStoryHistory { get; set; } = false;
    public bool IncludeImages { get; set; } = true;
    public List<string> ExcludeFields { get; set; } = new();

    public ExportCharacterDataCommand(Guid characterId, string format = "json")
    {
        CharacterId = characterId;
        Format = format;
    }
}

/// <summary>
/// Command: Charakter löschen
/// </summary>
public class DeleteCharacterCommand : ICommand
{
    [Required]
    public Guid CharacterId { get; set; }

    [Required]
    [StringLength(200)]
    public string DeletionReason { get; set; } = string.Empty;

    public bool HardDelete { get; set; } = false; // Soft delete by default
    public bool DeleteMemories { get; set; } = true;
    public bool NotifyOwner { get; set; } = true;
    public int RetentionDays { get; set; } = 30; // Für Soft Delete

    public DeleteCharacterCommand(Guid characterId, string deletionReason)
    {
        CharacterId = characterId;
        DeletionReason = deletionReason;
    }
}

/// <summary>
/// Command: Batch-Charakter-Operationen
/// </summary>
public class BatchCharacterOperationCommand : ICommand<List<CharacterDto>>
{
    [Required]
    [MinLength(1)]
    public List<Guid> CharacterIds { get; set; } = new();

    [Required]
    public string Operation { get; set; } = string.Empty; // "deactivate", "reactivate", "share", "export", "delete"

    public Dictionary<string, object>? OperationParameters { get; set; }
    public string? Reason { get; set; }

    public BatchCharacterOperationCommand(List<Guid> characterIds, string operation)
    {
        CharacterIds = characterIds;
        Operation = operation;
    }
}

/// <summary>
/// Command: Charakter-Ähnlichkeit analysieren
/// </summary>
public class AnalyzeCharacterCompatibilityCommand : ICommand<List<CharacterRecommendationDto>>
{
    [Required]
    public Guid CharacterId { get; set; }

    public List<Guid>? CompareWithCharacterIds { get; set; }
    public int MaxRecommendations { get; set; } = 10;
    public float MinCompatibilityScore { get; set; } = 0.3f;
    public bool IncludeSameOwner { get; set; } = false;

    public AnalyzeCharacterCompatibilityCommand(Guid characterId)
    {
        CharacterId = characterId;
    }
}

/// <summary>
/// Command: Charakter-DNA regenerieren
/// </summary>
public class RegenerateCharacterDNACommand : ICommand<CharacterDNADto>
{
    [Required]
    public Guid CharacterId { get; set; }

    public bool KeepCurrentTraits { get; set; } = true;
    public bool KeepArchetype { get; set; } = false;
    public List<string>? NewEmphasizedTraits { get; set; }
    public string? NewArchetype { get; set; }
    public string? RegenerationReason { get; set; }

    public RegenerateCharacterDNACommand(Guid characterId)
    {
        CharacterId = characterId;
    }
}