using Avatales.Shared.Models;

namespace Avatales.Domain.Characters.ValueObjects;

/// <summary>
/// CharacterSnapshot Value Object - Momentaufnahme eines Charakterzustands
/// Verwendet für AI-Kontext und Charakter-Historie
/// </summary>
public class CharacterSnapshot : IEquatable<CharacterSnapshot>
{
    public Guid CharacterId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int Level { get; private set; }
    public int StoriesExperienced { get; private set; }
    public Dictionary<CharacterTraitType, int> Traits { get; private set; } = new();
    public List<string> RecentMemorySummaries { get; private set; } = new();
    public DateTime SnapshotCreatedAt { get; private set; }
    public string PersonalityDescriptor { get; private set; } = string.Empty;
    public List<string> DominantTraits { get; private set; } = new();
    public List<string> PreferredStoryThemes { get; private set; } = new();

    // Erweiterte Kontext-Informationen für AI
    public string CurrentMood { get; private set; } = string.Empty;
    public List<string> RecentLearnings { get; private set; } = new();
    public Dictionary<string, object> AdditionalContext { get; private set; } = new();

    protected CharacterSnapshot() { } // For EF Core

    public CharacterSnapshot(
        Guid characterId,
        string name,
        string description,
        int level,
        int storiesExperienced,
        Dictionary<CharacterTraitType, int> traits,
        List<string> recentMemorySummaries,
        DateTime snapshotCreatedAt)
    {
        CharacterId = characterId;
        Name = name?.Trim() ?? throw new ArgumentException("Name cannot be empty");
        Description = description?.Trim() ?? string.Empty;
        Level = Math.Max(1, level);
        StoriesExperienced = Math.Max(0, storiesExperienced);
        Traits = traits?.ToDictionary(kvp => kvp.Key, kvp => Math.Clamp(kvp.Value, 1, 10)) ?? new Dictionary<CharacterTraitType, int>();
        RecentMemorySummaries = recentMemorySummaries?.Take(5).ToList() ?? new List<string>();
        SnapshotCreatedAt = snapshotCreatedAt;

        // Berechne abgeleitete Eigenschaften
        CalculatePersonalityDescriptor();
        CalculateDominantTraits();
        CalculatePreferredStoryThemes();
        DetermineMood();
    }

    public string GetAIContextString()
    {
        var context = $"Charakter: {Name}\n";
        context += $"Beschreibung: {Description}\n";
        context += $"Level: {Level}, Geschichten erlebt: {StoriesExperienced}\n\n";

        // Traits
        context += "Charaktereigenschaften:\n";
        foreach (var trait in Traits.OrderByDescending(t => t.Value))
        {
            var traitName = GetTraitName(trait.Key);
            var intensity = GetTraitIntensity(trait.Value);
            context += $"- {traitName}: {intensity} ({trait.Value}/10)\n";
        }

        // Persönlichkeit
        if (!string.IsNullOrEmpty(PersonalityDescriptor))
        {
            context += $"\nPersönlichkeit: {PersonalityDescriptor}\n";
        }

        // Dominante Traits
        if (DominantTraits.Any())
        {
            context += $"Dominante Eigenschaften: {string.Join(", ", DominantTraits)}\n";
        }

        // Stimmung
        if (!string.IsNullOrEmpty(CurrentMood))
        {
            context += $"Aktuelle Stimmung: {CurrentMood}\n";
        }

        // Bevorzugte Themen
        if (PreferredStoryThemes.Any())
        {
            context += $"Bevorzugte Story-Themen: {string.Join(", ", PreferredStoryThemes)}\n";
        }

        // Kürzliche Memories
        if (RecentMemorySummaries.Any())
        {
            context += "\nKürzliche Erinnerungen:\n";
            foreach (var memory in RecentMemorySummaries.Take(3))
            {
                context += $"- {memory}\n";
            }
        }

        // Lernfortschritte
        if (RecentLearnings.Any())
        {
            context += $"\nKürzliche Lernfortschritte: {string.Join(", ", RecentLearnings)}\n";
        }

        return context.Trim();
    }

    public string GetShortDescription()
    {
        var dominantTrait = DominantTraits.FirstOrDefault() ?? "ausgewogen";
        return $"{Name} (Level {Level}) - {dominantTrait}, {StoriesExperienced} Geschichten erlebt";
    }

    public Dictionary<string, object> GetAnalyticsData()
    {
        return new Dictionary<string, object>
        {
            { "CharacterId", CharacterId },
            { "Name", Name },
            { "Level", Level },
            { "StoriesExperienced", StoriesExperienced },
            { "SnapshotAge", (DateTime.UtcNow - SnapshotCreatedAt).TotalHours },
            { "DominantTraits", DominantTraits },
            { "TraitCount", Traits.Count },
            { "HighestTraitValue", Traits.Values.DefaultIfEmpty(0).Max() },
            { "LowestTraitValue", Traits.Values.DefaultIfEmpty(0).Min() },
            { "AverageTraitValue", Traits.Values.DefaultIfEmpty(0).Average() },
            { "MemoryCount", RecentMemorySummaries.Count },
            { "PersonalityType", PersonalityDescriptor },
            { "PreferredThemes", PreferredStoryThemes }
        };
    }

    public bool IsRecentSnapshot(int hours = 24)
    {
        return (DateTime.UtcNow - SnapshotCreatedAt).TotalHours <= hours;
    }

    public bool HasHighTrait(CharacterTraitType traitType, int threshold = 7)
    {
        return Traits.ContainsKey(traitType) && Traits[traitType] >= threshold;
    }

    public bool HasLowTrait(CharacterTraitType traitType, int threshold = 4)
    {
        return Traits.ContainsKey(traitType) && Traits[traitType] <= threshold;
    }

    public int GetTraitValue(CharacterTraitType traitType)
    {
        return Traits.TryGetValue(traitType, out var value) ? value : 5;
    }

    public bool IsExperiencedCharacter()
    {
        return StoriesExperienced >= 10 && Level >= 5;
    }

    public bool IsBeginnerCharacter()
    {
        return StoriesExperienced <= 3 && Level <= 2;
    }

    public List<CharacterTraitType> GetStrongTraits(int threshold = 7)
    {
        return Traits.Where(t => t.Value >= threshold).Select(t => t.Key).ToList();
    }

    public List<CharacterTraitType> GetWeakTraits(int threshold = 4)
    {
        return Traits.Where(t => t.Value <= threshold).Select(t => t.Key).ToList();
    }

    public string GetRecommendedStoryDirection()
    {
        var strongTraits = GetStrongTraits();
        var weakTraits = GetWeakTraits();

        if (strongTraits.Contains(CharacterTraitType.Courage) && strongTraits.Contains(CharacterTraitType.Determination))
        {
            return "Abenteuerliche Herausforderungen wo Mut und Entschlossenheit gefragt sind";
        }

        if (strongTraits.Contains(CharacterTraitType.Kindness) && strongTraits.Contains(CharacterTraitType.Empathy))
        {
            return "Soziale Geschichten über Freundschaft und anderen helfen";
        }

        if (strongTraits.Contains(CharacterTraitType.Curiosity) && strongTraits.Contains(CharacterTraitType.Intelligence))
        {
            return "Rätsel und Entdeckungsreisen mit Lernmöglichkeiten";
        }

        if (strongTraits.Contains(CharacterTraitType.Creativity))
        {
            return "Kreative Projekte und künstlerische Herausforderungen";
        }

        if (weakTraits.Any())
        {
            var weakTrait = weakTraits.First();
            return $"Geschichten die {GetTraitName(weakTrait)} entwickeln und stärken";
        }

        return "Vielseitige Abenteuer die verschiedene Eigenschaften ansprechen";
    }

    private void CalculatePersonalityDescriptor()
    {
        if (!Traits.Any()) return;

        var sortedTraits = Traits.OrderByDescending(t => t.Value).Take(3).ToList();
        var descriptors = new List<string>();

        foreach (var trait in sortedTraits)
        {
            if (trait.Value >= 7)
            {
                descriptors.Add(GetTraitAdjective(trait.Key));
            }
        }

        PersonalityDescriptor = descriptors.Any() 
            ? string.Join(", ", descriptors)
            : "Ausgewogene Persönlichkeit";
    }

    private void CalculateDominantTraits()
    {
        DominantTraits = Traits
            .Where(t => t.Value >= 7)
            .OrderByDescending(t => t.Value)
            .Take(3)
            .Select(t => GetTraitName(t.Key))
            .ToList();
    }

    private void CalculatePreferredStoryThemes()
    {
        var themes = new List<string>();

        foreach (var trait in Traits.Where(t => t.Value >= 7))
        {
            themes.AddRange(GetThemesForTrait(trait.Key));
        }

        PreferredStoryThemes = themes.Distinct().Take(5).ToList();
    }

    private void DetermineMood()
    {
        var averageTraitValue = Traits.Values.DefaultIfEmpty(5).Average();
        var hasHighOptimism = HasHighTrait(CharacterTraitType.Optimism, 7);
        var hasHighHumor = HasHighTrait(CharacterTraitType.Humor, 7);

        CurrentMood = (averageTraitValue, hasHighOptimism, hasHighHumor) switch
        {
            (>= 8, true, true) => "Sehr fröhlich und optimistisch",
            (>= 7, true, _) => "Optimistisch und positiv",
            (>= 6, _, true) => "Gut gelaunt und humorvoll",
            (>= 6, _, _) => "Ausgeglichen und zufrieden",
            (>= 4, _, _) => "Ruhig und nachdenklich",
            _ => "Bedacht und vorsichtig"
        };
    }

    private static string GetTraitName(CharacterTraitType traitType)
    {
        return traitType switch
        {
            CharacterTraitType.Courage => "Mut",
            CharacterTraitType.Curiosity => "Neugier",
            CharacterTraitType.Kindness => "Freundlichkeit",
            CharacterTraitType.Creativity => "Kreativität",
            CharacterTraitType.Intelligence => "Intelligenz",
            CharacterTraitType.Humor => "Humor",
            CharacterTraitType.Wisdom => "Weisheit",
            CharacterTraitType.Empathy => "Empathie",
            CharacterTraitType.Determination => "Entschlossenheit",
            CharacterTraitType.Optimism => "Optimismus",
            _ => traitType.ToString()
        };
    }

    private static string GetTraitAdjective(CharacterTraitType traitType)
    {
        return traitType switch
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
    }

    private static string GetTraitIntensity(int value)
    {
        return value switch
        {
            10 => "Meisterhaft",
            9 => "Außergewöhnlich",
            8 => "Sehr stark",
            7 => "Stark",
            6 => "Gut",
            5 => "Durchschnittlich",
            4 => "Schwach",
            3 => "Gering",
            2 => "Sehr schwach",
            1 => "Minimal",
            _ => "Unbekannt"
        };
    }

    private static List<string> GetThemesForTrait(CharacterTraitType traitType)
    {
        return traitType switch
        {
            CharacterTraitType.Courage => new List<string> { "Abenteuer", "Herausforderungen", "Gefahren überwinden" },
            CharacterTraitType.Curiosity => new List<string> { "Entdeckungen", "Mysterien", "Lernabenteuer" },
            CharacterTraitType.Kindness => new List<string> { "Freundschaft", "Helfen", "Mitgefühl" },
            CharacterTraitType.Creativity => new List<string> { "Kunst", "Erfindungen", "Kreative Lösungen" },
            CharacterTraitType.Intelligence => new List<string> { "Rätsel", "Strategien", "Wissen" },
            CharacterTraitType.Humor => new List<string> { "Lustige Situationen", "Schabernack", "Fröhliche Abenteuer" },
            CharacterTraitType.Wisdom => new List<string> { "Lebenslektionen", "Entscheidungen", "Führung" },
            CharacterTraitType.Empathy => new List<string> { "Emotionen", "Verstehen", "Konflikte lösen" },
            CharacterTraitType.Determination => new List<string> { "Ziele erreichen", "Ausdauer", "Durchhalten" },
            CharacterTraitType.Optimism => new List<string> { "Hoffnung", "Positive Wendungen", "Aufmunterung" },
            _ => new List<string> { "Vielseitige Abenteuer" }
        };
    }

    public bool Equals(CharacterSnapshot? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return CharacterId.Equals(other.CharacterId) && SnapshotCreatedAt.Equals(other.SnapshotCreatedAt);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as CharacterSnapshot);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(CharacterId, SnapshotCreatedAt);
    }

    public override string ToString()
    {
        return $"{Name} Snapshot (Level {Level}, {StoriesExperienced} Geschichten)";
    }
}