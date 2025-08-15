using Avatales.Shared.Extensions;
using Avatales.Shared.Models;

namespace Avatales.Domain.Characters.ValueObjects;

/// <summary>
/// CharacterDNA Value Object - Unveränderliche Basis-Eigenschaften eines Charakters
/// Definiert die "Persönlichkeits-DNA" die sich durch das Leben des Charakters zieht
/// </summary>
public class CharacterDNA : IEquatable<CharacterDNA>
{
    public Guid DnaId { get; private set; } = Guid.NewGuid();
    public string Archetype { get; private set; } = string.Empty; // "Explorer", "Helper", "Creator", etc.
    public Dictionary<CharacterTraitType, int> BaseTraits { get; private set; } = new();
    public List<string> CorePersonalityKeywords { get; private set; } = new();
    public string PrimaryMotivation { get; private set; } = string.Empty; // Was treibt den Charakter an?
    public string CoreFear { get; private set; } = string.Empty; // Grundlegende Ängste
    public string LearningStyle { get; private set; } = string.Empty; // "Visual", "Auditory", "Kinesthetic"
    public AvatarEmotion DefaultEmotion { get; private set; } = AvatarEmotion.Neutral;
    public int AdaptabilityFactor { get; private set; } = 5; // 1-10: Wie schnell lernt/ändert sich der Charakter
    public int EmotionalDepth { get; private set; } = 5; // 1-10: Emotionale Komplexität
    public int SocialTendency { get; private set; } = 5; // 1-10: Tendenz zu sozialen Interaktionen
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // DNA-spezifische Eigenschaften für Story-Generierung
    public List<string> PreferredStoryGenres { get; private set; } = new();
    public List<string> AvoidedTopics { get; private set; } = new(); // Themen die vermieden werden sollten
    public int ComplexityPreference { get; private set; } = 5; // 1-10: Bevorzugte Story-Komplexität
    public bool PrefersHappyEndings { get; private set; } = true;
    public int ChallengeAffinity { get; private set; } = 5; // 1-10: Wie gerne werden Herausforderungen angenommen

    protected CharacterDNA() { } // For EF Core

    private CharacterDNA(
        string archetype,
        Dictionary<CharacterTraitType, int> baseTraits,
        List<string> corePersonalityKeywords,
        string primaryMotivation,
        string coreFear,
        string learningStyle,
        AvatarEmotion defaultEmotion,
        int adaptabilityFactor,
        int emotionalDepth,
        int socialTendency,
        List<string> preferredStoryGenres,
        List<string> avoidedTopics,
        int complexityPreference,
        bool prefersHappyEndings,
        int challengeAffinity)
    {
        Archetype = archetype;
        BaseTraits = new Dictionary<CharacterTraitType, int>(baseTraits);
        CorePersonalityKeywords = new List<string>(corePersonalityKeywords);
        PrimaryMotivation = primaryMotivation;
        CoreFear = coreFear;
        LearningStyle = learningStyle;
        DefaultEmotion = defaultEmotion;
        AdaptabilityFactor = adaptabilityFactor;
        EmotionalDepth = emotionalDepth;
        SocialTendency = socialTendency;
        PreferredStoryGenres = new List<string>(preferredStoryGenres);
        AvoidedTopics = new List<string>(avoidedTopics);
        ComplexityPreference = complexityPreference;
        PrefersHappyEndings = prefersHappyEndings;
        ChallengeAffinity = challengeAffinity;
    }

    /// <summary>
    /// Erstellt eine zufällige DNA basierend auf Benutzer-Eingaben
    /// </summary>
    public static CharacterDNA CreateRandom(
        string? preferredArchetype = null,
        List<CharacterTraitType>? emphasizedTraits = null,
        int childAge = 7)
    {
        var random = new Random();

        // Wähle Archetyp
        var archetypes = GetAvailableArchetypes();
        var archetype = !string.IsNullOrEmpty(preferredArchetype) && archetypes.Contains(preferredArchetype)
            ? preferredArchetype
            : archetypes[random.Next(archetypes.Count)];

        // Generiere Basis-Traits
        var baseTraits = GenerateBaseTraits(archetype, emphasizedTraits, random);

        // Generiere Persönlichkeits-Keywords
        var personalityKeywords = GeneratePersonalityKeywords(archetype, baseTraits, random);

        // Altersgerechte Eigenschaften
        var ageAdjustedProperties = GetAgeAdjustedProperties(childAge, random);

        return new CharacterDNA(
            archetype: archetype,
            baseTraits: baseTraits,
            corePersonalityKeywords: personalityKeywords,
            primaryMotivation: GetRandomMotivation(archetype, random),
            coreFear: GetRandomFear(archetype, childAge, random),
            learningStyle: GetRandomLearningStyle(random),
            defaultEmotion: GetDefaultEmotionForArchetype(archetype),
            adaptabilityFactor: random.Next(3, 9), // 3-8 für realistische Variation
            emotionalDepth: ageAdjustedProperties.EmotionalDepth,
            socialTendency: random.Next(2, 9),
            preferredStoryGenres: GetPreferredGenres(archetype, childAge, random),
            avoidedTopics: GetAvoidedTopics(childAge),
            complexityPreference: ageAdjustedProperties.ComplexityPreference,
            prefersHappyEndings: ageAdjustedProperties.PrefersHappyEndings,
            challengeAffinity: random.Next(3, 8)
        );
    }

    /// <summary>
    /// Erstellt eine DNA basierend auf Benutzer-Spezifikationen
    /// </summary>
    public static CharacterDNA CreateCustom(
        string archetype,
        Dictionary<CharacterTraitType, int> customTraits,
        List<string> personalityKeywords,
        string primaryMotivation,
        string learningStyle = "Mixed",
        int childAge = 7)
    {
        var validTraits = ValidateAndNormalizeTraits(customTraits);
        var ageAdjustedProperties = GetAgeAdjustedProperties(childAge, new Random());

        return new CharacterDNA(
            archetype: archetype,
            baseTraits: validTraits,
            corePersonalityKeywords: personalityKeywords.Take(5).ToList(),
            primaryMotivation: primaryMotivation,
            coreFear: GetRandomFear(archetype, childAge, new Random()),
            learningStyle: learningStyle,
            defaultEmotion: GetDefaultEmotionForArchetype(archetype),
            adaptabilityFactor: 5,
            emotionalDepth: ageAdjustedProperties.EmotionalDepth,
            socialTendency: 5,
            preferredStoryGenres: GetPreferredGenres(archetype, childAge, new Random()),
            avoidedTopics: GetAvoidedTopics(childAge),
            complexityPreference: ageAdjustedProperties.ComplexityPreference,
            prefersHappyEndings: ageAdjustedProperties.PrefersHappyEndings,
            challengeAffinity: 5
        );
    }

    /// <summary>
    /// Verfügbare Charakter-Archetypen
    /// </summary>
    public static List<string> GetAvailableArchetypes()
    {
        return new List<string>
        {
            "Explorer",      // Entdecker - liebt Abenteuer und Neues
            "Helper",        // Helfer - möchte anderen beistehen
            "Creator",       // Erschaffer - liebt es, Dinge zu bauen/erschaffen
            "Protector",     // Beschützer - kümmert sich um andere
            "Seeker",        // Suchender - auf der Suche nach Antworten
            "Dreamer",       // Träumer - fantasievoll und kreativ
            "Leader",        // Anführer - gerne verantwortlich
            "Friend",        // Freund - sehr sozial orientiert
            "Scholar",       // Gelehrter - liebt das Lernen
            "Comedian",      // Spaßvogel - bringt andere zum Lachen
            "Peacemaker",    // Friedensstifter - löst Konflikte
            "Adventurer"     // Abenteurer - liebt Risiko und Aufregung
        };
    }

    private static Dictionary<CharacterTraitType, int> GenerateBaseTraits(
        string archetype,
        List<CharacterTraitType>? emphasizedTraits,
        Random random)
    {
        var traits = new Dictionary<CharacterTraitType, int>();

        // Setze alle Traits auf Standardwerte (3-7)
        foreach (CharacterTraitType trait in Enum.GetValues<CharacterTraitType>())
        {
            traits[trait] = random.Next(3, 8);
        }

        // Archetyp-spezifische Anpassungen
        switch (archetype)
        {
            case "Explorer":
                traits[CharacterTraitType.Curiosity] = random.Next(7, 11);
                traits[CharacterTraitType.Courage] = random.Next(6, 10);
                break;
            case "Helper":
                traits[CharacterTraitType.Kindness] = random.Next(8, 11);
                traits[CharacterTraitType.Empathy] = random.Next(7, 10);
                break;
            case "Creator":
                traits[CharacterTraitType.Creativity] = random.Next(8, 11);
                traits[CharacterTraitType.Determination] = random.Next(6, 9);
                break;
            case "Scholar":
                traits[CharacterTraitType.Intelligence] = random.Next(7, 11);
                traits[CharacterTraitType.Wisdom] = random.Next(6, 10);
                break;
            case "Comedian":
                traits[CharacterTraitType.Humor] = random.Next(8, 11);
                traits[CharacterTraitType.Optimism] = random.Next(7, 10);
                break;
                // Weitere Archetyp-spezifische Anpassungen...
        }

        // Betone spezifische Traits falls angegeben
        if (emphasizedTraits != null)
        {
            foreach (var trait in emphasizedTraits)
            {
                traits[trait] = Math.Min(10, traits[trait] + random.Next(1, 4));
            }
        }

        return traits;
    }

    private static List<string> GeneratePersonalityKeywords(
        string archetype,
        Dictionary<CharacterTraitType, int> baseTraits,
        Random random)
    {
        var allKeywords = new Dictionary<string, List<string>>
        {
            ["Explorer"] = new() { "neugierig", "mutig", "abenteuerlustig", "experimentierfreudig" },
            ["Helper"] = new() { "hilfsbereit", "mitfühlend", "freundlich", "unterstützend" },
            ["Creator"] = new() { "kreativ", "erfinderisch", "fantasievoll", "künstlerisch" },
            ["Scholar"] = new() { "wissbegierig", "analytisch", "durchdachend", "lernbegierig" },
            ["Dreamer"] = new() { "fantasievoll", "verträumt", "idealistisch", "visionär" }
        };

        var keywords = new List<string>();

        if (allKeywords.ContainsKey(archetype))
        {
            keywords.AddRange(allKeywords[archetype].TakeRandom(2));
        }

        // Füge trait-basierte Keywords hinzu
        var topTraits = baseTraits.OrderByDescending(kvp => kvp.Value).Take(2);
        foreach (var trait in topTraits)
        {
            keywords.Add(trait.Key.ToString().ToDisplayString().ToLower());
        }

        return keywords.Distinct().Take(5).ToList();
    }

    private static string GetRandomMotivation(string archetype, Random random)
    {
        var motivations = archetype switch
        {
            "Explorer" => new[] { "Neue Welten entdecken", "Das Unbekannte erforschen", "Grenzen überwinden" },
            "Helper" => new[] { "Anderen helfen", "Freude bereiten", "Probleme lösen" },
            "Creator" => new[] { "Etwas Schönes erschaffen", "Ideen verwirklichen", "Die Welt bunter machen" },
            "Scholar" => new[] { "Neues lernen", "Geheimnisse lüften", "Verstehen wie Dinge funktionieren" },
            _ => new[] { "Freunde finden", "Spaß haben", "Gut sein", "Wachsen und lernen" }
        };

        return motivations[random.Next(motivations.Length)];
    }

    private static string GetRandomFear(string archetype, int childAge, Random random)
    {
        // Altersgerechte Ängste
        var fears = childAge <= 6
            ? new[] { "Allein gelassen werden", "Dunkelheit", "Laute Geräusche" }
            : childAge <= 10
            ? new[] { "Versagen", "Abgelehnt werden", "Anderen wehtun", "Etwas Wichtiges verlieren" }
            : new[] { "Nicht gut genug sein", "Freunde enttäuschen", "Ungerecht behandelt werden" };

        return fears[random.Next(fears.Length)];
    }

    private static string GetRandomLearningStyle(Random random)
    {
        var styles = new[] { "Visual", "Auditory", "Kinesthetic", "Mixed" };
        return styles[random.Next(styles.Length)];
    }

    private static AvatarEmotion GetDefaultEmotionForArchetype(string archetype)
    {
        return archetype switch
        {
            "Explorer" => AvatarEmotion.Curious,
            "Helper" => AvatarEmotion.Happy,
            "Creator" => AvatarEmotion.Excited,
            "Scholar" => AvatarEmotion.Thoughtful,
            "Comedian" => AvatarEmotion.Playful,
            "Dreamer" => AvatarEmotion.Thoughtful,
            _ => AvatarEmotion.Happy
        };
    }

    private static List<string> GetPreferredGenres(string archetype, int childAge, Random random)
    {
        var allGenres = Enum.GetNames<StoryGenre>().ToList();
        var archetypeGenres = archetype switch
        {
            "Explorer" => new[] { "Adventure", "Mystery", "Nature" },
            "Helper" => new[] { "Friendship", "Family", "Educational" },
            "Creator" => new[] { "Fantasy", "Fairy_Tale", "Adventure" },
            "Scholar" => new[] { "Educational", "Science", "Historical" },
            _ => new[] { "Adventure", "Friendship", "Family" }
        };

        var genres = archetypeGenres.ToList();

        // Füge zufällige zusätzliche Genres hinzu
        var additionalGenres = allGenres.Except(genres).TakeRandom(2);
        genres.AddRange(additionalGenres);

        return genres.Take(4).ToList();
    }

    private static List<string> GetAvoidedTopics(int childAge)
    {
        var avoided = new List<string> { "Gewalt", "Tod", "Krieg", "Alpträume" };

        if (childAge <= 6)
        {
            avoided.AddRange(new[] { "Trennung", "Verlust", "Komplexe Probleme" });
        }

        return avoided;
    }

    private static (int EmotionalDepth, int ComplexityPreference, bool PrefersHappyEndings)
        GetAgeAdjustedProperties(int childAge, Random random)
    {
        return childAge switch
        {
            <= 6 => (
                EmotionalDepth: random.Next(2, 5),
                ComplexityPreference: random.Next(1, 4),
                PrefersHappyEndings: true
            ),
            <= 10 => (
                EmotionalDepth: random.Next(4, 7),
                ComplexityPreference: random.Next(3, 7),
                PrefersHappyEndings: random.Next(1, 11) > 2 // 80% Chance
            ),
            _ => (
                EmotionalDepth: random.Next(5, 9),
                ComplexityPreference: random.Next(4, 9),
                PrefersHappyEndings: random.Next(1, 11) > 4 // 60% Chance
            )
        };
    }

    private static Dictionary<CharacterTraitType, int> ValidateAndNormalizeTraits(
        Dictionary<CharacterTraitType, int> traits)
    {
        var normalized = new Dictionary<CharacterTraitType, int>();

        foreach (var trait in traits)
        {
            normalized[trait.Key] = Math.Max(1, Math.Min(10, trait.Value));
        }

        // Stelle sicher, dass alle Traits vorhanden sind
        foreach (CharacterTraitType traitType in Enum.GetValues<CharacterTraitType>())
        {
            if (!normalized.ContainsKey(traitType))
            {
                normalized[traitType] = 5; // Standardwert
            }
        }

        return normalized;
    }

    /// <summary>
    /// Berechnet Kompatibilität mit einem anderen CharacterDNA
    /// </summary>
    public double CalculateCompatibility(CharacterDNA other)
    {
        if (other == null) return 0;

        double score = 0;
        int factors = 0;

        // Vergleiche Traits
        foreach (var trait in BaseTraits)
        {
            if (other.BaseTraits.ContainsKey(trait.Key))
            {
                var difference = Math.Abs(trait.Value - other.BaseTraits[trait.Key]);
                score += (10 - difference) / 10.0; // Je ähnlicher, desto höher der Score
                factors++;
            }
        }

        // Vergleiche Story-Präferenzen
        var commonGenres = PreferredStoryGenres.Intersect(other.PreferredStoryGenres).Count();
        score += (double)commonGenres / Math.Max(PreferredStoryGenres.Count, other.PreferredStoryGenres.Count);
        factors++;

        // Vergleiche soziale Tendenzen
        var socialDifference = Math.Abs(SocialTendency - other.SocialTendency);
        score += (10 - socialDifference) / 10.0;
        factors++;

        return factors > 0 ? score / factors : 0;
    }

    /// <summary>
    /// Erstellt eine Zusammenfassung der DNA für UI-Anzeige
    /// </summary>
    public string GetSummary()
    {
        var topTraits = BaseTraits.OrderByDescending(kvp => kvp.Value)
            .Take(3)
            .Select(kvp => kvp.Key.ToDisplayString().ToLower());

        return $"{Archetype} • {string.Join(", ", topTraits)} • {PrimaryMotivation}";
    }

    // Equality Implementation
    public bool Equals(CharacterDNA? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return DnaId.Equals(other.DnaId);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as CharacterDNA);
    }

    public override int GetHashCode()
    {
        return DnaId.GetHashCode();
    }

    public static bool operator ==(CharacterDNA? left, CharacterDNA? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(CharacterDNA? left, CharacterDNA? right)
    {
        return !Equals(left, right);
    }
}