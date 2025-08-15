using Avatales.Shared.Extensions;
using Avatales.Shared.Models;

namespace Avatales.Domain.Characters.ValueObjects;

/// <summary>
/// CharacterTrait Value Object - Entwicklungsfähige Charaktereigenschaft
/// Verwaltet die Evolution von Traits durch Story-Erfahrungen
/// </summary>
public class CharacterTrait : IEquatable<CharacterTrait>
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public CharacterTraitType TraitType { get; private set; }
    public int CurrentValue { get; private set; } // 1-10 Skala
    public int BaseValue { get; private set; } // Ursprungswert aus DNA
    public int MaxValue { get; private set; } = 10; // Maximaler möglicher Wert
    public float ExperiencePoints { get; private set; } = 0f; // Erfahrungspunkte für dieses Trait
    public DateTime LastModified { get; private set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // Entwicklungs-Tracking
    public List<TraitEvolution> EvolutionHistory { get; private set; } = new();
    public Dictionary<string, float> InfluenceFactors { get; private set; } = new(); // Was beeinflusst dieses Trait
    public List<string> RecentExperiences { get; private set; } = new(); // Letzte 10 Erfahrungen

    // Stabilität und Wachstum
    public float StabilityFactor { get; private set; } = 1.0f; // Wie stabil ist das Trait (0.5-2.0)
    public float GrowthRate { get; private set; } = 1.0f; // Wie schnell entwickelt es sich (0.5-2.0)
    public int TimesReinforced { get; private set; } = 0; // Wie oft wurde es verstärkt
    public int TimesChallenged { get; private set; } = 0; // Wie oft wurde es herausgefordert

    protected CharacterTrait() { } // For EF Core

    public CharacterTrait(CharacterTraitType traitType, int baseValue, float stabilityFactor = 1.0f)
    {
        if (baseValue < 1 || baseValue > 10)
            throw new ArgumentException("Base value must be between 1 and 10", nameof(baseValue));

        if (stabilityFactor < 0.5f || stabilityFactor > 2.0f)
            throw new ArgumentException("Stability factor must be between 0.5 and 2.0", nameof(stabilityFactor));

        TraitType = traitType;
        BaseValue = baseValue;
        CurrentValue = baseValue;
        StabilityFactor = stabilityFactor;
        GrowthRate = CalculateInitialGrowthRate(traitType, baseValue);

        // Initialisiere Influence Factors
        InfluenceFactors = GetDefaultInfluenceFactors(traitType);

        RecordEvolution(TraitEvolutionType.Created, baseValue, "Trait created from DNA");
    }

    /// <summary>
    /// Fügt Erfahrung zu dem Trait hinzu basierend auf Story-Ereignissen
    /// </summary>
    public TraitChangeResult AddExperience(
        float experiencePoints,
        string experienceDescription,
        TraitInfluenceContext context)
    {
        if (experiencePoints <= 0)
            throw new ArgumentException("Experience points must be positive", nameof(experiencePoints));

        var previousValue = CurrentValue;
        ExperiencePoints += experiencePoints * GrowthRate;

        // Berechne neuen Trait-Wert
        var newValue = CalculateNewValue();
        var valueChanged = newValue != CurrentValue;

        if (valueChanged)
        {
            CurrentValue = newValue;
            RecordEvolution(TraitEvolutionType.Increased, newValue, experienceDescription);
            TimesReinforced++;
        }

        // Aktualisiere Influence Factors
        UpdateInfluenceFactors(context);

        // Füge zur Recent Experiences hinzu
        AddRecentExperience(experienceDescription);

        // Erhöhe Stabilität durch Verstärkung
        if (valueChanged)
        {
            StabilityFactor = Math.Min(2.0f, StabilityFactor + 0.05f);
        }

        LastModified = DateTime.UtcNow;

        return new TraitChangeResult(
            PreviousValue: previousValue,
            NewValue: CurrentValue,
            ExperienceGained: experiencePoints,
            ValueChanged: valueChanged,
            Message: GenerateChangeMessage(previousValue, CurrentValue, experienceDescription)
        );
    }

    /// <summary>
    /// Verringert Trait-Wert bei negativen Erfahrungen (selten und vorsichtig)
    /// </summary>
    public TraitChangeResult ChallengeTraitValue(
        float challengeIntensity,
        string challengeDescription,
        TraitInfluenceContext context)
    {
        if (challengeIntensity <= 0 || challengeIntensity > 1.0f)
            throw new ArgumentException("Challenge intensity must be between 0 and 1", nameof(challengeIntensity));

        var previousValue = CurrentValue;

        // Nur bei sehr niedrigen Stability-Faktoren und starken Herausforderungen
        var reductionChance = (1.0f - StabilityFactor) * challengeIntensity;

        if (reductionChance > 0.3f && CurrentValue > BaseValue) // Nicht unter Basis-Wert fallen
        {
            var reduction = Math.Max(1, (int)(challengeIntensity * 2));
            CurrentValue = Math.Max(BaseValue, CurrentValue - reduction);

            RecordEvolution(TraitEvolutionType.Challenged, CurrentValue, challengeDescription);
            TimesChallenged++;

            // Reduziere Growth Rate temporär
            GrowthRate = Math.Max(0.5f, GrowthRate - 0.1f);
        }

        UpdateInfluenceFactors(context);
        AddRecentExperience($"Herausgefordert: {challengeDescription}");
        LastModified = DateTime.UtcNow;

        return new TraitChangeResult(
            PreviousValue: previousValue,
            NewValue: CurrentValue,
            ExperienceGained: 0,
            ValueChanged: previousValue != CurrentValue,
            Message: GenerateChangeMessage(previousValue, CurrentValue, challengeDescription)
        );
    }

    /// <summary>
    /// Verstärkt ein Trait durch positive Verstärkung
    /// </summary>
    public TraitChangeResult ReinforcePositively(
        string reinforcementDescription,
        float multiplier = 1.5f)
    {
        var bonusExperience = CalculateBonusExperience() * multiplier;
        var context = new TraitInfluenceContext(
            StoryGenre: "Positive Reinforcement",
            EmotionalTone: "Positive",
            LearningContext: new[] { "Reinforcement", "Success" }
        );

        return AddExperience(bonusExperience, $"Verstärkt: {reinforcementDescription}", context);
    }

    /// <summary>
    /// Berechnet wie viel Erfahrung für den nächsten Level benötigt wird
    /// </summary>
    public float GetExperienceNeededForNextLevel()
    {
        if (CurrentValue >= MaxValue) return 0;

        return CalculateExperienceNeededForValue(CurrentValue + 1) - ExperiencePoints;
    }

    /// <summary>
    /// Gibt eine menschenlesbare Beschreibung des aktuellen Trait-Levels
    /// </summary>
    public string GetLevelDescription()
    {
        return CurrentValue switch
        {
            <= 2 => GetLevelDescriptor("Anfänger"),
            <= 4 => GetLevelDescriptor("Entwickelnd"),
            <= 6 => GetLevelDescriptor("Gut entwickelt"),
            <= 8 => GetLevelDescriptor("Stark ausgeprägt"),
            <= 9 => GetLevelDescriptor("Sehr stark"),
            10 => GetLevelDescriptor("Außergewöhnlich")
        };
    }

    /// <summary>
    /// Berechnet den Einfluss verschiedener Faktoren auf die Trait-Entwicklung
    /// </summary>
    public Dictionary<string, float> GetCurrentInfluenceMap()
    {
        var influence = new Dictionary<string, float>(InfluenceFactors);

        // Füge dynamische Faktoren hinzu
        influence["Stabilität"] = StabilityFactor;
        influence["Wachstumsrate"] = GrowthRate;
        influence["Verstärkungen"] = Math.Min(2.0f, TimesReinforced / 10.0f);
        influence["Herausforderungen"] = Math.Max(0.5f, 1.0f - (TimesChallenged / 20.0f));

        return influence;
    }

    /// <summary>
    /// Prüft ob das Trait für besondere Erkennung/Belohnung bereit ist
    /// </summary>
    public bool IsReadyForRecognition()
    {
        return CurrentValue >= 8 && TimesReinforced >= 5 && StabilityFactor >= 1.5f;
    }

    /// <summary>
    /// Berechnet Trait-Synergie mit anderen Traits
    /// </summary>
    public float CalculateSynergyWith(CharacterTrait otherTrait)
    {
        var synergies = GetTraitSynergies();

        if (synergies.ContainsKey(otherTrait.TraitType))
        {
            var baseSynergy = synergies[otherTrait.TraitType];
            var valueFactor = Math.Min(CurrentValue, otherTrait.CurrentValue) / 10.0f;
            return baseSynergy * valueFactor;
        }

        return 0f;
    }

    private int CalculateNewValue()
    {
        // Exponentiell ansteigende Anforderungen für höhere Levels
        for (int value = MaxValue; value >= 1; value--)
        {
            if (ExperiencePoints >= CalculateExperienceNeededForValue(value))
            {
                return value;
            }
        }
        return 1;
    }

    private float CalculateExperienceNeededForValue(int value)
    {
        // Exponentieller Anstieg: Level 1=0, Level 2=10, Level 3=25, Level 4=50, etc.
        return value == 1 ? 0 : (float)(Math.Pow(value - 1, 2.2) * 10);
    }

    private float CalculateInitialGrowthRate(CharacterTraitType traitType, int baseValue)
    {
        // Traits mit niedrigerem Basis-Wert wachsen schneller
        var baseAdjustment = (11 - baseValue) / 10.0f; // 0.2 - 1.0

        // Trait-spezifische Anpassungen
        var traitModifier = traitType switch
        {
            CharacterTraitType.Curiosity => 1.2f,      // Neugier entwickelt sich schnell
            CharacterTraitType.Creativity => 1.1f,     // Kreativität auch
            CharacterTraitType.Wisdom => 0.8f,         // Weisheit braucht Zeit
            CharacterTraitType.Intelligence => 0.9f,   // Intelligenz entwickelt sich langsamer
            _ => 1.0f
        };

        return Math.Max(0.5f, Math.Min(2.0f, baseAdjustment * traitModifier));
    }

    private Dictionary<string, float> GetDefaultInfluenceFactors(CharacterTraitType traitType)
    {
        var baseFactors = new Dictionary<string, float>
        {
            ["Stories"] = 1.0f,
            ["Social_Interaction"] = 0.8f,
            ["Learning_Activities"] = 0.9f,
            ["Challenges"] = 0.7f,
            ["Success"] = 1.1f,
            ["Positive_Feedback"] = 1.2f
        };

        // Trait-spezifische Anpassungen
        switch (traitType)
        {
            case CharacterTraitType.Empathy:
                baseFactors["Social_Interaction"] = 1.5f;
                baseFactors["Emotional_Stories"] = 1.3f;
                break;
            case CharacterTraitType.Courage:
                baseFactors["Challenges"] = 1.4f;
                baseFactors["Adventure_Stories"] = 1.2f;
                break;
            case CharacterTraitType.Intelligence:
                baseFactors["Learning_Activities"] = 1.4f;
                baseFactors["Problem_Solving"] = 1.3f;
                break;
        }

        return baseFactors;
    }

    private Dictionary<CharacterTraitType, float> GetTraitSynergies()
    {
        return TraitType switch
        {
            CharacterTraitType.Courage => new Dictionary<CharacterTraitType, float>
            {
                [CharacterTraitType.Determination] = 0.8f,
                [CharacterTraitType.Confidence] = 0.7f
            },
            CharacterTraitType.Empathy => new Dictionary<CharacterTraitType, float>
            {
                [CharacterTraitType.Kindness] = 0.9f,
                [CharacterTraitType.Wisdom] = 0.6f
            },
            CharacterTraitType.Creativity => new Dictionary<CharacterTraitType, float>
            {
                [CharacterTraitType.Curiosity] = 0.8f,
                [CharacterTraitType.Intelligence] = 0.5f
            },
            _ => new Dictionary<CharacterTraitType, float>()
        };
    }

    private void UpdateInfluenceFactors(TraitInfluenceContext context)
    {
        if (!string.IsNullOrEmpty(context.StoryGenre))
        {
            var genreKey = $"Genre_{context.StoryGenre}";
            InfluenceFactors[genreKey] = InfluenceFactors.GetValueOrDefault(genreKey, 0.5f) + 0.1f;
        }

        foreach (var learningContext in context.LearningContext)
        {
            var contextKey = $"Context_{learningContext}";
            InfluenceFactors[contextKey] = InfluenceFactors.GetValueOrDefault(contextKey, 0.5f) + 0.05f;
        }
    }

    private void AddRecentExperience(string experience)
    {
        RecentExperiences.Insert(0, experience);

        // Behalte nur die letzten 10 Erfahrungen
        if (RecentExperiences.Count > 10)
        {
            RecentExperiences.RemoveAt(10);
        }
    }

    private float CalculateBonusExperience()
    {
        var baseExperience = 5.0f; // Basis-Erfahrung für positive Verstärkung
        var levelModifier = CurrentValue / 10.0f; // Höhere Level brauchen mehr Erfahrung
        return baseExperience * (1 + levelModifier);
    }

    private void RecordEvolution(TraitEvolutionType evolutionType, int newValue, string description)
    {
        var evolution = new TraitEvolution(
            DateTime.UtcNow,
            evolutionType,
            newValue,
            description,
            ExperiencePoints
        );

        EvolutionHistory.Add(evolution);

        // Behalte nur die letzten 50 Evolutionen
        if (EvolutionHistory.Count > 50)
        {
            EvolutionHistory.RemoveAt(0);
        }
    }

    private string GetLevelDescriptor(string baseDescriptor)
    {
        return $"{TraitType.ToDisplayString()}: {baseDescriptor}";
    }

    private string GenerateChangeMessage(int previousValue, int newValue, string experience)
    {
        if (newValue > previousValue)
        {
            return $"{TraitType.ToDisplayString()} ist durch '{experience}' von Level {previousValue} auf {newValue} gestiegen!";
        }
        else if (newValue < previousValue)
        {
            return $"{TraitType.ToDisplayString()} wurde durch '{experience}' herausgefordert (Level {previousValue} → {newValue})";
        }
        else
        {
            return $"{TraitType.ToDisplayString()} wurde durch '{experience}' verstärkt";
        }
    }

    // Equality Implementation
    public bool Equals(CharacterTrait? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id.Equals(other.Id);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as CharacterTrait);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(CharacterTrait? left, CharacterTrait? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(CharacterTrait? left, CharacterTrait? right)
    {
        return !Equals(left, right);
    }
}

/// <summary>
/// Ergebnis einer Trait-Änderung
/// </summary>
public record TraitChangeResult(
    int PreviousValue,
    int NewValue,
    float ExperienceGained,
    bool ValueChanged,
    string Message
);

/// <summary>
/// Kontext für Trait-Beeinflussung
/// </summary>
public record TraitInfluenceContext(
    string StoryGenre,
    string EmotionalTone,
    IEnumerable<string> LearningContext
);

/// <summary>
/// Evolution-Eintrag für Trait-Entwicklung
/// </summary>
public record TraitEvolution(
    DateTime Timestamp,
    TraitEvolutionType Type,
    int NewValue,
    string Description,
    float ExperiencePointsAtTime
);

/// <summary>
/// Typen von Trait-Evolutionen
/// </summary>
public enum TraitEvolutionType
{
    Created = 1,
    Increased = 2,
    Challenged = 3,
    Reinforced = 4,
    Reset = 5
}