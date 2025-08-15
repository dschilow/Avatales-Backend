using Avatales.Shared.Models;

namespace Avatales.Domain.Characters.Events;

/// <summary>
/// Event: Neuer Charakter wurde erstellt
/// </summary>
public class CharacterCreatedEvent : DomainEvent
{
    public string CharacterName { get; }
    public Guid OwnerId { get; }
    public bool IsAdopted { get; }
    public DateTime CreationTimestamp { get; }

    public CharacterCreatedEvent(Guid characterId, string characterName, Guid ownerId, bool isAdopted) 
        : base(characterId)
    {
        CharacterName = characterName;
        OwnerId = ownerId;
        IsAdopted = isAdopted;
        CreationTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "CharacterCreated";
}

/// <summary>
/// Event: Charakter wurde umbenannt
/// </summary>
public class CharacterRenamedEvent : DomainEvent
{
    public string OldName { get; }
    public string NewName { get; }
    public DateTime RenamedTimestamp { get; }

    public CharacterRenamedEvent(Guid characterId, string oldName, string newName) 
        : base(characterId)
    {
        OldName = oldName;
        NewName = newName;
        RenamedTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "CharacterRenamed";
}

/// <summary>
/// Event: Charakter-Avatar wurde aktualisiert
/// </summary>
public class CharacterAvatarUpdatedEvent : DomainEvent
{
    public string? NewAvatarUrl { get; }
    public DateTime UpdatedTimestamp { get; }

    public CharacterAvatarUpdatedEvent(Guid characterId, string? newAvatarUrl) 
        : base(characterId)
    {
        NewAvatarUrl = newAvatarUrl;
        UpdatedTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "CharacterAvatarUpdated";
}

/// <summary>
/// Event: Charakter-Teilungsstatus wurde geändert
/// </summary>
public class CharacterSharingStatusChangedEvent : DomainEvent
{
    public CharacterSharingStatus OldStatus { get; }
    public CharacterSharingStatus NewStatus { get; }
    public DateTime StatusChangedTimestamp { get; }

    public CharacterSharingStatusChangedEvent(
        Guid characterId, 
        CharacterSharingStatus oldStatus, 
        CharacterSharingStatus newStatus) 
        : base(characterId)
    {
        OldStatus = oldStatus;
        NewStatus = newStatus;
        StatusChangedTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "CharacterSharingStatusChanged";
}

/// <summary>
/// Event: Charakter hat Erfahrung aus einer Geschichte gewonnen
/// </summary>
public class CharacterGainedExperienceEvent : DomainEvent
{
    public Guid StoryId { get; }
    public int ExperienceGained { get; }
    public int NewExperienceTotal { get; }
    public List<string> NewWords { get; }
    public List<string> TraitsInfluenced { get; }
    public DateTime ExperienceGainedTimestamp { get; }

    public CharacterGainedExperienceEvent(
        Guid characterId,
        Guid storyId,
        int experienceGained,
        int newExperienceTotal,
        List<string> newWords,
        List<string> traitsInfluenced) 
        : base(characterId)
    {
        StoryId = storyId;
        ExperienceGained = experienceGained;
        NewExperienceTotal = newExperienceTotal;
        NewWords = newWords ?? new List<string>();
        TraitsInfluenced = traitsInfluenced ?? new List<string>();
        ExperienceGainedTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "CharacterGainedExperience";
}

/// <summary>
/// Event: Charakter ist ein Level aufgestiegen
/// </summary>
public class CharacterLeveledUpEvent : DomainEvent
{
    public int OldLevel { get; }
    public int NewLevel { get; }
    public int TotalExperience { get; }
    public List<string> UnlockedFeatures { get; }
    public DateTime LevelUpTimestamp { get; }

    public CharacterLeveledUpEvent(
        Guid characterId,
        int oldLevel,
        int newLevel,
        int totalExperience,
        List<string>? unlockedFeatures = null) 
        : base(characterId)
    {
        OldLevel = oldLevel;
        NewLevel = newLevel;
        TotalExperience = totalExperience;
        UnlockedFeatures = unlockedFeatures ?? new List<string>();
        LevelUpTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "CharacterLeveledUp";
}

/// <summary>
/// Event: Charakter-Trait wurde verbessert
/// </summary>
public class CharacterTraitImprovedEvent : DomainEvent
{
    public CharacterTraitType TraitType { get; }
    public int OldValue { get; }
    public int NewValue { get; }
    public string ReasonForImprovement { get; }
    public float ExperienceGained { get; }
    public DateTime ImprovementTimestamp { get; }

    public CharacterTraitImprovedEvent(
        Guid characterId,
        CharacterTraitType traitType,
        int oldValue,
        int newValue,
        string reasonForImprovement,
        float experienceGained) 
        : base(characterId)
    {
        TraitType = traitType;
        OldValue = oldValue;
        NewValue = newValue;
        ReasonForImprovement = reasonForImprovement;
        ExperienceGained = experienceGained;
        ImprovementTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "CharacterTraitImproved";
}

/// <summary>
/// Event: Neue Erinnerung wurde hinzugefügt
/// </summary>
public class CharacterMemoryAddedEvent : DomainEvent
{
    public Guid MemoryId { get; }
    public MemoryType MemoryType { get; }
    public string MemoryTitle { get; }
    public int Importance { get; }
    public Guid? StoryId { get; }
    public DateTime MemoryAddedTimestamp { get; }

    public CharacterMemoryAddedEvent(
        Guid characterId,
        Guid memoryId,
        MemoryType memoryType,
        string memoryTitle,
        int importance,
        Guid? storyId = null) 
        : base(characterId)
    {
        MemoryId = memoryId;
        MemoryType = memoryType;
        MemoryTitle = memoryTitle;
        Importance = importance;
        StoryId = storyId;
        MemoryAddedTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "CharacterMemoryAdded";
}

/// <summary>
/// Event: Erinnerung wurde konsolidiert (kombiniert/verstärkt)
/// </summary>
public class CharacterMemoryConsolidatedEvent : DomainEvent
{
    public List<Guid> ConsolidatedMemoryIds { get; }
    public Guid NewMemoryId { get; }
    public string ConsolidationReason { get; }
    public DateTime ConsolidationTimestamp { get; }

    public CharacterMemoryConsolidatedEvent(
        Guid characterId,
        List<Guid> consolidatedMemoryIds,
        Guid newMemoryId,
        string consolidationReason) 
        : base(characterId)
    {
        ConsolidatedMemoryIds = consolidatedMemoryIds ?? new List<Guid>();
        NewMemoryId = newMemoryId;
        ConsolidationReason = consolidationReason;
        ConsolidationTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "CharacterMemoryConsolidated";
}

/// <summary>
/// Event: Charakter wurde von anderem Benutzer adoptiert
/// </summary>
public class CharacterAdoptedEvent : DomainEvent
{
    public Guid NewOwnerId { get; }
    public Guid? PreviousOwnerId { get; }
    public string AdoptionReason { get; }
    public DateTime AdoptionTimestamp { get; }

    public CharacterAdoptedEvent(
        Guid characterId,
        Guid newOwnerId,
        Guid? previousOwnerId,
        string adoptionReason) 
        : base(characterId)
    {
        NewOwnerId = newOwnerId;
        PreviousOwnerId = previousOwnerId;
        AdoptionReason = adoptionReason;
        AdoptionTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "CharacterAdopted";
}

/// <summary>
/// Event: Charakter wurde in der Community geteilt
/// </summary>
public class CharacterSharedToCommunityEvent : DomainEvent
{
    public Guid SharedById { get; }
    public CharacterSharingStatus ShareLevel { get; }
    public string? ShareMessage { get; }
    public DateTime SharedTimestamp { get; }

    public CharacterSharedToCommunityEvent(
        Guid characterId,
        Guid sharedById,
        CharacterSharingStatus shareLevel,
        string? shareMessage = null) 
        : base(characterId)
    {
        SharedById = sharedById;
        ShareLevel = shareLevel;
        ShareMessage = shareMessage;
        SharedTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "CharacterSharedToCommunity";
}

/// <summary>
/// Event: Charakter wurde als inaktiv markiert
/// </summary>
public class CharacterDeactivatedEvent : DomainEvent
{
    public string DeactivationReason { get; }
    public DateTime DeactivatedTimestamp { get; }

    public CharacterDeactivatedEvent(Guid characterId, string deactivationReason) 
        : base(characterId)
    {
        DeactivationReason = deactivationReason;
        DeactivatedTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "CharacterDeactivated";
}

/// <summary>
/// Event: Charakter wurde wieder aktiviert
/// </summary>
public class CharacterReactivatedEvent : DomainEvent
{
    public string ReactivationReason { get; }
    public DateTime ReactivatedTimestamp { get; }

    public CharacterReactivatedEvent(Guid characterId, string reactivationReason) 
        : base(characterId)
    {
        ReactivationReason = reactivationReason;
        ReactivatedTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "CharacterReactivated";
}

/// <summary>
/// Event: Charakter hat eine bedeutsame Interaktion mit anderem Charakter
/// </summary>
public class CharacterSocialInteractionEvent : DomainEvent
{
    public Guid OtherCharacterId { get; }
    public string InteractionType { get; }
    public string InteractionDescription { get; }
    public List<CharacterTraitType> TraitsInfluenced { get; }
    public DateTime InteractionTimestamp { get; }

    public CharacterSocialInteractionEvent(
        Guid characterId,
        Guid otherCharacterId,
        string interactionType,
        string interactionDescription,
        List<CharacterTraitType>? traitsInfluenced = null) 
        : base(characterId)
    {
        OtherCharacterId = otherCharacterId;
        InteractionType = interactionType;
        InteractionDescription = interactionDescription;
        TraitsInfluenced = traitsInfluenced ?? new List<CharacterTraitType>();
        InteractionTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "CharacterSocialInteraction";
}

/// <summary>
/// Event: Charakter hat ein Lernziel erreicht
/// </summary>
public class CharacterLearningGoalAchievedEvent : DomainEvent
{
    public Guid LearningGoalId { get; }
    public string LearningGoalTitle { get; }
    public LearningGoalCategory Category { get; }
    public int AttemptsNeeded { get; }
    public List<string> EvidenceOfLearning { get; }
    public DateTime AchievedTimestamp { get; }

    public CharacterLearningGoalAchievedEvent(
        Guid characterId,
        Guid learningGoalId,
        string learningGoalTitle,
        LearningGoalCategory category,
        int attemptsNeeded,
        List<string>? evidenceOfLearning = null) 
        : base(characterId)
    {
        LearningGoalId = learningGoalId;
        LearningGoalTitle = learningGoalTitle;
        Category = category;
        AttemptsNeeded = attemptsNeeded;
        EvidenceOfLearning = evidenceOfLearning ?? new List<string>();
        AchievedTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "CharacterLearningGoalAchieved";
}

/// <summary>
/// Event: Charakter hat einen Meilenstein erreicht
/// </summary>
public class CharacterMilestoneReachedEvent : DomainEvent
{
    public string MilestoneType { get; }
    public string MilestoneDescription { get; }
    public Dictionary<string, object> MilestoneData { get; }
    public DateTime MilestoneTimestamp { get; }

    public CharacterMilestoneReachedEvent(
        Guid characterId,
        string milestoneType,
        string milestoneDescription,
        Dictionary<string, object>? milestoneData = null) 
        : base(characterId)
    {
        MilestoneType = milestoneType;
        MilestoneDescription = milestoneDescription;
        MilestoneData = milestoneData ?? new Dictionary<string, object>();
        MilestoneTimestamp = DateTime.UtcNow;
    }

    public override string EventType => "CharacterMilestoneReached";
}