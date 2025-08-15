namespace Avatales.Shared.Models;

/// <summary>
/// Benutzerrollen im System
/// </summary>
public enum UserRole
{
    FamilyMember = 1,
    Parent = 2,
    Premium = 3,
    Moderator = 4,
    Admin = 5
}

/// <summary>
/// Abonnement-Typen
/// </summary>
public enum SubscriptionType
{
    Free = 1,
    Starter = 2,
    Family = 3,
    Premium = 4,
    PremiumAdult = 5
}

/// <summary>
/// Charaktereigenschaften/Traits
/// </summary>
public enum CharacterTraitType
{
    Courage = 1,        // Mut
    Curiosity = 2,      // Neugier
    Kindness = 3,       // Freundlichkeit
    Creativity = 4,     // Kreativität
    Intelligence = 5,   // Intelligenz
    Humor = 6,          // Humor
    Wisdom = 7,         // Weisheit
    Empathy = 8,        // Empathie
    Determination = 9,  // Entschlossenheit
    Optimism = 10       // Optimismus
}

/// <summary>
/// Story-Genres
/// </summary>
public enum StoryGenre
{
    Adventure = 1,      // Abenteuer
    Fantasy = 2,        // Fantasy
    Mystery = 3,        // Rätsel/Mystery
    Educational = 4,    // Lehrreich
    Friendship = 5,     // Freundschaft
    Family = 6,         // Familie
    Nature = 7,         // Natur
    Science = 8,        // Wissenschaft
    Space = 9,          // Weltraum
    Historical = 10,    // Historisch
    Fairy_Tale = 11,    // Märchen
    Comedy = 12         // Komödie
}

/// <summary>
/// Content-Moderation Status
/// </summary>
public enum ContentModerationStatus
{
    Pending = 1,        // Wartend
    Approved = 2,       // Genehmigt
    Rejected = 3,       // Abgelehnt
    FlaggedForReview = 4, // Zur Überprüfung markiert
    AutoApproved = 5    // Automatisch genehmigt
}

/// <summary>
/// Lernziele
/// </summary>
public enum LearningGoal
{
    BuildCourage = 1,        // Mut entwickeln
    ExpandVocabulary = 2,    // Wortschatz erweitern
    PracticeKindness = 3,    // Freundlichkeit üben
    DevelopCreativity = 4,   // Kreativität entwickeln
    ProblemSolving = 5,      // Problemlösung
    SocialSkills = 6,        // Soziale Fähigkeiten
    EnvironmentalAwareness = 7, // Umweltbewusstsein
    CulturalLearning = 8,    // Kulturelles Lernen
    EmotionalIntelligence = 9, // Emotionale Intelligenz
    CriticalThinking = 10    // Kritisches Denken
}

/// <summary>
/// Social Feed Item Typen
/// </summary>
public enum SocialFeedItemType
{
    CharacterShared = 1,     // Charakter geteilt
    StoryFeatured = 2,       // Geschichte featured
    Milestone = 3,           // Meilenstein erreicht
    Achievement = 4,         // Erfolg freigeschaltet
    CommunityUpdate = 5      // Community-Update
}



/// <summary>
/// AI Service Types
/// </summary>
public enum AIServiceType
{
    StoryGeneration = 1,    // Story-Generierung
    CharacterGeneration = 2, // Charakter-Generierung
    ImageGeneration = 3,    // Bild-Generierung
    ContentModeration = 4   // Content-Moderation
}



/// <summary>
/// Status für das Teilen von Charakteren
/// </summary>
public enum CharacterSharingStatus
{
    Private = 1,        // Nur für den Besitzer sichtbar
    Family = 2,         // Sichtbar für Familienmitglieder
    Friends = 3,        // Sichtbar für Freunde
    Community = 4,      // Öffentlich in der Community
    Featured = 5        // Von Moderatoren hervorgehoben
}

/// <summary>
/// Status der Story-Generierung
/// </summary>
public enum StoryGenerationStatus
{
    Pending = 1,        // Warten auf Verarbeitung
    InProgress = 2,     // Wird gerade generiert
    Completed = 3,      // Erfolgreich generiert
    Failed = 4,         // Fehler bei Generierung
    Cancelled = 5,      // Vom Benutzer abgebrochen
    Reviewing = 6       // In Moderation
}

/// <summary>
/// Typen von Charakter-Erinnerungen
/// </summary>
public enum MemoryType
{
    StoryExperience = 1,    // Erlebnis aus einer Geschichte
    UserInteraction = 2,    // Direkte Interaktion mit dem Benutzer
    LearningMoment = 3,     // Wichtige Lernmomente
    EmotionalEvent = 4,     // Emotional bedeutsame Ereignisse
    SkillDevelopment = 5,   // Fähigkeitsentwicklung
    SocialConnection = 6,   // Soziale Verbindungen
    Achievement = 7,        // Errungenschaften
    Challenge = 8,          // Herausforderungen
    Discovery = 9,          // Entdeckungen
    Reflection = 10         // Reflexionen und Gedanken
}

/// <summary>
/// Wichtigkeitslevel für Erinnerungen
/// </summary>
public enum MemoryImportance
{
    Low = 1,            // Niedrige Priorität (verfällt schnell)
    Medium = 2,         // Mittlere Priorität (normale Verfallsrate)
    High = 3,           // Hohe Priorität (verfällt langsam)
    Critical = 4,       // Kritisch wichtig (verfällt nie)
    Core = 5            // Kern-Erinnerung (Teil der Persönlichkeit)
}

/// <summary>
/// Lernziel-Kategorien
/// </summary>
public enum LearningGoalCategory
{
    Vocabulary = 1,         // Wortschatz
    Reading = 2,            // Leseverständnis
    Creativity = 3,         // Kreativität
    ProblemSolving = 4,     // Problemlösung
    SocialSkills = 5,       // Soziale Kompetenzen
    Empathy = 6,            // Empathie
    Courage = 7,            // Mut
    Perseverance = 8,       // Durchhaltevermögen
    CriticalThinking = 9,   // Kritisches Denken
    Curiosity = 10,         // Neugier
    SelfConfidence = 11,    // Selbstvertrauen
    Cooperation = 12,       // Zusammenarbeit
    Leadership = 13,        // Führungskompetenz
    Responsibility = 14,    // Verantwortung
    Independence = 15       // Selbstständigkeit
}

/// <summary>
/// Schwierigkeitsgrad von Lernzielen
/// </summary>
public enum LearningDifficulty
{
    Beginner = 1,       // Anfänger (3-5 Jahre)
    Elementary = 2,     // Grundschule (6-8 Jahre)
    Intermediate = 3,   // Mittelstufe (9-11 Jahre)
    Advanced = 4,       // Fortgeschritten (12+ Jahre)
    Expert = 5          // Experte (für besonders begabte Kinder)
}

/// <summary>
/// Status von Lernzielen
/// </summary>
public enum LearningGoalStatus
{
    NotStarted = 1,     // Noch nicht begonnen
    InProgress = 2,     // In Bearbeitung
    Completed = 3,      // Abgeschlossen
    Mastered = 4,       // Gemeistert
    NeedsReview = 5     // Benötigt Wiederholung
}

/// <summary>
/// Soziale Aktivitätstypen
/// </summary>
public enum SocialActivityType
{
    CharacterShared = 1,        // Charakter geteilt
    CharacterAdopted = 2,       // Charakter adoptiert
    StoryLiked = 3,             // Geschichte geliked
    StoryShared = 4,            // Geschichte geteilt
    CommentAdded = 5,           // Kommentar hinzugefügt
    FollowUser = 6,             // Benutzer gefolgt
    CollaborativeStory = 7,     // Gemeinschaftsgeschichte
    ChallengeCompleted = 8      // Herausforderung abgeschlossen
}

/// <summary>
/// Benachrichtigungstypen
/// </summary>
public enum NotificationType
{
    Welcome = 1,                    // Willkommensnachricht
    NewStoryGenerated = 2,          // Neue Geschichte generiert
    LearningGoalCompleted = 3,      // Lernziel erreicht
    CharacterLevelUp = 4,           // Charakter Level-Up
    WeeklyReport = 5,               // Wöchentlicher Bericht
    SocialActivity = 6,             // Soziale Aktivität
    SystemMaintenance = 7,          // Systemwartung
    SecurityAlert = 8,              // Sicherheitswarnung
    SubscriptionExpiry = 9,         // Abonnement läuft ab
    NewFeature = 10                 // Neue Funktion verfügbar
}

/// <summary>
/// Avatar-Emotionen für die Darstellung
/// </summary>
public enum AvatarEmotion
{
    Neutral = 1,        // Neutral
    Happy = 2,          // Glücklich
    Excited = 3,        // Aufgeregt
    Curious = 4,        // Neugierig
    Confident = 5,      // Selbstbewusst
    Thoughtful = 6,     // Nachdenklich
    Surprised = 7,      // Überrascht
    Concerned = 8,      // Besorgt
    Proud = 9,          // Stolz
    Determined = 10,    // Entschlossen
    Playful = 11,       // Verspielt
    Wise = 12          // Weise
}

/// <summary>
/// Inhalts-Kategorien für Stories
/// </summary>
public enum ContentCategory
{
    General = 1,            // Allgemein
    Educational = 2,        // Bildung
    Entertainment = 3,      // Unterhaltung
    Social = 4,             // Sozial
    Creative = 5,           // Kreativ
    Adventure = 6,          // Abenteuer
    Mystery = 7,            // Rätsel
    Science = 8,            // Wissenschaft
    History = 9,            // Geschichte
    Nature = 10,            // Natur
    Fantasy = 11,           // Fantasy
    Friendship = 12,        // Freundschaft
    Family = 13,            // Familie
    Values = 14,            // Werte
    Problem_Solving = 15    // Problemlösung
}