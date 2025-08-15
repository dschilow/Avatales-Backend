using Avatales.Shared.Models;
using Avatales.Shared.Extensions;

namespace Avatales.Domain.Stories.ValueObjects;

/// <summary>
/// StoryScene Value Object - Einzelne Szene innerhalb einer Geschichte
/// Repräsentiert einen Abschnitt der Geschichte mit spezifischen Eigenschaften
/// </summary>
public class StoryScene : IEquatable<StoryScene>
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public int SceneNumber { get; private set; } // Reihenfolge in der Geschichte
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public string ImagePrompt { get; private set; } = string.Empty; // Für AI-Bildgenerierung
    public string? ImageUrl { get; private set; }
    public int WordCount { get; private set; }
    public int EstimatedReadingTimeSeconds { get; private set; }
    public AvatarEmotion PrimaryEmotion { get; private set; } = AvatarEmotion.Neutral;
    public string EmotionalTone { get; private set; } = "Neutral"; // "Happy", "Exciting", "Thoughtful", etc.

    // Pädagogische Eigenschaften
    public List<string> KeyWords { get; private set; } = new(); // Wichtige Vokabeln in dieser Szene
    public List<string> LearningMoments { get; private set; } = new(); // Was wird hier gelernt
    public LearningDifficulty DifficultyLevel { get; private set; } = LearningDifficulty.Elementary;

    // Interaktive Elemente
    public List<SceneChoice> Choices { get; private set; } = new(); // Für interaktive Geschichten
    public Dictionary<string, object> InteractiveElements { get; private set; } = new(); // Spiele, Rätsel, etc.
    public bool HasInteractiveElement { get; private set; } = false;

    // Charakter-Entwicklung
    public Dictionary<CharacterTraitType, float> TraitInfluences { get; private set; } = new(); // Welche Traits werden beeinflusst
    public List<string> CharacterActions { get; private set; } = new(); // Was tut der Charakter
    public List<string> CharacterLearnings { get; private set; } = new(); // Was lernt der Charakter

    // Metadaten
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public bool IsClimax { get; private set; } = false; // Ist das der Höhepunkt der Geschichte?
    public bool RequiresParentalGuidance { get; private set; } = false;

    protected StoryScene() { } // For EF Core

    public StoryScene(
        int sceneNumber,
        string title,
        string content,
        AvatarEmotion primaryEmotion = AvatarEmotion.Neutral,
        LearningDifficulty difficultyLevel = LearningDifficulty.Elementary)
    {
        if (sceneNumber <= 0)
            throw new ArgumentException("Scene number must be positive", nameof(sceneNumber));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Scene title cannot be empty", nameof(title));

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Scene content cannot be empty", nameof(content));

        if (!title.IsChildFriendly())
            throw new ArgumentException("Scene title contains inappropriate content", nameof(title));

        if (!content.IsChildFriendly())
            throw new ArgumentException("Scene content contains inappropriate content", nameof(content));

        SceneNumber = sceneNumber;
        Title = title.Trim();
        Content = content.Trim();
        PrimaryEmotion = primaryEmotion;
        DifficultyLevel = difficultyLevel;

        // Berechne abgeleitete Eigenschaften
        WordCount = CalculateWordCount(content);
        EstimatedReadingTimeSeconds = CalculateReadingTime(WordCount, difficultyLevel);
        EmotionalTone = DetermineEmotionalTone(primaryEmotion);

        // Extrahiere Lern-Elemente
        ExtractKeyWords();
        AnalyzeCharacterActions();
    }

    /// <summary>
    /// Fügt interaktive Wahlmöglichkeiten zur Szene hinzu
    /// </summary>
    public void AddChoices(IEnumerable<SceneChoice> choices)
    {
        if (choices?.Any() == true)
        {
            Choices.Clear();
            Choices.AddRange(choices.Take(4)); // Maximal 4 Wahlmöglichkeiten
            HasInteractiveElement = true;
        }
    }

    /// <summary>
    /// Fügt ein interaktives Element hinzu (Spiel, Rätsel, etc.)
    /// </summary>
    public void AddInteractiveElement(string elementType, object elementData)
    {
        if (string.IsNullOrWhiteSpace(elementType))
            throw new ArgumentException("Element type cannot be empty", nameof(elementType));

        InteractiveElements[elementType] = elementData;
        HasInteractiveElement = true;
    }

    /// <summary>
    /// Setzt das Bild für die Szene
    /// </summary>
    public void SetImage(string imageUrl, string? imagePrompt = null)
    {
        ImageUrl = imageUrl?.Trim();
        if (!string.IsNullOrWhiteSpace(imagePrompt))
        {
            ImagePrompt = imagePrompt.Trim();
        }
    }

    /// <summary>
    /// Fügt Trait-Einflüsse hinzu
    /// </summary>
    public void AddTraitInfluence(CharacterTraitType traitType, float influence)
    {
        if (influence < 0 || influence > 1.0f)
            throw new ArgumentException("Influence must be between 0 and 1", nameof(influence));

        TraitInfluences[traitType] = influence;
    }

    /// <summary>
    /// Markiert die Szene als Höhepunkt
    /// </summary>
    public void MarkAsClimax()
    {
        IsClimax = true;
    }

    /// <summary>
    /// Gibt eine kinderfreundliche Zusammenfassung der Szene
    /// </summary>
    public string GetChildFriendlySummary(int maxLength = 100)
    {
        var summary = Content.Length <= maxLength
            ? Content
            : Content.Substring(0, maxLength - 3) + "...";

        return summary.SanitizeForChildren();
    }

    /// <summary>
    /// Berechnet die Eignung für ein bestimmtes Alter
    /// </summary>
    public bool IsSuitableForAge(int childAge)
    {
        var ageRequirement = DifficultyLevel switch
        {
            LearningDifficulty.Beginner => 3,
            LearningDifficulty.Elementary => 6,
            LearningDifficulty.Intermediate => 9,
            LearningDifficulty.Advanced => 12,
            LearningDifficulty.Expert => 15,
            _ => 6
        };

        return childAge >= ageRequirement && !RequiresParentalGuidance;
    }

    private int CalculateWordCount(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 0;

        return content.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private int CalculateReadingTime(int wordCount, LearningDifficulty difficulty)
    {
        // Wörter pro Minute basierend auf Schwierigkeit
        var wordsPerMinute = difficulty switch
        {
            LearningDifficulty.Beginner => 50,      // Langsam für Anfänger
            LearningDifficulty.Elementary => 80,    // Normal für Grundschule
            LearningDifficulty.Intermediate => 120, // Schneller für Mittelstufe
            LearningDifficulty.Advanced => 150,     // Noch schneller
            LearningDifficulty.Expert => 180,       // Schnellstes Tempo
            _ => 100
        };

        return (int)Math.Ceiling((double)wordCount / wordsPerMinute * 60); // Sekunden
    }

    private string DetermineEmotionalTone(AvatarEmotion emotion)
    {
        return emotion switch
        {
            AvatarEmotion.Happy => "Fröhlich",
            AvatarEmotion.Excited => "Aufregend",
            AvatarEmotion.Curious => "Neugierig",
            AvatarEmotion.Confident => "Selbstbewusst",
            AvatarEmotion.Thoughtful => "Nachdenklich",
            AvatarEmotion.Surprised => "Überraschend",
            AvatarEmotion.Concerned => "Besorgt",
            AvatarEmotion.Proud => "Stolz",
            AvatarEmotion.Determined => "Entschlossen",
            AvatarEmotion.Playful => "Verspielt",
            AvatarEmotion.Wise => "Weise",
            _ => "Neutral"
        };
    }

    private void ExtractKeyWords()
    {
        // Einfache Keyword-Extraktion (kann später durch NLP verbessert werden)
        var words = Content.Split(new[] { ' ', '\t', '\n', '\r', '.', ',', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 4) // Nur längere Wörter
            .Where(w => w.IsChildFriendly())
            .GroupBy(w => w.ToLower())
            .Where(g => g.Count() == 1) // Einzigartige Wörter
            .Select(g => g.Key)
            .Take(5)
            .ToList();

        KeyWords.AddRange(words);
    }

    private void AnalyzeCharacterActions()
    {
        // Einfache Aktionsanalyse basierend auf häufigen Verben
        var actionVerbs = new[] { "geht", "rennt", "springt", "klettert", "hilft", "rettet", "entdeckt", "findet", "löst", "baut", "erschafft", "lernt" };

        foreach (var verb in actionVerbs)
        {
            if (Content.ToLower().Contains(verb))
            {
                CharacterActions.AddIfNotExists($"Der Charakter {verb}");
            }
        }
    }

    // Equality Implementation
    public bool Equals(StoryScene? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id.Equals(other.Id);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as StoryScene);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(StoryScene? left, StoryScene? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(StoryScene? left, StoryScene? right)
    {
        return !Equals(left, right);
    }
}

/// <summary>
/// Wahlmöglichkeit in einer interaktiven Szene
/// </summary>
public class SceneChoice : IEquatable<SceneChoice>
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Text { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Dictionary<CharacterTraitType, float> TraitInfluences { get; private set; } = new();
    public string? NextSceneId { get; private set; } // Für verzweigte Geschichten
    public bool IsOptimal { get; private set; } = false; // Beste Wahl pädagogisch

    public SceneChoice(string text, string description = "", bool isOptimal = false)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Choice text cannot be empty", nameof(text));

        if (!text.IsChildFriendly())
            throw new ArgumentException("Choice text contains inappropriate content", nameof(text));

        Text = text.Trim();
        Description = description?.Trim() ?? string.Empty;
        IsOptimal = isOptimal;
    }

    public void AddTraitInfluence(CharacterTraitType traitType, float influence)
    {
        TraitInfluences[traitType] = Math.Max(0, Math.Min(1.0f, influence));
    }

    public void SetNextScene(string nextSceneId)
    {
        NextSceneId = nextSceneId;
    }

    public bool Equals(SceneChoice? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id.Equals(other.Id);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as SceneChoice);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}

/// <summary>
/// LearningGoal Value Object - Pädagogisches Lernziel
/// Definiert was Kinder durch eine Geschichte lernen sollen
/// </summary>
public class LearningGoal : IEquatable<LearningGoal>
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public LearningGoalCategory Category { get; private set; }
    public LearningDifficulty Difficulty { get; private set; }
    public int TargetAge { get; private set; } // Zielgruppen-Alter
    public int Priority { get; private set; } = 1; // 1-5, höhere Priorität = wichtiger

    // Fortschritts-Tracking
    public LearningGoalStatus Status { get; private set; } = LearningGoalStatus.NotStarted;
    public float ProgressPercentage { get; private set; } = 0f;
    public DateTime? CompletedAt { get; private set; }
    public int AttemptsCount { get; private set; } = 0;

    // Erfolgs-Messungen
    public List<string> SuccessCriteria { get; private set; } = new(); // Was zeigt Erfolg an
    public Dictionary<string, object> ProgressMetrics { get; private set; } = new(); // Messbare Fortschritte
    public List<string> EvidenceOfLearning { get; private set; } = new(); // Belege für das Lernen

    // Pädagogische Eigenschaften
    public List<CharacterTraitType> RelatedTraits { get; private set; } = new(); // Welche Traits werden entwickelt
    public List<string> KeyConcepts { get; private set; } = new(); // Hauptkonzepte
    public List<string> VocabularyWords { get; private set; } = new(); // Neue Vokabeln
    public bool RequiresReflection { get; private set; } = false; // Braucht Nachdenken
    public bool RequiresDiscussion { get; private set; } = false; // Braucht Gespräch

    // Adaptive Eigenschaften
    public float AdaptiveDifficulty { get; private set; } = 1.0f; // Kann sich anpassen
    public List<string> AlternativeApproaches { get; private set; } = new(); // Alternative Lernwege
    public Dictionary<string, float> LearningStyleAdaptations { get; private set; } = new(); // Anpassungen an Lernstile

    protected LearningGoal() { } // For EF Core

    public LearningGoal(
        string title,
        string description,
        LearningGoalCategory category,
        LearningDifficulty difficulty,
        int targetAge,
        int priority = 1)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Learning goal title cannot be empty", nameof(title));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Learning goal description cannot be empty", nameof(description));

        if (!title.IsChildFriendly())
            throw new ArgumentException("Learning goal title contains inappropriate content", nameof(title));

        if (targetAge < 3 || targetAge > 18)
            throw new ArgumentException("Target age must be between 3 and 18", nameof(targetAge));

        if (priority < 1 || priority > 5)
            throw new ArgumentException("Priority must be between 1 and 5", nameof(priority));

        Title = title.Trim();
        Description = description.Trim();
        Category = category;
        Difficulty = difficulty;
        TargetAge = targetAge;
        Priority = priority;

        // Initialisiere basierend auf Kategorie
        InitializeForCategory();
    }

    /// <summary>
    /// Startet die Arbeit an diesem Lernziel
    /// </summary>
    public void StartProgress()
    {
        if (Status == LearningGoalStatus.NotStarted)
        {
            Status = LearningGoalStatus.InProgress;
            AttemptsCount++;
        }
    }

    /// <summary>
    /// Aktualisiert den Fortschritt des Lernziels
    /// </summary>
    public void UpdateProgress(float progressPercentage, string? evidence = null)
    {
        if (progressPercentage < 0 || progressPercentage > 100)
            throw new ArgumentException("Progress percentage must be between 0 and 100", nameof(progressPercentage));

        ProgressPercentage = progressPercentage;

        if (!string.IsNullOrWhiteSpace(evidence))
        {
            EvidenceOfLearning.Add($"{DateTime.UtcNow:yyyy-MM-dd}: {evidence}");
        }

        // Update Status basierend auf Fortschritt
        Status = progressPercentage switch
        {
            >= 100 => LearningGoalStatus.Completed,
            >= 80 => LearningGoalStatus.Mastered,
            > 0 => LearningGoalStatus.InProgress,
            _ => Status
        };

        if (Status == LearningGoalStatus.Completed && CompletedAt == null)
        {
            CompletedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Markiert als Wiederholung benötigt
    /// </summary>
    public void MarkForReview(string reason)
    {
        Status = LearningGoalStatus.NeedsReview;
        EvidenceOfLearning.Add($"{DateTime.UtcNow:yyyy-MM-dd}: Review needed - {reason}");
    }

    /// <summary>
    /// Fügt Erfolgs-Kriterium hinzu
    /// </summary>
    public void AddSuccessCriteria(string criteria)
    {
        if (!string.IsNullOrWhiteSpace(criteria) && criteria.IsChildFriendly())
        {
            SuccessCriteria.AddIfNotExists(criteria.Trim());
        }
    }

    /// <summary>
    /// Fügt verwandtes Trait hinzu
    /// </summary>
    public void AddRelatedTrait(CharacterTraitType traitType)
    {
        RelatedTraits.AddIfNotExists(traitType);
    }

    /// <summary>
    /// Berechnet die Eignung für ein Kind
    /// </summary>
    public float CalculateSuitabilityForChild(int childAge, List<CharacterTraitType> childTraits)
    {
        float suitability = 1.0f;

        // Alters-Passung
        var ageDifference = Math.Abs(childAge - TargetAge);
        suitability *= Math.Max(0.3f, 1.0f - (ageDifference * 0.1f));

        // Trait-Relevanz
        var matchingTraits = RelatedTraits.Intersect(childTraits).Count();
        var traitBonus = matchingTraits > 0 ? 1.0f + (matchingTraits * 0.2f) : 1.0f;
        suitability *= Math.Min(2.0f, traitBonus);

        // Schwierigkeit vs. Alter
        var expectedDifficulty = CalculateExpectedDifficultyForAge(childAge);
        if (Difficulty == expectedDifficulty)
        {
            suitability *= 1.2f; // Bonus für perfekte Schwierigkeit
        }
        else if (Math.Abs((int)Difficulty - (int)expectedDifficulty) > 1)
        {
            suitability *= 0.7f; // Abzug für zu unterschiedliche Schwierigkeit
        }

        return Math.Max(0f, Math.Min(2.0f, suitability));
    }

    /// <summary>
    /// Erstellt eine altersgerechte Beschreibung
    /// </summary>
    public string GetAgeAppropriateDescription(int childAge)
    {
        if (childAge <= 6)
        {
            // Sehr einfache Sprache
            return SimplifyForYoungChildren(Description);
        }
        else if (childAge <= 10)
        {
            // Mittlere Komplexität
            return Description;
        }
        else
        {
            // Vollständige Beschreibung mit Details
            return GetDetailedDescription();
        }
    }

    private void InitializeForCategory()
    {
        switch (Category)
        {
            case LearningGoalCategory.Vocabulary:
                RequiresReflection = false;
                RequiresDiscussion = false;
                AddSuccessCriteria("Verwendet neue Wörter aktiv");
                AddSuccessCriteria("Versteht Wortbedeutungen");
                break;

            case LearningGoalCategory.SocialSkills:
                RequiresReflection = true;
                RequiresDiscussion = true;
                AddRelatedTrait(CharacterTraitType.Empathy);
                AddRelatedTrait(CharacterTraitType.Kindness);
                AddSuccessCriteria("Zeigt verbesserte soziale Interaktion");
                break;

            case LearningGoalCategory.ProblemSolving:
                RequiresReflection = true;
                AddRelatedTrait(CharacterTraitType.Intelligence);
                AddRelatedTrait(CharacterTraitType.Determination);
                AddSuccessCriteria("Wendet Problemlösungsstrategien an");
                break;

            case LearningGoalCategory.Creativity:
                RequiresReflection = false;
                AddRelatedTrait(CharacterTraitType.Creativity);
                AddRelatedTrait(CharacterTraitType.Curiosity);
                AddSuccessCriteria("Zeigt kreative Ansätze");
                break;
        }
    }

    private LearningDifficulty CalculateExpectedDifficultyForAge(int age)
    {
        return age switch
        {
            <= 5 => LearningDifficulty.Beginner,
            <= 8 => LearningDifficulty.Elementary,
            <= 11 => LearningDifficulty.Intermediate,
            <= 14 => LearningDifficulty.Advanced,
            _ => LearningDifficulty.Expert
        };
    }

    private string SimplifyForYoungChildren(string text)
    {
        // Vereinfache Text für kleine Kinder
        var simplified = text
            .Replace("entwickeln", "lernen")
            .Replace("verstehen", "wissen")
            .Replace("analysieren", "anschauen")
            .Replace("reflektieren", "nachdenken");

        return simplified.Truncate(50);
    }

    private string GetDetailedDescription()
    {
        var details = Description;

        if (SuccessCriteria.Any())
        {
            details += $"\n\nErfolgskriterien: {string.Join(", ", SuccessCriteria)}";
        }

        if (KeyConcepts.Any())
        {
            details += $"\n\nWichtige Konzepte: {string.Join(", ", KeyConcepts)}";
        }

        return details;
    }

    // Equality Implementation
    public bool Equals(LearningGoal? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id.Equals(other.Id);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as LearningGoal);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(LearningGoal? left, LearningGoal? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(LearningGoal? left, LearningGoal? right)
    {
        return !Equals(left, right);
    }
}