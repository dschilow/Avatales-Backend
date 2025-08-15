using Avatales.Domain.Stories.ValueObjects;
using Avatales.Domain.Stories.Events;
using Avatales.Shared.Models;

namespace Avatales.Domain.Stories.Entities;

/// <summary>
/// Story-Entität repräsentiert eine generierte Geschichte
/// Enthält Szenen, Lernziele und Charakter-Interaktionen
/// </summary>
public class Story : BaseEntity
{
    private readonly List<StoryScene> _scenes = new();
    private readonly List<LearningGoal> _learningGoals = new();
    private readonly List<string> _imageUrls = new();
    private readonly List<string> _tags = new();

    public string Title { get; private set; } = string.Empty;
    public string Summary { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public Guid MainCharacterId { get; private set; }
    public Guid AuthorUserId { get; private set; }
    public StoryGenre Genre { get; private set; }
    public string UserPrompt { get; private set; } = string.Empty;
    public StoryGenerationStatus GenerationStatus { get; private set; } = StoryGenerationStatus.Pending;
    public ContentModerationStatus ModerationStatus { get; private set; } = ContentModerationStatus.Pending;
    public int ReadingTimeMinutes { get; private set; }
    public int WordCount { get; private set; }
    public int RecommendedAge { get; private set; } = 6;
    public bool IsPublic { get; private set; } = false;
    public bool HasImages { get; private set; } = false;
    public bool HasLearningMode { get; private set; } = false;
    public DateTime? CompletedAt { get; private set; }
    public DateTime? PublishedAt { get; private set; }

    // AI Generation Metadata
    public string AIModel { get; private set; } = string.Empty;
    public decimal GenerationCost { get; private set; }
    public int TokensUsed { get; private set; }
    public TimeSpan GenerationTime { get; private set; }

    // Engagement Statistics
    public int ViewCount { get; private set; } = 0;
    public int LikeCount { get; private set; } = 0;
    public int ShareCount { get; private set; } = 0;
    public double AverageRating { get; private set; } = 0.0;
    public int RatingCount { get; private set; } = 0;

    // Navigation Properties
    public IReadOnlyCollection<StoryScene> Scenes => _scenes.AsReadOnly();
    public IReadOnlyCollection<LearningGoal> LearningGoals => _learningGoals.AsReadOnly();
    public IReadOnlyCollection<string> ImageUrls => _imageUrls.AsReadOnly();
    public IReadOnlyCollection<string> Tags => _tags.AsReadOnly();

    protected Story() { } // For EF Core

    public Story(
        string title,
        string userPrompt,
        Guid mainCharacterId,
        Guid authorUserId,
        StoryGenre genre = StoryGenre.Adventure)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Story title cannot be empty", nameof(title));

        if (mainCharacterId == Guid.Empty)
            throw new ArgumentException("Main character ID cannot be empty", nameof(mainCharacterId));

        if (authorUserId == Guid.Empty)
            throw new ArgumentException("Author user ID cannot be empty", nameof(authorUserId));

        Title = title.Trim();
        UserPrompt = userPrompt?.Trim() ?? string.Empty;
        MainCharacterId = mainCharacterId;
        AuthorUserId = authorUserId;
        Genre = genre;
        GenerationStatus = StoryGenerationStatus.Pending;
        ModerationStatus = ContentModerationStatus.Pending;

        AddDomainEvent(new StoryCreationStartedEvent(Id, title, mainCharacterId, authorUserId, genre));
    }

    public void StartGeneration(string aiModel = "gpt-4")
    {
        if (GenerationStatus != StoryGenerationStatus.Pending)
            throw new InvalidOperationException($"Cannot start generation. Current status: {GenerationStatus}");

        GenerationStatus = StoryGenerationStatus.InProgress;
        AIModel = aiModel;
        MarkAsUpdated();

        AddDomainEvent(new StoryGenerationStartedEvent(Id, aiModel));
    }

    public void CompleteGeneration(
        string content,
        string summary,
        List<StoryScene> scenes,
        decimal cost,
        int tokensUsed,
        TimeSpan generationTime)
    {
        if (GenerationStatus != StoryGenerationStatus.InProgress)
            throw new InvalidOperationException($"Cannot complete generation. Current status: {GenerationStatus}");

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Story content cannot be empty", nameof(content));

        Content = content.Trim();
        Summary = summary?.Trim() ?? string.Empty;
        GenerationCost = cost;
        TokensUsed = tokensUsed;
        GenerationTime = generationTime;
        GenerationStatus = StoryGenerationStatus.Completed;
        CompletedAt = DateTime.UtcNow;

        // Berechne Statistiken
        WordCount = Content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        ReadingTimeMinutes = Math.Max(1, WordCount / 200); // 200 Wörter pro Minute für Kinder

        // Füge Szenen hinzu
        foreach (var scene in scenes ?? new List<StoryScene>())
        {
            AddScene(scene);
        }

        MarkAsUpdated();
        AddDomainEvent(new StoryGenerationCompletedEvent(Id, WordCount, scenes?.Count ?? 0, cost));
    }

    public void FailGeneration(string errorMessage)
    {
        if (GenerationStatus != StoryGenerationStatus.InProgress)
            throw new InvalidOperationException($"Cannot fail generation. Current status: {GenerationStatus}");

        GenerationStatus = StoryGenerationStatus.Failed;
        MarkAsUpdated();

        AddDomainEvent(new StoryGenerationFailedEvent(Id, errorMessage));
    }

    public void AddScene(StoryScene scene)
    {
        if (scene == null)
            throw new ArgumentNullException(nameof(scene));

        _scenes.Add(scene);
        MarkAsUpdated();
    }

    public void AddLearningGoal(LearningGoal learningGoal)
    {
        if (learningGoal == null)
            throw new ArgumentNullException(nameof(learningGoal));

        if (_learningGoals.Count >= 5)
            throw new InvalidOperationException("Cannot add more than 5 learning goals per story");

        _learningGoals.Add(learningGoal);
        HasLearningMode = true;
        MarkAsUpdated();
    }

    public void AddImageUrl(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return;

        var cleanUrl = imageUrl.Trim();
        if (!_imageUrls.Contains(cleanUrl) && _imageUrls.Count < 10)
        {
            _imageUrls.Add(cleanUrl);
            HasImages = true;
            MarkAsUpdated();
        }
    }

    public void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return;

        var cleanTag = tag.Trim().ToLower();
        if (!_tags.Contains(cleanTag) && _tags.Count < 15)
        {
            _tags.Add(cleanTag);
            MarkAsUpdated();
        }
    }

    public void SetModerationStatus(ContentModerationStatus status, string? reason = null)
    {
        var oldStatus = ModerationStatus;
        ModerationStatus = status;
        MarkAsUpdated();

        AddDomainEvent(new StoryModerationStatusChangedEvent(Id, oldStatus, status, reason));

        // Automatisch veröffentlichen wenn genehmigt
        if (status == ContentModerationStatus.Approved && !IsPublic)
        {
            PublishStory();
        }
    }

    public void PublishStory()
    {
        if (GenerationStatus != StoryGenerationStatus.Completed)
            throw new InvalidOperationException("Cannot publish incomplete story");

        if (ModerationStatus != ContentModerationStatus.Approved && ModerationStatus != ContentModerationStatus.AutoApproved)
            throw new InvalidOperationException("Cannot publish unapproved story");

        IsPublic = true;
        PublishedAt = DateTime.UtcNow;
        MarkAsUpdated();

        AddDomainEvent(new StoryPublishedEvent(Id, Title, Genre));
    }

    public void UnpublishStory(string reason = "Content review")
    {
        if (!IsPublic)
            return;

        IsPublic = false;
        PublishedAt = null;
        MarkAsUpdated();

        AddDomainEvent(new StoryUnpublishedEvent(Id, reason));
    }

    public void RecordView()
    {
        ViewCount++;
        MarkAsUpdated();

        // Event für populäre Geschichten
        if (ViewCount % 100 == 0)
        {
            AddDomainEvent(new StoryMilestoneAchievedEvent(Id, "Views", ViewCount));
        }
    }

    public void AddLike()
    {
        LikeCount++;
        MarkAsUpdated();
        UpdateAverageRating();

        if (LikeCount % 10 == 0)
        {
            AddDomainEvent(new StoryMilestoneAchievedEvent(Id, "Likes", LikeCount));
        }
    }

    public void RemoveLike()
    {
        if (LikeCount > 0)
        {
            LikeCount--;
            MarkAsUpdated();
            UpdateAverageRating();
        }
    }

    public void RecordShare()
    {
        ShareCount++;
        MarkAsUpdated();

        if (ShareCount % 5 == 0)
        {
            AddDomainEvent(new StoryMilestoneAchievedEvent(Id, "Shares", ShareCount));
        }
    }

    public void AddRating(double rating)
    {
        if (rating < 1.0 || rating > 5.0)
            throw new ArgumentException("Rating must be between 1.0 and 5.0", nameof(rating));

        // Berechne neuen Durchschnitt
        var totalRating = AverageRating * RatingCount + rating;
        RatingCount++;
        AverageRating = totalRating / RatingCount;

        MarkAsUpdated();
    }

    public bool IsChildFriendly()
    {
        return ModerationStatus == ContentModerationStatus.Approved || 
               ModerationStatus == ContentModerationStatus.AutoApproved;
    }

    public bool IsPopular()
    {
        return ViewCount >= 100 || LikeCount >= 20 || AverageRating >= 4.5;
    }

    public bool IsTrending()
    {
        var recentViews = ViewCount; // Hier würde man zeitbasierte Metriken verwenden
        return IsPublic && recentViews >= 50 && (DateTime.UtcNow - (PublishedAt ?? DateTime.UtcNow)).TotalDays <= 7;
    }

    public bool CanBeShared()
    {
        return IsPublic && IsChildFriendly();
    }

    public string GetReadingDifficulty()
    {
        return WordCount switch
        {
            < 100 => "Sehr einfach",
            < 300 => "Einfach",
            < 500 => "Mittel",
            < 800 => "Fortgeschritten",
            _ => "Schwierig"
        };
    }

    public List<string> GetRecommendedTags()
    {
        var recommendedTags = new List<string> { Genre.ToString().ToLower() };

        if (HasLearningMode)
            recommendedTags.Add("lernmodus");

        if (HasImages)
            recommendedTags.Add("bebildert");

        if (ReadingTimeMinutes <= 5)
            recommendedTags.Add("kurz");
        else if (ReadingTimeMinutes >= 15)
            recommendedTags.Add("lang");

        if (IsPopular())
            recommendedTags.Add("beliebt");

        return recommendedTags;
    }

    public StoryAnalytics GetAnalytics()
    {
        return new StoryAnalytics(
            Id,
            Title,
            ViewCount,
            LikeCount,
            ShareCount,
            AverageRating,
            RatingCount,
            WordCount,
            ReadingTimeMinutes,
            _scenes.Count,
            HasLearningMode,
            IsPublic,
            PublishedAt,
            CompletedAt);
    }

    private void UpdateAverageRating()
    {
        // Vereinfachte Bewertung basierend auf Likes vs Views
        if (ViewCount > 0)
        {
            var likeRatio = (double)LikeCount / ViewCount;
            AverageRating = Math.Min(5.0, 1.0 + (likeRatio * 4.0));
        }
    }

    public bool CanBeUsedForCharacterDevelopment()
    {
        return IsChildFriendly() && 
               GenerationStatus == StoryGenerationStatus.Completed &&
               !string.IsNullOrEmpty(Content);
    }

    public List<string> ExtractCharacterDevelopmentOpportunities()
    {
        var opportunities = new List<string>();

        if (HasLearningMode && _learningGoals.Any())
        {
            opportunities.AddRange(_learningGoals.Select(lg => lg.Name));
        }

        // Analysiere Genre für Trait-Entwicklung
        opportunities.AddRange(Genre switch
        {
            StoryGenre.Adventure => new[] { "Mut", "Entschlossenheit" },
            StoryGenre.Friendship => new[] { "Freundlichkeit", "Empathie" },
            StoryGenre.Mystery => new[] { "Neugier", "Intelligenz" },
            StoryGenre.Educational => new[] { "Weisheit", "Lernbereitschaft" },
            StoryGenre.Comedy => new[] { "Humor", "Optimismus" },
            _ => new[] { "Allgemeine Entwicklung" }
        });

        return opportunities.Distinct().ToList();
    }
}