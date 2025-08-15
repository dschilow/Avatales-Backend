using Avatales.Shared.Models;

namespace Avatales.Domain.Stories.ValueObjects;

/// <summary>
/// LearningGoal Value Object - pädagogisches Lernziel für Geschichten
/// Definiert was Kinder durch die Geschichte lernen sollen
/// </summary>
public class LearningGoal : IEquatable<LearningGoal>
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public LearningGoal GoalType { get; private set; }
    public int TargetAge { get; private set; }
    public int DifficultyLevel { get; private set; } // 1-10
    public List<string> Keywords { get; private set; } = new();
    public List<string> MeasurementCriteria { get; private set; } = new();
    public string Category { get; private set; } = string.Empty;
    public bool IsMainGoal { get; private set; } = false;
    public int ExpectedDurationMinutes { get; private set; }

    protected LearningGoal() { } // For EF Core

    public LearningGoal(
        string name,
        string description,
        LearningGoal goalType,
        int targetAge = 6,
        int difficultyLevel = 5,
        bool isMainGoal = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Learning goal name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Learning goal description cannot be empty", nameof(description));

        if (targetAge < 3 || targetAge > 16)
            throw new ArgumentException("Target age must be between 3 and 16", nameof(targetAge));

        if (difficultyLevel < 1 || difficultyLevel > 10)
            throw new ArgumentException("Difficulty level must be between 1 and 10", nameof(difficultyLevel));

        Name = name.Trim();
        Description = description.Trim();
        GoalType = goalType;
        TargetAge = targetAge;
        DifficultyLevel = difficultyLevel;
        IsMainGoal = isMainGoal;

        Category = DetermineCategory(goalType);
        ExpectedDurationMinutes = CalculateExpectedDuration();
        InitializeKeywords();
        InitializeMeasurementCriteria();
    }

    public bool IsAppropriateForAge(int age)
    {
        // Lernziel ist angemessen wenn das Kind im Zielbereich ist (±2 Jahre)
        return Math.Abs(age - TargetAge) <= 2;
    }

    public bool IsEasyForAge(int age)
    {
        return age > TargetAge + 2;
    }

    public bool IsChallengingForAge(int age)
    {
        return age < TargetAge - 1;
    }

    public string GetDifficultyDescription()
    {
        return DifficultyLevel switch
        {
            1 => "Sehr einfach",
            2 => "Einfach",
            3 => "Leicht",
            4 => "Einfach-mittel",
            5 => "Mittel",
            6 => "Mittel-schwer",
            7 => "Anspruchsvoll",
            8 => "Schwierig",
            9 => "Sehr schwierig",
            10 => "Expertenlevel",
            _ => "Unbekannt"
        };
    }

    public string GetAgeRangeDescription()
    {
        var minAge = Math.Max(3, TargetAge - 2);
        var maxAge = Math.Min(16, TargetAge + 2);
        return $"{minAge}-{maxAge} Jahre";
    }

    public List<string> GetRecommendedActivities()
    {
        return GoalType switch
        {
            Avatales.Shared.Models.LearningGoal.BuildCourage => new List<string>
            {
                "Mutige Entscheidungen treffen",
                "Ängste überwinden",
                "Anderen helfen",
                "Neue Herausforderungen annehmen"
            },
            Avatales.Shared.Models.LearningGoal.ExpandVocabulary => new List<string>
            {
                "Neue Wörter entdecken",
                "Bedeutungen erraten",
                "Synonyme finden",
                "Wörter in Sätzen verwenden"
            },
            Avatales.Shared.Models.LearningGoal.PracticeKindness => new List<string>
            {
                "Anderen helfen",
                "Freundlich sein",
                "Teilen lernen",
                "Mitgefühl zeigen"
            },
            Avatales.Shared.Models.LearningGoal.DevelopCreativity => new List<string>
            {
                "Kreative Lösungen finden",
                "Kunst schaffen",
                "Geschichten erfinden",
                "Probleme anders lösen"
            },
            Avatales.Shared.Models.LearningGoal.ProblemSolving => new List<string>
            {
                "Logisch denken",
                "Schritt für Schritt vorgehen",
                "Alternativen abwägen",
                "Strategien entwickeln"
            },
            Avatales.Shared.Models.LearningGoal.SocialSkills => new List<string>
            {
                "Mit anderen kommunizieren",
                "Konflikte lösen",
                "Teamwork üben",
                "Empathie entwickeln"
            },
            Avatales.Shared.Models.LearningGoal.EnvironmentalAwareness => new List<string>
            {
                "Natur schützen",
                "Umweltbewusst handeln",
                "Tiere respektieren",
                "Nachhaltigkeit verstehen"
            },
            Avatales.Shared.Models.LearningGoal.CulturalLearning => new List<string>
            {
                "Andere Kulturen kennenlernen",
                "Toleranz entwickeln",
                "Traditionen verstehen",
                "Vielfalt schätzen"
            },
            Avatales.Shared.Models.LearningGoal.EmotionalIntelligence => new List<string>
            {
                "Gefühle erkennen",
                "Emotionen ausdrücken",
                "Empathie zeigen",
                "Selbstregulation üben"
            },
            Avatales.Shared.Models.LearningGoal.CriticalThinking => new List<string>
            {
                "Fragen stellen",
                "Informationen bewerten",
                "Logisch argumentieren",
                "Entscheidungen durchdenken"
            },
            _ => new List<string> { "Allgemeine Lernaktivitäten" }
        };
    }

    public List<string> GetSuccessIndicators()
    {
        return GoalType switch
        {
            Avatales.Shared.Models.LearningGoal.BuildCourage => new List<string>
            {
                "Traut sich neue Dinge zu",
                "Steht für andere ein",
                "Überwinden von Ängsten",
                "Selbstvertrauen gestärkt"
            },
            Avatales.Shared.Models.LearningGoal.ExpandVocabulary => new List<string>
            {
                "Verwendet neue Wörter",
                "Versteht komplexere Begriffe",
                "Erweiterte Ausdrucksfähigkeit",
                "Aktiver Wortschatz gewachsen"
            },
            Avatales.Shared.Models.LearningGoal.PracticeKindness => new List<string>
            {
                "Hilft anderen spontan",
                "Zeigt Mitgefühl",
                "Teilt gerne",
                "Rücksichtsvoll im Umgang"
            },
            _ => MeasurementCriteria
        };
    }

    public string GetStoryIntegrationSuggestion()
    {
        return GoalType switch
        {
            Avatales.Shared.Models.LearningGoal.BuildCourage => 
                "Hauptcharakter steht vor einer Herausforderung, die Mut erfordert",
            Avatales.Shared.Models.LearningGoal.ExpandVocabulary => 
                "Geschichte enthält altersgerechte neue Wörter mit Kontext",
            Avatales.Shared.Models.LearningGoal.PracticeKindness => 
                "Situationen wo Freundlichkeit und Hilfsbereitschaft gezeigt wird",
            Avatales.Shared.Models.LearningGoal.DevelopCreativity => 
                "Probleme werden mit kreativen, unkonventionellen Lösungen gelöst",
            Avatales.Shared.Models.LearningGoal.ProblemSolving => 
                "Schrittweise Problemlösung wird demonstriert und erklärt",
            Avatales.Shared.Models.LearningGoal.SocialSkills => 
                "Interaktionen zwischen Charakteren zeigen gute Kommunikation",
            _ => "Lernziel wird natürlich in die Handlung integriert"
        };
    }

    public Dictionary<string, object> GetAnalytics()
    {
        return new Dictionary<string, object>
        {
            { "Name", Name },
            { "GoalType", GoalType.ToString() },
            { "Category", Category },
            { "TargetAge", TargetAge },
            { "AgeRange", GetAgeRangeDescription() },
            { "DifficultyLevel", DifficultyLevel },
            { "DifficultyDescription", GetDifficultyDescription() },
            { "IsMainGoal", IsMainGoal },
            { "ExpectedDurationMinutes", ExpectedDurationMinutes },
            { "KeywordCount", Keywords.Count },
            { "MeasurementCriteriaCount", MeasurementCriteria.Count },
            { "RecommendedActivities", GetRecommendedActivities() },
            { "SuccessIndicators", GetSuccessIndicators() }
        };
    }

    private string DetermineCategory(LearningGoal goalType)
    {
        return goalType switch
        {
            Avatales.Shared.Models.LearningGoal.BuildCourage or 
            Avatales.Shared.Models.LearningGoal.EmotionalIntelligence => "Emotionale Entwicklung",
            
            Avatales.Shared.Models.LearningGoal.ExpandVocabulary or 
            Avatales.Shared.Models.LearningGoal.CriticalThinking => "Kognitive Entwicklung",
            
            Avatales.Shared.Models.LearningGoal.PracticeKindness or 
            Avatales.Shared.Models.LearningGoal.SocialSkills => "Soziale Entwicklung",
            
            Avatales.Shared.Models.LearningGoal.DevelopCreativity => "Kreative Entwicklung",
            
            Avatales.Shared.Models.LearningGoal.ProblemSolving => "Problemlösungskompetenz",
            
            Avatales.Shared.Models.LearningGoal.EnvironmentalAwareness => "Umweltbewusstsein",
            
            Avatales.Shared.Models.LearningGoal.CulturalLearning => "Kulturelle Bildung",
            
            _ => "Allgemeine Entwicklung"
        };
    }

    private int CalculateExpectedDuration()
    {
        return DifficultyLevel switch
        {
            <= 3 => 5,   // Einfache Ziele: 5 Minuten
            <= 6 => 10,  // Mittlere Ziele: 10 Minuten
            <= 8 => 15,  // Schwere Ziele: 15 Minuten
            _ => 20      // Sehr schwere Ziele: 20 Minuten
        };
    }

    private void InitializeKeywords()
    {
        Keywords = GoalType switch
        {
            Avatales.Shared.Models.LearningGoal.BuildCourage => 
                new List<string> { "mut", "tapfer", "angst", "herausforderung", "stark" },
            Avatales.Shared.Models.LearningGoal.ExpandVocabulary => 
                new List<string> { "wörter", "sprache", "bedeutung", "lernen", "verstehen" },
            Avatales.Shared.Models.LearningGoal.PracticeKindness => 
                new List<string> { "freundlich", "hilfsbereit", "teilen", "mitgefühl", "nett" },
            Avatales.Shared.Models.LearningGoal.DevelopCreativity => 
                new List<string> { "kreativ", "erfinden", "ideen", "kunst", "fantasie" },
            Avatales.Shared.Models.LearningGoal.ProblemSolving => 
                new List<string> { "lösung", "denken", "strategie", "logik", "analysieren" },
            _ => new List<string> { "lernen", "entwicklung", "wachstum" }
        };
    }

    private void InitializeMeasurementCriteria()
    {
        MeasurementCriteria = GoalType switch
        {
            Avatales.Shared.Models.LearningGoal.BuildCourage => new List<string>
            {
                "Zeigt mutiges Verhalten",
                "Überwindet Ängste",
                "Hilft anderen trotz Risiko"
            },
            Avatales.Shared.Models.LearningGoal.ExpandVocabulary => new List<string>
            {
                "Lernt 3-5 neue Wörter",
                "Verwendet neue Begriffe korrekt",
                "Versteht Wortbedeutungen im Kontext"
            },
            Avatales.Shared.Models.LearningGoal.PracticeKindness => new List<string>
            {
                "Zeigt hilfsbereites Verhalten",
                "Teilt mit anderen",
                "Drückt Mitgefühl aus"
            },
            _ => new List<string> { "Zeigt Fortschritt im Lernbereich" }
        };
    }

    public bool Equals(LearningGoal? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Name == other.Name && GoalType == other.GoalType && TargetAge == other.TargetAge;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as LearningGoal);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, GoalType, TargetAge);
    }

    public override string ToString()
    {
        return $"{Name} ({Category}, {GetAgeRangeDescription()})";
    }

    public static bool operator ==(LearningGoal? left, LearningGoal? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(LearningGoal? left, LearningGoal? right)
    {
        return !Equals(left, right);
    }

    // Factory Methods für häufig verwendete Lernziele
    public static LearningGoal CreateCourageGoal(int targetAge = 6)
    {
        return new LearningGoal(
            "Mut entwickeln",
            "Das Kind lernt, mutig zu sein und sich Herausforderungen zu stellen",
            Avatales.Shared.Models.LearningGoal.BuildCourage,
            targetAge,
            Math.Max(3, targetAge - 2),
            true);
    }

    public static LearningGoal CreateVocabularyGoal(int targetAge = 6, int wordCount = 5)
    {
        return new LearningGoal(
            $"Wortschatz erweitern ({wordCount} neue Wörter)",
            $"Das Kind lernt {wordCount} neue altersgerechte Wörter und deren Bedeutung",
            Avatales.Shared.Models.LearningGoal.ExpandVocabulary,
            targetAge,
            Math.Min(10, wordCount / 2 + 2));
    }

    public static LearningGoal CreateKindnessGoal(int targetAge = 5)
    {
        return new LearningGoal(
            "Freundlichkeit üben",
            "Das Kind lernt, freundlich und hilfsbereit zu anderen zu sein",
            Avatales.Shared.Models.LearningGoal.PracticeKindness,
            targetAge,
            Math.Max(2, targetAge - 3));
    }

    public static LearningGoal CreateProblemSolvingGoal(int targetAge = 8)
    {
        return new LearningGoal(
            "Probleme lösen",
            "Das Kind entwickelt logisches Denken und Problemlösungsstrategien",
            Avatales.Shared.Models.LearningGoal.ProblemSolving,
            targetAge,
            Math.Min(10, targetAge - 2));
    }
}