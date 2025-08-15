namespace Avatales.Domain.Stories.ValueObjects;

/// <summary>
/// StoryAnalytics Value Object - Analytik und Statistiken für Geschichten
/// Sammelt Metriken für Performance-Tracking und Optimierung
/// </summary>
public class StoryAnalytics : IEquatable<StoryAnalytics>
{
    public Guid StoryId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    
    // Engagement Metriken
    public int ViewCount { get; private set; }
    public int LikeCount { get; private set; }
    public int ShareCount { get; private set; }
    public double AverageRating { get; private set; }
    public int RatingCount { get; private set; }
    
    // Content Metriken
    public int WordCount { get; private set; }
    public int ReadingTimeMinutes { get; private set; }
    public int SceneCount { get; private set; }
    public bool HasLearningMode { get; private set; }
    public bool IsPublic { get; private set; }
    
    // Zeitstempel
    public DateTime? PublishedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime AnalyticsGeneratedAt { get; private set; }

    // Berechnete Metriken
    public double EngagementRate => ViewCount > 0 ? (double)(LikeCount + ShareCount) / ViewCount : 0;
    public double LikeRate => ViewCount > 0 ? (double)LikeCount / ViewCount : 0;
    public double ShareRate => ViewCount > 0 ? (double)ShareCount / ViewCount : 0;
    public bool IsPopular => ViewCount >= 100 || LikeCount >= 20 || AverageRating >= 4.5;
    public bool IsTrending => ViewCount >= 50 && (DateTime.UtcNow - (PublishedAt ?? DateTime.UtcNow)).TotalDays <= 7;

    protected StoryAnalytics() { } // For EF Core

    public StoryAnalytics(
        Guid storyId,
        string title,
        int viewCount,
        int likeCount,
        int shareCount,
        double averageRating,
        int ratingCount,
        int wordCount,
        int readingTimeMinutes,
        int sceneCount,
        bool hasLearningMode,
        bool isPublic,
        DateTime? publishedAt,
        DateTime? completedAt)
    {
        StoryId = storyId;
        Title = title?.Trim() ?? throw new ArgumentException("Title cannot be empty");
        ViewCount = Math.Max(0, viewCount);
        LikeCount = Math.Max(0, likeCount);
        ShareCount = Math.Max(0, shareCount);
        AverageRating = Math.Clamp(averageRating, 0.0, 5.0);
        RatingCount = Math.Max(0, ratingCount);
        WordCount = Math.Max(0, wordCount);
        ReadingTimeMinutes = Math.Max(0, readingTimeMinutes);
        SceneCount = Math.Max(0, sceneCount);
        HasLearningMode = hasLearningMode;
        IsPublic = isPublic;
        PublishedAt = publishedAt;
        CompletedAt = completedAt;
        AnalyticsGeneratedAt = DateTime.UtcNow;
    }

    public string GetPerformanceCategory()
    {
        if (IsPopular && IsTrending)
            return "Viral";
        
        if (IsPopular)
            return "Beliebt";
            
        if (IsTrending)
            return "Im Trend";
            
        if (EngagementRate >= 0.15)
            return "Gut performend";
            
        if (EngagementRate >= 0.05)
            return "Durchschnittlich";
            
        return "Niedrige Performance";
    }

    public string GetContentCategory()
    {
        var categories = new List<string>();

        if (HasLearningMode)
            categories.Add("Lehrreich");

        categories.Add(WordCount switch
        {
            < 200 => "Kurz",
            < 500 => "Mittel",
            _ => "Lang"
        });

        categories.Add(SceneCount switch
        {
            1 => "Einfach",
            <= 3 => "Standard",
            _ => "Komplex"
        });

        return string.Join(", ", categories);
    }

    public string GetAudienceReach()
    {
        if (!IsPublic)
            return "Privat";

        return ViewCount switch
        {
            0 => "Keine Reichweite",
            < 10 => "Sehr geringe Reichweite",
            < 50 => "Geringe Reichweite",
            < 200 => "Mittlere Reichweite",
            < 1000 => "Hohe Reichweite",
            _ => "Sehr hohe Reichweite"
        };
    }

    public double GetQualityScore()
    {
        var score = 0.0;

        // Rating Score (40% der Gesamtbewertung)
        if (RatingCount > 0)
        {
            score += (AverageRating / 5.0) * 0.4;
        }

        // Engagement Score (30% der Gesamtbewertung)
        score += Math.Min(1.0, EngagementRate * 10) * 0.3;

        // Content Quality Score (20% der Gesamtbewertung)
        var contentScore = 0.0;
        if (WordCount >= 100 && WordCount <= 800) contentScore += 0.5; // Optimale Länge
        if (SceneCount >= 2 && SceneCount <= 5) contentScore += 0.3; // Gute Struktur
        if (HasLearningMode) contentScore += 0.2; // Pädagogischer Wert
        
        score += Math.Min(1.0, contentScore) * 0.2;

        // Consistency Score (10% der Gesamtbewertung)
        var daysPublished = PublishedAt.HasValue ? (DateTime.UtcNow - PublishedAt.Value).TotalDays : 0;
        var consistencyScore = daysPublished > 0 ? Math.Min(1.0, ViewCount / daysPublished / 10) : 0;
        score += consistencyScore * 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public List<string> GetRecommendations()
    {
        var recommendations = new List<string>();

        if (ViewCount < 10 && IsPublic)
        {
            recommendations.Add("Story in sozialen Kanälen teilen");
            recommendations.Add("Tags und Beschreibung optimieren");
        }

        if (LikeRate < 0.1 && ViewCount > 20)
        {
            recommendations.Add("Content-Qualität überprüfen");
            recommendations.Add("Zielgruppe besser ansprechen");
        }

        if (AverageRating < 3.0 && RatingCount >= 5)
        {
            recommendations.Add("Feedback analysieren und Story überarbeiten");
        }

        if (WordCount > 800)
        {
            recommendations.Add("Story für Kinder kürzen");
        }

        if (WordCount < 100)
        {
            recommendations.Add("Story ausführlicher gestalten");
        }

        if (!HasLearningMode && IsPopular)
        {
            recommendations.Add("Lernmodus hinzufügen für pädagogischen Wert");
        }

        if (SceneCount > 5)
        {
            recommendations.Add("Szenen zusammenfassen für bessere Lesbarkeit");
        }

        if (ShareCount == 0 && LikeCount > 10)
        {
            recommendations.Add("Share-Funktionen prominenter platzieren");
        }

        return recommendations;
    }

    public Dictionary<string, object> GetMetricsSnapshot()
    {
        return new Dictionary<string, object>
        {
            { "StoryId", StoryId },
            { "Title", Title },
            { "ViewCount", ViewCount },
            { "LikeCount", LikeCount },
            { "ShareCount", ShareCount },
            { "AverageRating", AverageRating },
            { "RatingCount", RatingCount },
            { "EngagementRate", Math.Round(EngagementRate, 3) },
            { "LikeRate", Math.Round(LikeRate, 3) },
            { "ShareRate", Math.Round(ShareRate, 3) },
            { "QualityScore", Math.Round(GetQualityScore(), 3) },
            { "WordCount", WordCount },
            { "ReadingTimeMinutes", ReadingTimeMinutes },
            { "SceneCount", SceneCount },
            { "HasLearningMode", HasLearningMode },
            { "IsPublic", IsPublic },
            { "IsPopular", IsPopular },
            { "IsTrending", IsTrending },
            { "PerformanceCategory", GetPerformanceCategory() },
            { "ContentCategory", GetContentCategory() },
            { "AudienceReach", GetAudienceReach() },
            { "DaysPublished", PublishedAt.HasValue ? (DateTime.UtcNow - PublishedAt.Value).TotalDays : 0 },
            { "Recommendations", GetRecommendations() }
        };
    }

    public bool HasStrongPerformance()
    {
        return GetQualityScore() >= 0.7 && EngagementRate >= 0.1;
    }

    public bool NeedsImprovement()
    {
        return GetQualityScore() < 0.4 || (ViewCount > 50 && EngagementRate < 0.02);
    }

    public string GetTargetAudienceRecommendation()
    {
        if (HasLearningMode && ReadingTimeMinutes <= 10)
            return "Perfekt für Vorschulkinder (3-6 Jahre)";

        if (WordCount >= 300 && WordCount <= 600)
            return "Ideal für Grundschulkinder (6-10 Jahre)";

        if (WordCount > 600 && SceneCount >= 4)
            return "Geeignet für ältere Kinder (10-14 Jahre)";

        if (ReadingTimeMinutes <= 5)
            return "Kurze Aufmerksamkeitsspanne (3-5 Jahre)";

        return "Allgemeine Zielgruppe (6-12 Jahre)";
    }

    public TimeSpan GetTimeToPopularity()
    {
        if (!IsPopular || !PublishedAt.HasValue)
            return TimeSpan.Zero;

        // Schätze basierend auf aktueller Performance
        var viewsPerDay = ViewCount / Math.Max(1, (DateTime.UtcNow - PublishedAt.Value).TotalDays);
        var daysToPopularity = Math.Max(0, (100 - ViewCount) / Math.Max(1, viewsPerDay));
        
        return TimeSpan.FromDays(daysToPopularity);
    }

    public List<string> GetStrengths()
    {
        var strengths = new List<string>();

        if (AverageRating >= 4.0)
            strengths.Add("Hohe Benutzerbewertung");

        if (EngagementRate >= 0.15)
            strengths.Add("Starke Benutzerinteraktion");

        if (ShareCount > LikeCount * 0.3)
            strengths.Add("Sehr teilenswert");

        if (HasLearningMode)
            strengths.Add("Pädagogischer Wert");

        if (ReadingTimeMinutes >= 5 && ReadingTimeMinutes <= 15)
            strengths.Add("Optimale Leselänge");

        if (SceneCount >= 3 && SceneCount <= 5)
            strengths.Add("Gute Struktur");

        return strengths;
    }

    public List<string> GetWeaknesses()
    {
        var weaknesses = new List<string>();

        if (ViewCount < 10 && IsPublic)
            weaknesses.Add("Geringe Sichtbarkeit");

        if (LikeRate < 0.05 && ViewCount > 20)
            weaknesses.Add("Niedrige Gefällt-mir-Rate");

        if (AverageRating < 3.0 && RatingCount >= 3)
            weaknesses.Add("Unterdurchschnittliche Bewertung");

        if (ShareCount == 0 && ViewCount > 50)
            weaknesses.Add("Wird nicht geteilt");

        if (WordCount > 800)
            weaknesses.Add("Zu lang für Zielgruppe");

        if (WordCount < 100)
            weaknesses.Add("Zu kurz für wertvollen Inhalt");

        return weaknesses;
    }

    public bool Equals(StoryAnalytics? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return StoryId.Equals(other.StoryId) && AnalyticsGeneratedAt.Equals(other.AnalyticsGeneratedAt);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as StoryAnalytics);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(StoryId, AnalyticsGeneratedAt);
    }

    public override string ToString()
    {
        return $"{Title}: {ViewCount} Views, {LikeCount} Likes, {GetPerformanceCategory()}";
    }

    public static bool operator ==(StoryAnalytics? left, StoryAnalytics? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(StoryAnalytics? left, StoryAnalytics? right)
    {
        return !Equals(left, right);
    }

    // Factory Methods
    public static StoryAnalytics CreateEmpty(Guid storyId, string title)
    {
        return new StoryAnalytics(
            storyId,
            title,
            0, 0, 0, 0.0, 0,
            0, 0, 0,
            false, false,
            null, null);
    }

    public static StoryAnalytics CreateFromStory(
        Guid storyId,
        string title,
        int wordCount,
        int readingTimeMinutes,
        int sceneCount,
        bool hasLearningMode,
        bool isPublic)
    {
        return new StoryAnalytics(
            storyId,
            title,
            0, 0, 0, 0.0, 0,
            wordCount,
            readingTimeMinutes,
            sceneCount,
            hasLearningMode,
            isPublic,
            isPublic ? DateTime.UtcNow : null,
            DateTime.UtcNow);
    }
}