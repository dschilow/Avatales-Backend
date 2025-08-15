using Avatales.Domain.Characters.ValueObjects;
using Avatales.Domain.Characters.Events;
using Avatales.Shared.Models;

namespace Avatales.Domain.Characters.Entities;

/// <summary>
/// Character-Entität repräsentiert einen persistenten Avatar mit Gedächtnis und Entwicklung
/// Kernstück des Avatales-Systems mit Trait-Evolution und Story-Memory
/// </summary>
public class Character : BaseEntity
{
    private readonly List<CharacterTrait> _traits = new();
    private readonly List<CharacterMemory> _memories = new();
    private readonly List<string> _tags = new();

    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string AvatarImageUrl { get; private set; } = string.Empty;
    public Guid OwnerId { get; private set; }
    public Guid? OriginalCharacterId { get; private set; } // Für adoptierte Charaktere
    public CharacterSharingStatus SharingStatus { get; private set; } = CharacterSharingStatus.Private;
    public int Level { get; private set; } = 1;
    public int ExperiencePoints { get; private set; } = 0;
    public int StoriesExperienced { get; private set; } = 0;
    public DateTime LastStoryAt { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Character DNA (unveränderliche Basis-Eigenschaften)
    public CharacterDNA DNA { get; private set; } = default!;

    // Navigation Properties
    public IReadOnlyCollection<CharacterTrait> Traits => _traits.AsReadOnly();
    public IReadOnlyCollection<CharacterMemory> Memories => _memories.AsReadOnly();
    public IReadOnlyCollection<string> Tags => _tags.AsReadOnly();

    // Statistics
    public int TotalWordsLearned { get; private set; } = 0;
    public int TotalLessonsCompleted { get; private set; } = 0;
    public int TimesSortiesByOthers { get; private set; } = 0;
    public DateTime? LastInteractionAt { get; private set; }

    protected Character() { } // For EF Core

    public Character(
        string name,
        string description,
        Guid ownerId,
        CharacterDNA dna,
        string avatarImageUrl = "",
        Guid? originalCharacterId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Character name cannot be empty", nameof(name));

        if (name.Length > 50)
            throw new ArgumentException("Character name cannot exceed 50 characters", nameof(name));

        if (!name.IsChildFriendly())
            throw new ArgumentException("Character name contains inappropriate content", nameof(name));

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        OwnerId = ownerId;
        DNA = dna ?? throw new ArgumentNullException(nameof(dna));
        AvatarImageUrl = avatarImageUrl?.Trim() ?? string.Empty;
        OriginalCharacterId = originalCharacterId;
        LastStoryAt = DateTime.UtcNow;
        LastInteractionAt = DateTime.UtcNow;

        // Initialisiere Basis-Traits basierend auf der DNA
        InitializeTraitsFromDNA();

        AddDomainEvent(new CharacterCreatedEvent(Id, name, ownerId, originalCharacterId.HasValue));
    }

    public string GetDisplayInfo()
    {
        return $"{Name} (Level {Level}) - {StoriesExperienced} Geschichten erlebt";
    }

    public bool IsAdoptedCharacter()
    {
        return OriginalCharacterId.HasValue;
    }

    public bool CanSharePublicly()
    {
        return StoriesExperienced >= 3 && Level >= 2;
    }

    public bool CanAdoptNewCharacters()
    {
        // Ein Benutzer kann weitere Charaktere adoptieren wenn er bereits einen entwickelt hat
        return StoriesExperienced >= 5 && Level >= 3;
    }

    public void UpdateBasicInfo(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Character name cannot be empty", nameof(name));

        if (!name.IsChildFriendly())
            throw new ArgumentException("Character name contains inappropriate content", nameof(name));

        var oldName = Name;
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;

        MarkAsUpdated();

        if (oldName != Name)
        {
            AddDomainEvent(new CharacterRenamedEvent(Id, oldName, Name));
        }
    }

    public void UpdateAvatar(string avatarImageUrl)
    {
        AvatarImageUrl = avatarImageUrl?.Trim() ?? string.Empty;
        MarkAsUpdated();
        AddDomainEvent(new CharacterAvatarUpdatedEvent(Id, avatarImageUrl));
    }

    public void UpdateSharingStatus(CharacterSharingStatus newStatus)
    {
        if (!CanSharePublicly() && newStatus == CharacterSharingStatus.Community)
        {
            throw new InvalidOperationException("Character must have at least 3 stories and level 2 to share publicly");
        }

        var oldStatus = SharingStatus;
        SharingStatus = newStatus;
        MarkAsUpdated();

        AddDomainEvent(new CharacterSharingStatusChangedEvent(Id, oldStatus, newStatus));
    }

    public void AddExperienceFromStory(int experienceGained, List<string>? newWordsLearned = null)
    {
        ExperiencePoints += experienceGained;
        StoriesExperienced++;
        LastStoryAt = DateTime.UtcNow;
        LastInteractionAt = DateTime.UtcNow;

        if (newWordsLearned != null)
        {
            TotalWordsLearned += newWordsLearned.Count;
        }

        // Level-Up prüfen
        var newLevel = CalculateLevel(ExperiencePoints);
        if (newLevel > Level)
        {
            var oldLevel = Level;
            Level = newLevel;
            AddDomainEvent(new CharacterLeveledUpEvent(Id, oldLevel, newLevel));
        }

        MarkAsUpdated();
        AddDomainEvent(new CharacterExperienceGainedEvent(Id, experienceGained, StoriesExperienced));
    }

    public void AddMemory(CharacterMemory memory)
    {
        if (memory == null)
            throw new ArgumentNullException(nameof(memory));

        // Prüfe Memory-Limit (wird später durch Memory-System ersetzt)
        if (_memories.Count >= 100) // Temporäres Limit
        {
            // Entferne älteste, unwichtige Memories
            var oldMemories = _memories
                .Where(m => m.Importance <= 2)
                .OrderBy(m => m.OccurredAt)
                .Take(_memories.Count - 99)
                .ToList();

            foreach (var oldMemory in oldMemories)
            {
                _memories.Remove(oldMemory);
            }
        }

        _memories.Add(memory);
        MarkAsUpdated();
        AddDomainEvent(new CharacterMemoryAddedEvent(Id, memory.Title, memory.Importance));
    }

    public void UpdateTrait(CharacterTraitType traitType, int change, string reason = "")
    {
        var trait = _traits.FirstOrDefault(t => t.TraitType == traitType);
        if (trait != null)
        {
            var oldValue = trait.Value;
            trait.AdjustValue(change, reason);
            
            AddDomainEvent(new CharacterTraitChangedEvent(Id, traitType, oldValue, trait.Value, reason));
        }

        MarkAsUpdated();
    }

    public CharacterTrait? GetTrait(CharacterTraitType traitType)
    {
        return _traits.FirstOrDefault(t => t.TraitType == traitType);
    }

    public int GetTraitValue(CharacterTraitType traitType)
    {
        return GetTrait(traitType)?.Value ?? 0;
    }

    public void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return;

        var cleanTag = tag.Trim().ToLower();
        if (!_tags.Contains(cleanTag) && _tags.Count < 10)
        {
            _tags.Add(cleanTag);
            MarkAsUpdated();
        }
    }

    public void RemoveTag(string tag)
    {
        if (_tags.Remove(tag?.Trim().ToLower() ?? string.Empty))
        {
            MarkAsUpdated();
        }
    }

    public List<CharacterMemory> GetRecentMemories(int count = 5)
    {
        return _memories
            .OrderByDescending(m => m.OccurredAt)
            .Take(count)
            .ToList();
    }

    public List<CharacterMemory> GetImportantMemories(int minImportance = 4)
    {
        return _memories
            .Where(m => m.Importance >= minImportance)
            .OrderByDescending(m => m.Importance)
            .ThenByDescending(m => m.OccurredAt)
            .ToList();
    }

    public Character CreateAdoptionCopy(Guid newOwnerId)
    {
        // Erstelle eine Kopie für Adoption
        var adoptedCharacter = new Character(
            Name,
            Description,
            newOwnerId,
            DNA.CreateCopy(),
            AvatarImageUrl,
            Id); // Original-ID setzen

        // Basis-Traits übertragen, aber Erfahrung zurücksetzen
        foreach (var trait in _traits)
        {
            adoptedCharacter._traits.Add(trait.CreateBaseCopy());
        }

        // Nur grundlegende Memories übertragen (DNA-basierte Erinnerungen)
        var basicMemories = _memories
            .Where(m => m.Importance >= 4)
            .Take(3)
            .Select(m => m.CreateBaseCopy())
            .ToList();

        foreach (var memory in basicMemories)
        {
            adoptedCharacter._memories.Add(memory);
        }

        return adoptedCharacter;
    }

    public void MarkAsShared()
    {
        TimesSortiesByOthers++;
        MarkAsUpdated();
        AddDomainEvent(new CharacterSharedEvent(Id, TimesSortiesByOthers));
    }

    public void Deactivate(string reason)
    {
        IsActive = false;
        MarkAsUpdated();
        AddDomainEvent(new CharacterDeactivatedEvent(Id, reason));
    }

    public void Reactivate()
    {
        IsActive = true;
        LastInteractionAt = DateTime.UtcNow;
        MarkAsUpdated();
        AddDomainEvent(new CharacterReactivatedEvent(Id));
    }

    private void InitializeTraitsFromDNA()
    {
        foreach (var baseTrait in DNA.BaseTraits)
        {
            var trait = new CharacterTrait(
                baseTrait.TraitType,
                baseTrait.BaseValue,
                $"Initial trait from character DNA");
                
            _traits.Add(trait);
        }
    }

    private static int CalculateLevel(int experiencePoints)
    {
        // Einfache Level-Berechnung: Level = sqrt(XP/100) + 1
        return Math.Min(50, (int)Math.Sqrt(experiencePoints / 100.0) + 1);
    }

    public CharacterSnapshot CreateSnapshot()
    {
        return new CharacterSnapshot(
            Id,
            Name,
            Description,
            Level,
            StoriesExperienced,
            _traits.ToDictionary(t => t.TraitType, t => t.Value),
            GetRecentMemories(3).Select(m => m.Summary).ToList(),
            DateTime.UtcNow);
    }

    // Hilfsmethoden für Geschichten-Generierung
    public string GetPersonalityDescription()
    {
        var dominantTraits = _traits
            .Where(t => t.Value >= 7)
            .OrderByDescending(t => t.Value)
            .Take(3)
            .ToList();

        if (!dominantTraits.Any())
            return "Ein ausgewogener Charakter mit vielfältigen Eigenschaften.";

        var traitDescriptions = dominantTraits
            .Select(t => GetTraitDescription(t.TraitType, t.Value))
            .ToList();

        return string.Join(", ", traitDescriptions) + ".";
    }

    private static string GetTraitDescription(CharacterTraitType traitType, int value)
    {
        var intensity = value switch
        {
            >= 9 => "sehr",
            >= 7 => "ziemlich",
            >= 5 => "etwas",
            _ => "wenig"
        };

        var traitName = traitType switch
        {
            CharacterTraitType.Courage => "mutig",
            CharacterTraitType.Curiosity => "neugierig",
            CharacterTraitType.Kindness => "freundlich",
            CharacterTraitType.Creativity => "kreativ",
            CharacterTraitType.Intelligence => "intelligent",
            CharacterTraitType.Humor => "humorvoll",
            CharacterTraitType.Wisdom => "weise",
            CharacterTraitType.Empathy => "einfühlsam",
            CharacterTraitType.Determination => "entschlossen",
            CharacterTraitType.Optimism => "optimistisch",
            _ => "besonders"
        };

        return $"{intensity} {traitName}";
    }
}