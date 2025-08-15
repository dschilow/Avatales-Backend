using Avatales.Domain.Users.Entities;
using Avatales.Domain.Characters.Entities;
using Avatales.Domain.Stories.Entities;
using Avatales.Shared.Models;
using Avatales.Application.Common.Interfaces;

namespace Avatales.Application.Interfaces.Repositories;

/// <summary>
/// Repository Interface für User Entity
/// Erweitert das Base Repository um User-spezifische Abfragen
/// </summary>
public interface IUserRepository : IRepository<User>
{
    // User-spezifische Abfragen
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsWithEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<List<User>> GetChildrenByParentIdAsync(Guid parentUserId, CancellationToken cancellationToken = default);
    Task<List<User>> GetByRoleAsync(UserRole role, CancellationToken cancellationToken = default);
    Task<User?> GetWithChildrenAsync(Guid userId, CancellationToken cancellationToken = default);
    
    // Subscription-bezogene Abfragen
    Task<List<User>> GetUsersWithExpiredSubscriptionsAsync(DateTime beforeDate, CancellationToken cancellationToken = default);
    Task<List<User>> GetActiveSubscribersAsync(SubscriptionType subscriptionType, CancellationToken cancellationToken = default);
    Task<int> GetActiveUserCountAsync(DateTime since, CancellationToken cancellationToken = default);
    
    // Sicherheits-bezogene Abfragen
    Task<List<User>> GetLockedUsersAsync(CancellationToken cancellationToken = default);
    Task<List<User>> GetUnverifiedUsersOlderThanAsync(DateTime beforeDate, CancellationToken cancellationToken = default);
    Task<List<User>> GetUsersWithFailedLoginsAsync(int minFailedAttempts, CancellationToken cancellationToken = default);
    
    // Statistik-Abfragen
    Task<Dictionary<UserRole, int>> GetUserCountByRoleAsync(CancellationToken cancellationToken = default);
    Task<Dictionary<SubscriptionType, int>> GetUserCountBySubscriptionAsync(CancellationToken cancellationToken = default);
    Task<List<User>> GetMostActiveUsersAsync(int count, DateTime since, CancellationToken cancellationToken = default);
    
    // Kinder-Account spezifische Abfragen
    Task<List<User>> GetChildrenByAgeRangeAsync(int minAge, int maxAge, CancellationToken cancellationToken = default);
    Task<List<User>> GetChildrenWithExcessiveUsageAsync(TimeSpan dailyLimit, CancellationToken cancellationToken = default);
    
    // Präferenzen und Einstellungen
    Task<List<User>> GetUsersByPreferenceAsync(string key, string value, CancellationToken cancellationToken = default);
    Task<Dictionary<string, string>> GetUserPreferencesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task UpdateUserPreferencesAsync(Guid userId, Dictionary<string, string> preferences, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository Interface für Character Entity
/// Spezialisiert auf Charakter-Management und -Abfragen
/// </summary>
public interface ICharacterRepository : IRepository<Character>
{
    // Charakter-Ownership Abfragen
    Task<List<Character>> GetByOwnerIdAsync(Guid ownerId, bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<List<Character>> GetActiveCharactersByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default);
    Task<int> GetCharacterCountByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default);
    
    // Charakter-Sharing und Community
    Task<List<Character>> GetSharedCharactersAsync(CharacterSharingStatus sharingStatus, CancellationToken cancellationToken = default);
    Task<List<Character>> GetAdoptableCharactersAsync(Guid? excludeOwnerId = null, CancellationToken cancellationToken = default);
    Task<List<Character>> GetCommunityCharactersAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<List<Character>> GetFeaturedCharactersAsync(CancellationToken cancellationToken = default);
    
    // Charakter-Suche und Filterung
    Task<List<Character>> SearchCharactersAsync(
        string? nameQuery = null,
        string? archetype = null,
        List<CharacterTraitType>? traits = null,
        int? minLevel = null,
        int? maxLevel = null,
        CharacterSharingStatus? sharingStatus = null,
        List<string>? tags = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
    
    // Charakter-Empfehlungen
    Task<List<Character>> GetRecommendedCharactersAsync(Guid userId, int count = 10, CancellationToken cancellationToken = default);
    Task<List<Character>> GetSimilarCharactersAsync(Guid characterId, int count = 5, CancellationToken cancellationToken = default);
    Task<List<Character>> GetCharactersByArchetypeAsync(string archetype, CancellationToken cancellationToken = default);
    
    // Charakter-Statistiken und Popularität
    Task<List<Character>> GetMostPopularCharactersAsync(int count, DateTime? since = null, CancellationToken cancellationToken = default);
    Task<List<Character>> GetMostActiveCharactersAsync(int count, DateTime since, CancellationToken cancellationToken = default);
    Task<Dictionary<string, int>> GetCharacterCountByArchetypeAsync(CancellationToken cancellationToken = default);
    Task<Dictionary<CharacterSharingStatus, int>> GetCharacterCountBySharingStatusAsync(CancellationToken cancellationToken = default);
    
    // Charakter-Entwicklung und Evolution
    Task<List<Character>> GetCharactersReadyForEvolutionAsync(CancellationToken cancellationToken = default);
    Task<List<Character>> GetCharactersByLevelRangeAsync(int minLevel, int maxLevel, CancellationToken cancellationToken = default);
    Task<List<Character>> GetCharactersWithHighTraitValuesAsync(CharacterTraitType traitType, int minValue, CancellationToken cancellationToken = default);
    
    // Memory-Management
    Task<List<Character>> GetCharactersWithExcessiveMemoriesAsync(int maxMemories, CancellationToken cancellationToken = default);
    Task<List<Character>> GetCharactersNeedingMemoryConsolidationAsync(CancellationToken cancellationToken = default);
    
    // Detaillierte Abfragen mit Navigation Properties
    Task<Character?> GetWithFullDetailsAsync(Guid characterId, CancellationToken cancellationToken = default);
    Task<Character?> GetWithTraitsAndMemoriesAsync(Guid characterId, CancellationToken cancellationToken = default);
    Task<List<Character>> GetWithOwnerDetailsAsync(List<Guid> characterIds, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository Interface für Story Entity
/// Fokussiert auf Story-Management und Content-Abfragen
/// </summary>
public interface IStoryRepository : IRepository<Story>
{
    // Story-Authorship Abfragen
    Task<List<Story>> GetByAuthorIdAsync(Guid authorUserId, CancellationToken cancellationToken = default);
    Task<List<Story>> GetByCharacterIdAsync(Guid characterId, CancellationToken cancellationToken = default);
    Task<int> GetStoryCountByAuthorAsync(Guid authorUserId, CancellationToken cancellationToken = default);
    
    // Story-Status und Moderation
    Task<List<Story>> GetByGenerationStatusAsync(StoryGenerationStatus status, CancellationToken cancellationToken = default);
    Task<List<Story>> GetByModerationStatusAsync(ContentModerationStatus status, CancellationToken cancellationToken = default);
    Task<List<Story>> GetPendingModerationAsync(CancellationToken cancellationToken = default);
    Task<List<Story>> GetFailedGenerationsAsync(DateTime since, CancellationToken cancellationToken = default);
    
    // Story-Community und Sharing
    Task<List<Story>> GetPublicStoriesAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<List<Story>> GetFeaturedStoriesAsync(CancellationToken cancellationToken = default);
    Task<List<Story>> GetCommunityStoriesAsync(StoryGenre? genre = null, int? minAge = null, int? maxAge = null, 
        int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    
    // Story-Suche und Filterung
    Task<List<Story>> SearchStoriesAsync(
        string? titleQuery = null,
        string? contentQuery = null,
        List<StoryGenre>? genres = null,
        int? minAge = null,
        int? maxAge = null,
        int? minReadingTime = null,
        int? maxReadingTime = null,
        bool? hasImages = null,
        bool? hasLearningMode = null,
        List<string>? tags = null,
        Guid? authorUserId = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
    
    // Story-Empfehlungen
    Task<List<Story>> GetRecommendedStoriesAsync(Guid userId, Guid? characterId = null, int count = 10, CancellationToken cancellationToken = default);
    Task<List<Story>> GetSimilarStoriesAsync(Guid storyId, int count = 5, CancellationToken cancellationToken = default);
    Task<List<Story>> GetStoriesByGenreAsync(StoryGenre genre, int count = 20, CancellationToken cancellationToken = default);
    
    // Story-Statistiken und Popularität
    Task<List<Story>> GetMostPopularStoriesAsync(int count, DateTime? since = null, CancellationToken cancellationToken = default);
    Task<List<Story>> GetMostReadStoriesAsync(int count, DateTime since, CancellationToken cancellationToken = default);
    Task<List<Story>> GetHighestRatedStoriesAsync(int count, int minRatings = 5, CancellationToken cancellationToken = default);
    Task<Dictionary<StoryGenre, int>> GetStoryCountByGenreAsync(CancellationToken cancellationToken = default);
    Task<Dictionary<int, int>> GetStoryCountByAgeCategoryAsync(CancellationToken cancellationToken = default);
    
    // Story-Performance und Analytics
    Task<List<Story>> GetStoriesWithMostEngagementAsync(int count, DateTime since, CancellationToken cancellationToken = default);
    Task<List<Story>> GetStoriesForAgeGroupAsync(int targetAge, int tolerance = 2, CancellationToken cancellationToken = default);
    Task<List<Story>> GetStoriesWithLearningGoalsAsync(List<LearningGoalCategory> categories, CancellationToken cancellationToken = default);
    
    // Reading History und Progress
    Task<List<Story>> GetReadStoriesByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<Story>> GetInProgressStoriesByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<Story>> GetFavoriteStoriesByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    
    // Story-Content Management
    Task<List<Story>> GetStoriesNeedingImagesAsync(CancellationToken cancellationToken = default);
    Task<List<Story>> GetLongStoriesAsync(int minWords, CancellationToken cancellationToken = default);
    Task<List<Story>> GetShortStoriesAsync(int maxWords, CancellationToken cancellationToken = default);
    Task<List<Story>> GetStoriesWithExcessiveScenesAsync(int maxScenes, CancellationToken cancellationToken = default);
    
    // Detaillierte Abfragen mit Navigation Properties
    Task<Story?> GetWithFullDetailsAsync(Guid storyId, CancellationToken cancellationToken = default);
    Task<Story?> GetWithScenesAndLearningGoalsAsync(Guid storyId, CancellationToken cancellationToken = default);
    Task<List<Story>> GetWithStatisticsAsync(List<Guid> storyIds, CancellationToken cancellationToken = default);
    
    // Batch-Operationen
    Task<List<Story>> GetStoriesBatchAsync(List<Guid> storyIds, CancellationToken cancellationToken = default);
    Task UpdateMultipleStoriesAsync(List<Story> stories, CancellationToken cancellationToken = default);
    Task ArchiveMultipleStoriesAsync(List<Guid> storyIds, Guid archivedByUserId, string reason, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository Interface für Story Reading Progress und Interactions
/// Spezialisiert auf Lesefortschritt und Benutzerinteraktionen
/// </summary>
public interface IStoryInteractionRepository
{
    // Reading Progress
    Task<StoryReadingProgress?> GetReadingProgressAsync(Guid storyId, Guid userId, CancellationToken cancellationToken = default);
    Task<List<StoryReadingProgress>> GetReadingProgressByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SaveReadingProgressAsync(StoryReadingProgress progress, CancellationToken cancellationToken = default);
    Task<List<StoryReadingProgress>> GetInProgressReadingsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<StoryReadingProgress>> GetCompletedReadingsAsync(Guid userId, DateTime? since = null, CancellationToken cancellationToken = default);
    
    // Story Ratings und Reviews
    Task<StoryRating?> GetStoryRatingAsync(Guid storyId, Guid userId, CancellationToken cancellationToken = default);
    Task<List<StoryRating>> GetStoryRatingsAsync(Guid storyId, CancellationToken cancellationToken = default);
    Task SaveStoryRatingAsync(StoryRating rating, CancellationToken cancellationToken = default);
    Task<double> GetAverageRatingAsync(Guid storyId, CancellationToken cancellationToken = default);
    Task<Dictionary<int, int>> GetRatingDistributionAsync(Guid storyId, CancellationToken cancellationToken = default);
    
    // Story Statistics
    Task<StoryStatistics?> GetStoryStatisticsAsync(Guid storyId, CancellationToken cancellationToken = default);
    Task UpdateStoryStatisticsAsync(StoryStatistics statistics, CancellationToken cancellationToken = default);
    Task IncrementStoryViewAsync(Guid storyId, Guid userId, CancellationToken cancellationToken = default);
    Task RecordStoryShareAsync(Guid storyId, Guid sharedByUserId, string shareMethod, CancellationToken cancellationToken = default);
    
    // Learning Goal Progress
    Task<List<LearningGoalProgress>> GetLearningGoalProgressAsync(Guid storyId, Guid userId, CancellationToken cancellationToken = default);
    Task SaveLearningGoalProgressAsync(LearningGoalProgress progress, CancellationToken cancellationToken = default);
    Task<List<LearningGoalProgress>> GetCompletedLearningGoalsAsync(Guid userId, DateTime? since = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository Interface für Character Memory Management
/// Spezialisiert auf das komplexe Memory-System
/// </summary>
public interface ICharacterMemoryRepository
{
    // Memory CRUD
    Task<CharacterMemory?> GetMemoryAsync(Guid memoryId, CancellationToken cancellationToken = default);
    Task<List<CharacterMemory>> GetMemoriesByCharacterAsync(Guid characterId, int? limit = null, CancellationToken cancellationToken = default);
    Task<CharacterMemory> SaveMemoryAsync(CharacterMemory memory, CancellationToken cancellationToken = default);
    Task DeleteMemoryAsync(Guid memoryId, CancellationToken cancellationToken = default);
    
    // Memory Queries
    Task<List<CharacterMemory>> GetMemoriesByTypeAsync(Guid characterId, MemoryType memoryType, CancellationToken cancellationToken = default);
    Task<List<CharacterMemory>> GetMemoriesByImportanceAsync(Guid characterId, MemoryImportance importance, CancellationToken cancellationToken = default);
    Task<List<CharacterMemory>> GetRecentMemoriesAsync(Guid characterId, int count = 10, CancellationToken cancellationToken = default);
    Task<List<CharacterMemory>> GetOldestMemoriesAsync(Guid characterId, int count = 10, CancellationToken cancellationToken = default);
    
    // Memory Search
    Task<List<CharacterMemory>> SearchMemoriesAsync(Guid characterId, string query, CancellationToken cancellationToken = default);
    Task<List<CharacterMemory>> GetMemoriesByTagsAsync(Guid characterId, List<string> tags, CancellationToken cancellationToken = default);
    Task<List<CharacterMemory>> GetMemoriesLinkedToStoryAsync(Guid storyId, CancellationToken cancellationToken = default);
    
    // Memory Consolidation
    Task<List<CharacterMemory>> GetMemoriesForConsolidationAsync(Guid characterId, CancellationToken cancellationToken = default);
    Task<List<CharacterMemory>> GetRelatedMemoriesAsync(Guid memoryId, float similarityThreshold = 0.7f, CancellationToken cancellationToken = default);
    Task ConsolidateMemoriesAsync(List<Guid> memoryIds, CharacterMemory consolidatedMemory, CancellationToken cancellationToken = default);
    
    // Memory Analytics
    Task<int> GetMemoryCountByCharacterAsync(Guid characterId, CancellationToken cancellationToken = default);
    Task<Dictionary<MemoryType, int>> GetMemoryCountByTypeAsync(Guid characterId, CancellationToken cancellationToken = default);
    Task<List<CharacterMemory>> GetMostAccessedMemoriesAsync(Guid characterId, int count = 10, CancellationToken cancellationToken = default);
    Task<List<CharacterMemory>> GetLeastAccessedMemoriesAsync(Guid characterId, DateTime olderThan, CancellationToken cancellationToken = default);
}

/// <summary>
/// Zusätzliche Value Objects für Repositories
/// </summary>
public class StoryReadingProgress
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StoryId { get; set; }
    public Guid UserId { get; set; }
    public Guid CharacterUsedId { get; set; }
    public int CurrentScene { get; set; }
    public int TotalScenes { get; set; }
    public TimeSpan ReadingTimeElapsed { get; set; }
    public DateTime LastReadAt { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Dictionary<string, object> InteractionData { get; set; } = new();
}

public class StoryRating
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StoryId { get; set; }
    public Guid UserId { get; set; }
    public int Rating { get; set; } // 1-5
    public string? ReviewComment { get; set; }
    public List<string> PositiveAspects { get; set; } = new();
    public List<string> NegativeAspects { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class StoryStatistics
{
    public Guid StoryId { get; set; }
    public int TotalReads { get; set; }
    public int UniqueReaders { get; set; }
    public double AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public int ShareCount { get; set; }
    public TimeSpan AverageReadingTime { get; set; }
    public Dictionary<string, int> AgeGroupPopularity { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class LearningGoalProgress
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LearningGoalId { get; set; }
    public Guid UserId { get; set; }
    public Guid StoryId { get; set; }
    public float ProgressPercentage { get; set; }
    public LearningGoalStatus Status { get; set; }
    public List<string> EvidenceOfLearning { get; set; } = new();
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int AttemptsCount { get; set; }
}