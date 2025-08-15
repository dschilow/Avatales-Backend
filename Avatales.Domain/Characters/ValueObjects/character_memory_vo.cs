using Avatales.Shared.Models;

namespace Avatales.Domain.Characters.ValueObjects;

/// <summary>
/// CharacterMemory Value Object - Erinnerung eines Charakters
/// Vorbereitung für das hierarchische Memory-System (wird später vollständig implementiert)
/// </summary>
public class CharacterMemory : IEquatable<CharacterMemory>
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Title { get; private set; } = string.Empty;
    public string Summary { get; private set; } = string.Empty;
    public string FullContent { get; private set; } = string.Empty;
    public MemoryType MemoryType { get; private set; }
    public int Importance { get; private set; } // 1-10 Scale
    public DateTime OccurredAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastAccessedAt { get; private set; }
    public int AccessCount { get; private set; } = 0;
    public List<string> Tags { get; private set; } = new();
    public List<string> AssociatedCharacters { get; private set; } = new();
    public List<string> EmotionalContext { get; private set; } = new();
    public Guid? StoryId { get; private set; }
    public bool IsConsolidated { get; private set; } = false;

    // Memory-System Eigenschaften (für zukünftige Implementierung)
    public MemoryImportance ImportanceLevel { get; private set; }
    public int DecayResistance { get; private set; } = 1; // Wie gut widersteht das Memory dem Vergessen
    public List<Guid> LinkedMemoryIds { get; private set; } = new();

    protected CharacterMemory() { } // For EF Core

    public CharacterMemory(
        string title,
        string summary,
        MemoryType memoryType,
        int importance = 5,
        DateTime? occurredAt = null,
        Guid? storyId = null,
        string fullContent = "")
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Memory title cannot be empty", nameof(title));

        if (string.IsNullOrWhiteSpace(summary))
            throw new ArgumentException("Memory summary cannot be empty", nameof(summary));

        if (importance < 1 || importance > 10)
            throw new ArgumentException("Importance must be between 1 and 10", nameof(importance));

        Title = title.Trim();
        Summary = summary.Trim();
        FullContent = fullContent?.Trim() ?? string.Empty;
        MemoryType = memoryType;
        Importance = importance;
        OccurredAt = occurredAt ?? DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
        StoryId = storyId;

        // Bestimme Importance Level basierend auf numerischem Wert
        ImportanceLevel = importance switch
        {
            >= 9 => MemoryImportance.Critical,
            >= 7 => MemoryImportance.High,
            >= 5 => MemoryImportance.Medium,
            >= 3 => MemoryImportance.Low,
            _ => MemoryImportance.Trivial
        };

        // Decay Resistance basierend auf Importance
        DecayResistance = importance switch
        {
            >= 8 => 5, // Sehr widerstandsfähig
            >= 6 => 3, // Mäßig widerstandsfähig
            >= 4 => 2, // Etwas widerstandsfähig
            _ => 1      // Schwach widerstandsfähig
        };
    }

    public void Access()
    {
        AccessCount++;
        LastAccessedAt = DateTime.UtcNow;

        // Wichtige Memories werden durch Zugriff stärker
        if (Importance < 10 && AccessCount % 5 == 0)
        {
            Importance = Math.Min(10, Importance + 1);
            UpdateImportanceLevel();
        }
    }

    public void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return;

        var cleanTag = tag.Trim().ToLower();
        if (!Tags.Contains(cleanTag) && Tags.Count < 10)
        {
            Tags.Add(cleanTag);
        }
    }

    public void AddAssociatedCharacter(string characterName)
    {
        if (string.IsNullOrWhiteSpace(characterName)) return;

        var cleanName = characterName.Trim();
        if (!AssociatedCharacters.Contains(cleanName, StringComparer.OrdinalIgnoreCase))
        {
            AssociatedCharacters.Add(cleanName);
        }
    }

    public void AddEmotionalContext(string emotion)
    {
        if (string.IsNullOrWhiteSpace(emotion)) return;

        var cleanEmotion = emotion.Trim().ToLower();
        if (!EmotionalContext.Contains(cleanEmotion) && EmotionalContext.Count < 5)
        {
            EmotionalContext.Add(cleanEmotion);
        }
    }

    public void LinkToMemory(Guid memoryId)
    {
        if (!LinkedMemoryIds.Contains(memoryId) && LinkedMemoryIds.Count < 20)
        {
            LinkedMemoryIds.Add(memoryId);
        }
    }

    public void MarkAsConsolidated()
    {
        IsConsolidated = true;
        
        // Konsolidierte Memories sind wichtiger und widerstandsfähiger
        if (Importance < 10)
        {
            Importance = Math.Min(10, Importance + 1);
            UpdateImportanceLevel();
        }

        DecayResistance = Math.Min(5, DecayResistance + 1);
    }

    public bool IsRecentMemory(int hours = 24)
    {
        return (DateTime.UtcNow - CreatedAt).TotalHours <= hours;
    }

    public bool IsOldMemory(int days = 30)
    {
        return (DateTime.UtcNow - CreatedAt).TotalDays >= days;
    }

    public bool IsFrequentlyAccessed()
    {
        return AccessCount >= 5;
    }

    public bool IsRarelyAccessed()
    {
        return AccessCount <= 1 && (DateTime.UtcNow - CreatedAt).TotalDays >= 7;
    }

    public bool ShouldBePreserved()
    {
        return Importance >= 7 || 
               IsFrequentlyAccessed() || 
               IsConsolidated ||
               MemoryType == MemoryType.Achievement;
    }

    public double GetMemoryStrength()
    {
        var baseStrength = Importance / 10.0;
        var accessBonus = Math.Min(0.3, AccessCount * 0.05);
        var timeDecay = IsRecentMemory() ? 0 : Math.Min(0.5, (DateTime.UtcNow - CreatedAt).TotalDays * 0.01);
        var consolidationBonus = IsConsolidated ? 0.2 : 0;

        return Math.Max(0.1, Math.Min(1.0, baseStrength + accessBonus + consolidationBonus - timeDecay));
    }

    public string GetMemoryStrengthDescription()
    {
        var strength = GetMemoryStrength();
        return strength switch
        {
            >= 0.9 => "Sehr lebendige Erinnerung",
            >= 0.7 => "Klare Erinnerung",
            >= 0.5 => "Deutliche Erinnerung",
            >= 0.3 => "Schwache Erinnerung",
            _ => "Verschwommene Erinnerung"
        };
    }

    public List<string> GetContextKeywords()
    {
        var keywords = new List<string>();
        keywords.AddRange(Tags);
        keywords.AddRange(AssociatedCharacters);
        keywords.AddRange(EmotionalContext);
        
        // Füge Memory-Type spezifische Keywords hinzu
        keywords.AddRange(MemoryType switch
        {
            MemoryType.Experience => new[] { "erfahrung", "erlebnis", "abenteuer" },
            MemoryType.Learning => new[] { "lernen", "wissen", "entdeckung" },
            MemoryType.Emotional => new[] { "gefühl", "emotion", "stimmung" },
            MemoryType.Relationship => new[] { "freundschaft", "beziehung", "begegnung" },
            MemoryType.Achievement => new[] { "erfolg", "erreicht", "geschafft" },
            _ => new[] { "erinnerung" }
        });

        return keywords.Distinct().ToList();
    }

    public CharacterMemory CreateBaseCopy()
    {
        // Für Adoption: Erstelle vereinfachte Kopie mit reduzierter Wichtigkeit
        var adoptionImportance = Math.Max(1, Importance - 2);
        
        return new CharacterMemory(
            Title,
            Summary,
            MemoryType,
            adoptionImportance,
            OccurredAt,
            null, // Keine Story-Verbindung
            string.Empty) // Kein Full Content
        {
            Tags = Tags.Take(3).ToList(), // Nur wichtigste Tags
            EmotionalContext = EmotionalContext.Take(2).ToList()
        };
    }

    public Dictionary<string, object> GetMemoryAnalytics()
    {
        return new Dictionary<string, object>
        {
            { "Id", Id },
            { "Title", Title },
            { "MemoryType", MemoryType.ToString() },
            { "Importance", Importance },
            { "ImportanceLevel", ImportanceLevel.ToString() },
            { "AccessCount", AccessCount },
            { "MemoryStrength", GetMemoryStrength() },
            { "IsConsolidated", IsConsolidated },
            { "DaysSinceCreated", (DateTime.UtcNow - CreatedAt).TotalDays },
            { "DaysSinceOccurred", (DateTime.UtcNow - OccurredAt).TotalDays },
            { "TagCount", Tags.Count },
            { "LinkedMemoriesCount", LinkedMemoryIds.Count },
            { "DecayResistance", DecayResistance },
            { "ShouldBePreserved", ShouldBePreserved() }
        };
    }

    private void UpdateImportanceLevel()
    {
        ImportanceLevel = Importance switch
        {
            >= 9 => MemoryImportance.Critical,
            >= 7 => MemoryImportance.High,
            >= 5 => MemoryImportance.Medium,
            >= 3 => MemoryImportance.Low,
            _ => MemoryImportance.Trivial
        };
    }

    public bool Equals(CharacterMemory? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id.Equals(other.Id);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as CharacterMemory);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public override string ToString()
    {
        return $"{Title} (Wichtigkeit: {Importance}/10, {MemoryType})";
    }

    public static bool operator ==(CharacterMemory? left, CharacterMemory? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(CharacterMemory? left, CharacterMemory? right)
    {
        return !Equals(left, right);
    }

    // Factory Methods für häufige Memory-Typen
    public static CharacterMemory CreateStoryMemory(string title, string summary, int importance, Guid storyId)
    {
        return new CharacterMemory(title, summary, MemoryType.Experience, importance, DateTime.UtcNow, storyId);
    }

    public static CharacterMemory CreateLearningMemory(string title, string summary, int importance = 6)
    {
        return new CharacterMemory(title, summary, MemoryType.Learning, importance);
    }

    public static CharacterMemory CreateEmotionalMemory(string title, string summary, string emotion, int importance = 7)
    {
        var memory = new CharacterMemory(title, summary, MemoryType.Emotional, importance);
        memory.AddEmotionalContext(emotion);
        return memory;
    }

    public static CharacterMemory CreateAchievementMemory(string title, string summary, int importance = 8)
    {
        return new CharacterMemory(title, summary, MemoryType.Achievement, importance);
    }

    public static CharacterMemory CreateRelationshipMemory(string title, string summary, string characterName, int importance = 6)
    {
        var memory = new CharacterMemory(title, summary, MemoryType.Relationship, importance);
        memory.AddAssociatedCharacter(characterName);
        return memory;
    }
}