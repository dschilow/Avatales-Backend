using Microsoft.Extensions.Logging;
using Avatales.Domain.Characters.Entities;
using Avatales.Domain.Characters.ValueObjects;
using Avatales.Application.Interfaces.Repositories;
using Avatales.Application.Common.Interfaces;
using Avatales.Shared.Models;

namespace Avatales.Domain.Characters.Services;

/// <summary>
/// Domain Service für das hierarchische Character Memory System
/// Verwaltet Erinnerungen, Konsolidierung und Memory-Zyklen
/// </summary>
public interface ICharacterMemoryService
{
    // Memory-Management
    Task<CharacterMemory> AddMemoryAsync(Character character, CharacterMemory memory, CancellationToken cancellationToken = default);
    Task<List<CharacterMemory>> GetRelevantMemoriesAsync(Character character, string context, int maxMemories = 10, CancellationToken cancellationToken = default);
    Task<CharacterMemory> ConsolidateMemoriesAsync(Character character, List<CharacterMemory> memoriesToConsolidate, string consolidationReason, CancellationToken cancellationToken = default);
    
    // Memory-Lifecycle
    Task ProcessMemoryDecayAsync(Character character, CancellationToken cancellationToken = default);
    Task<List<CharacterMemory>> IdentifyMemoriesForConsolidationAsync(Character character, CancellationToken cancellationToken = default);
    Task ArchiveOldMemoriesAsync(Character character, int maxActiveMemories = 50, CancellationToken cancellationToken = default);
    
    // Memory-Queries
    Task<List<CharacterMemory>> SearchMemoriesAsync(Character character, string query, MemoryType? type = null, CancellationToken cancellationToken = default);
    Task<Dictionary<string, List<CharacterMemory>>> GetMemoriesByTopicAsync(Character character, CancellationToken cancellationToken = default);
    Task<float> CalculateMemorySimilarityAsync(CharacterMemory memory1, CharacterMemory memory2, CancellationToken cancellationToken = default);
    
    // Memory-Analytics
    Task<MemoryAnalysis> AnalyzeMemoryPatternsAsync(Character character, CancellationToken cancellationToken = default);
    Task<List<CharacterMemory>> GetMostInfluentialMemoriesAsync(Character character, int count = 5, CancellationToken cancellationToken = default);
}

public class CharacterMemoryService : ICharacterMemoryService
{
    private readonly ICharacterMemoryRepository _memoryRepository;
    private readonly ILogger<CharacterMemoryService> _logger;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAIService _aiService;
    private readonly ICacheService _cacheService;

    public CharacterMemoryService(
        ICharacterMemoryRepository memoryRepository,
        ILogger<CharacterMemoryService> logger,
        IDateTimeProvider dateTimeProvider,
        IAIService aiService,
        ICacheService cacheService)
    {
        _memoryRepository = memoryRepository;
        _logger = logger;
        _dateTimeProvider = dateTimeProvider;
        _aiService = aiService;
        _cacheService = cacheService;
    }

    public async Task<CharacterMemory> AddMemoryAsync(Character character, CharacterMemory memory, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding memory '{Title}' to character {CharacterId}", memory.Title, character.Id);

        try
        {
            // 1. Validiere Memory
            ValidateMemory(memory);

            // 2. Prüfe auf Duplikate
            var existingMemories = await _memoryRepository.GetMemoriesByCharacterAsync(character.Id, cancellationToken: cancellationToken);
            var duplicateMemory = await FindDuplicateMemoryAsync(memory, existingMemories, cancellationToken);
            
            if (duplicateMemory != null)
            {
                _logger.LogInformation("Found duplicate memory, consolidating instead of adding new one");
                return await ConsolidateMemoriesAsync(character, new List<CharacterMemory> { duplicateMemory, memory }, "Duplicate memory consolidation", cancellationToken);
            }

            // 3. Speichere Memory
            var savedMemory = await _memoryRepository.SaveMemoryAsync(memory, cancellationToken);

            // 4. Prüfe ob Konsolidierung erforderlich ist
            await CheckForAutoConsolidationAsync(character, savedMemory, cancellationToken);

            // 5. Prüfe Memory-Limits
            await EnforceMemoryLimitsAsync(character, cancellationToken);

            _logger.LogInformation("Successfully added memory '{Title}' to character {CharacterId}", memory.Title, character.Id);
            return savedMemory;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding memory '{Title}' to character {CharacterId}", memory.Title, character.Id);
            throw;
        }
    }

    public async Task<List<CharacterMemory>> GetRelevantMemoriesAsync(Character character, string context, int maxMemories = 10, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving relevant memories for character {CharacterId} with context: {Context}", character.Id, context);

        try
        {
            // Cache-Key für diesen Context
            var cacheKey = $"relevant_memories:{character.Id}:{context.GetHashCode()}:{maxMemories}";
            var cachedMemories = await _cacheService.GetAsync<List<CharacterMemory>>(cacheKey, cancellationToken);
            
            if (cachedMemories != null)
            {
                return cachedMemories;
            }

            // 1. Alle Memories des Charakters laden
            var allMemories = await _memoryRepository.GetMemoriesByCharacterAsync(character.Id, cancellationToken: cancellationToken);

            if (!allMemories.Any())
            {
                return new List<CharacterMemory>();
            }

            // 2. Relevanz-Scoring mit AI
            var scoredMemories = await ScoreMemoryRelevanceAsync(allMemories, context, cancellationToken);

            // 3. Nach Relevanz und Wichtigkeit sortieren
            var relevantMemories = scoredMemories
                .OrderByDescending(m => m.RelevanceScore)
                .ThenByDescending(m => m.Memory.Importance)
                .ThenByDescending(m => m.Memory.AccessCount)
                .Take(maxMemories)
                .Select(m => m.Memory)
                .ToList();

            // 4. Access Count erhöhen
            foreach (var memory in relevantMemories)
            {
                memory.IncrementAccessCount();
                await _memoryRepository.SaveMemoryAsync(memory, cancellationToken);
            }

            // 5. Cache für 5 Minuten
            await _cacheService.SetAsync(cacheKey, relevantMemories, TimeSpan.FromMinutes(5), cancellationToken);

            _logger.LogDebug("Retrieved {Count} relevant memories for character {CharacterId}", relevantMemories.Count, character.Id);
            return relevantMemories;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving relevant memories for character {CharacterId}", character.Id);
            throw;
        }
    }

    public async Task<CharacterMemory> ConsolidateMemoriesAsync(Character character, List<CharacterMemory> memoriesToConsolidate, string consolidationReason, CancellationToken cancellationToken = default)
    {
        if (memoriesToConsolidate.Count < 2)
            throw new ArgumentException("At least 2 memories required for consolidation");

        _logger.LogInformation("Consolidating {Count} memories for character {CharacterId}", memoriesToConsolidate.Count, character.Id);

        try
        {
            // 1. Generiere konsolidiertes Memory mit AI
            var consolidatedContent = await GenerateConsolidatedMemoryAsync(memoriesToConsolidate, consolidationReason, cancellationToken);

            // 2. Erstelle neues konsolidiertes Memory
            var consolidatedMemory = new CharacterMemory(
                title: consolidatedContent.Title,
                summary: consolidatedContent.Summary,
                memoryType: DetermineConsolidatedType(memoriesToConsolidate),
                importance: CalculateConsolidatedImportance(memoriesToConsolidate),
                fullContent: consolidatedContent.FullContent);

            // 3. Übertrage Metadaten
            consolidatedMemory.AddTags(memoriesToConsolidate.SelectMany(m => m.Tags).Distinct());
            consolidatedMemory.AddAssociatedCharacters(memoriesToConsolidate.SelectMany(m => m.AssociatedCharacters).Distinct());
            consolidatedMemory.AddEmotionalContext(memoriesToConsolidate.SelectMany(m => m.EmotionalContext).Distinct());

            // 4. Verknüpfe ursprüngliche Memories
            foreach (var originalMemory in memoriesToConsolidate)
            {
                consolidatedMemory.LinkMemory(originalMemory.Id);
            }

            // 5. Speichere konsolidiertes Memory
            var savedConsolidatedMemory = await _memoryRepository.SaveMemoryAsync(consolidatedMemory, cancellationToken);

            // 6. Markiere ursprüngliche Memories als konsolidiert
            foreach (var originalMemory in memoriesToConsolidate)
            {
                originalMemory.MarkAsConsolidated(savedConsolidatedMemory.Id);
                await _memoryRepository.SaveMemoryAsync(originalMemory, cancellationToken);
            }

            _logger.LogInformation("Successfully consolidated {Count} memories into new memory '{Title}'", 
                memoriesToConsolidate.Count, consolidatedMemory.Title);

            return savedConsolidatedMemory;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consolidating memories for character {CharacterId}", character.Id);
            throw;
        }
    }

    public async Task ProcessMemoryDecayAsync(Character character, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Processing memory decay for character {CharacterId}", character.Id);

        try
        {
            var memories = await _memoryRepository.GetMemoriesByCharacterAsync(character.Id, cancellationToken: cancellationToken);
            var currentTime = _dateTimeProvider.UtcNow;

            foreach (var memory in memories)
            {
                // Berechne Decay basierend auf Zeit und Eigenschaften
                var daysSinceLastAccess = (currentTime - (memory.LastAccessedAt ?? memory.CreatedAt)).TotalDays;
                var decayFactor = CalculateDecayFactor(memory, daysSinceLastAccess);

                if (decayFactor > 0.5) // Memory ist stark verfallen
                {
                    if (memory.Importance <= 3 && memory.DecayResistance <= 2)
                    {
                        // Schwache Memories archivieren
                        await ArchiveMemoryAsync(memory, "Natural decay", cancellationToken);
                    }
                    else if (memory.AccessCount == 0 && daysSinceLastAccess > 30)
                    {
                        // Nie zugegriffene alte Memories archivieren
                        await ArchiveMemoryAsync(memory, "No access for 30 days", cancellationToken);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing memory decay for character {CharacterId}", character.Id);
            throw;
        }
    }

    public async Task<List<CharacterMemory>> IdentifyMemoriesForConsolidationAsync(Character character, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Identifying memories for consolidation for character {CharacterId}", character.Id);

        try
        {
            var memories = await _memoryRepository.GetMemoriesByCharacterAsync(character.Id, cancellationToken: cancellationToken);
            var consolidationCandidates = new List<CharacterMemory>();

            // Finde ähnliche Memories
            for (int i = 0; i < memories.Count; i++)
            {
                for (int j = i + 1; j < memories.Count; j++)
                {
                    var similarity = await CalculateMemorySimilarityAsync(memories[i], memories[j], cancellationToken);
                    
                    if (similarity > 0.7f) // Hohe Ähnlichkeit
                    {
                        consolidationCandidates.Add(memories[i]);
                        consolidationCandidates.Add(memories[j]);
                    }
                }
            }

            return consolidationCandidates.Distinct().ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error identifying memories for consolidation for character {CharacterId}", character.Id);
            throw;
        }
    }

    public async Task ArchiveOldMemoriesAsync(Character character, int maxActiveMemories = 50, CancellationToken cancellationToken = default)
    {
        var memories = await _memoryRepository.GetMemoriesByCharacterAsync(character.Id, cancellationToken: cancellationToken);
        
        if (memories.Count <= maxActiveMemories)
            return;

        // Sortiere nach Wichtigkeit und letztem Zugriff
        var memoriesToArchive = memories
            .Where(m => !m.IsConsolidated && m.ImportanceLevel != MemoryImportance.Core)
            .OrderBy(m => m.Importance)
            .ThenBy(m => m.LastAccessedAt ?? m.CreatedAt)
            .Take(memories.Count - maxActiveMemories)
            .ToList();

        foreach (var memory in memoriesToArchive)
        {
            await ArchiveMemoryAsync(memory, "Automatic archival due to memory limit", cancellationToken);
        }
    }

    public async Task<List<CharacterMemory>> SearchMemoriesAsync(Character character, string query, MemoryType? type = null, CancellationToken cancellationToken = default)
    {
        var memories = await _memoryRepository.GetMemoriesByCharacterAsync(character.Id, cancellationToken: cancellationToken);
        
        // Filtere nach Type falls angegeben
        if (type.HasValue)
        {
            memories = memories.Where(m => m.MemoryType == type.Value).ToList();
        }

        // Einfache Textsuche (könnte mit AI verbessert werden)
        var filteredMemories = memories
            .Where(m => 
                m.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                m.Summary.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                m.FullContent.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                m.Tags.Any(tag => tag.Contains(query, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(m => CalculateSearchRelevance(m, query))
            .ToList();

        return filteredMemories;
    }

    public async Task<Dictionary<string, List<CharacterMemory>>> GetMemoriesByTopicAsync(Character character, CancellationToken cancellationToken = default)
    {
        var memories = await _memoryRepository.GetMemoriesByCharacterAsync(character.Id, cancellationToken: cancellationToken);
        
        // Gruppiere nach Tags und extrahierte Topics
        var memoryGroups = new Dictionary<string, List<CharacterMemory>>();
        
        foreach (var memory in memories)
        {
            // Gruppiere nach Tags
            foreach (var tag in memory.Tags)
            {
                if (!memoryGroups.ContainsKey(tag))
                    memoryGroups[tag] = new List<CharacterMemory>();
                memoryGroups[tag].Add(memory);
            }
            
            // Gruppiere nach Memory Type
            var typeKey = memory.MemoryType.ToString();
            if (!memoryGroups.ContainsKey(typeKey))
                memoryGroups[typeKey] = new List<CharacterMemory>();
            memoryGroups[typeKey].Add(memory);
        }

        return memoryGroups;
    }

    public async Task<float> CalculateMemorySimilarityAsync(CharacterMemory memory1, CharacterMemory memory2, CancellationToken cancellationToken = default)
    {
        // Einfacher Ähnlichkeits-Algorithmus (könnte mit AI verbessert werden)
        float similarity = 0f;

        // Tag-Überschneidung
        var commonTags = memory1.Tags.Intersect(memory2.Tags).Count();
        var totalTags = memory1.Tags.Union(memory2.Tags).Count();
        if (totalTags > 0)
            similarity += (float)commonTags / totalTags * 0.3f;

        // Gleicher Memory-Type
        if (memory1.MemoryType == memory2.MemoryType)
            similarity += 0.2f;

        // Zeitliche Nähe
        var timeDifference = Math.Abs((memory1.OccurredAt - memory2.OccurredAt).TotalDays);
        if (timeDifference < 7) // Innerhalb einer Woche
            similarity += 0.2f;

        // Content-Ähnlichkeit (einfach)
        var contentSimilarity = CalculateTextSimilarity(memory1.Summary, memory2.Summary);
        similarity += contentSimilarity * 0.3f;

        return Math.Min(1.0f, similarity);
    }

    public async Task<MemoryAnalysis> AnalyzeMemoryPatternsAsync(Character character, CancellationToken cancellationToken = default)
    {
        var memories = await _memoryRepository.GetMemoriesByCharacterAsync(character.Id, cancellationToken: cancellationToken);
        
        var analysis = new MemoryAnalysis
        {
            TotalMemories = memories.Count,
            ConsolidatedMemories = memories.Count(m => m.IsConsolidated),
            MemoryTypeDistribution = memories.GroupBy(m => m.MemoryType).ToDictionary(g => g.Key.ToString(), g => g.Count()),
            ImportanceDistribution = memories.GroupBy(m => m.Importance).ToDictionary(g => g.Key, g => g.Count()),
            AverageImportance = memories.Any() ? memories.Average(m => m.Importance) : 0,
            MostFrequentTags = memories.SelectMany(m => m.Tags).GroupBy(t => t).OrderByDescending(g => g.Count()).Take(10).ToDictionary(g => g.Key, g => g.Count()),
            OldestMemory = memories.OrderBy(m => m.CreatedAt).FirstOrDefault()?.CreatedAt,
            NewestMemory = memories.OrderByDescending(m => m.CreatedAt).FirstOrDefault()?.CreatedAt,
            MostAccessedMemory = memories.OrderByDescending(m => m.AccessCount).FirstOrDefault(),
            LeastAccessedMemories = memories.Where(m => m.AccessCount == 0).Count(),
            RecommendedActions = GenerateRecommendedActions(memories)
        };

        return analysis;
    }

    public async Task<List<CharacterMemory>> GetMostInfluentialMemoriesAsync(Character character, int count = 5, CancellationToken cancellationToken = default)
    {
        var memories = await _memoryRepository.GetMemoriesByCharacterAsync(character.Id, cancellationToken: cancellationToken);
        
        return memories
            .OrderByDescending(m => CalculateInfluenceScore(m))
            .Take(count)
            .ToList();
    }

    // Private Helper Methods

    private void ValidateMemory(CharacterMemory memory)
    {
        if (string.IsNullOrWhiteSpace(memory.Title))
            throw new ArgumentException("Memory title cannot be empty");
        
        if (string.IsNullOrWhiteSpace(memory.Summary))
            throw new ArgumentException("Memory summary cannot be empty");
        
        if (memory.Importance < 1 || memory.Importance > 10)
            throw new ArgumentException("Memory importance must be between 1 and 10");
    }

    private async Task<CharacterMemory?> FindDuplicateMemoryAsync(CharacterMemory newMemory, List<CharacterMemory> existingMemories, CancellationToken cancellationToken)
    {
        foreach (var existing in existingMemories)
        {
            var similarity = await CalculateMemorySimilarityAsync(newMemory, existing, cancellationToken);
            if (similarity > 0.9f) // Sehr hohe Ähnlichkeit = Duplikat
            {
                return existing;
            }
        }
        return null;
    }

    private async Task CheckForAutoConsolidationAsync(Character character, CharacterMemory newMemory, CancellationToken cancellationToken)
    {
        // Finde ähnliche Memories für automatische Konsolidierung
        var similarMemories = await FindSimilarMemoriesAsync(character.Id, newMemory, cancellationToken);
        
        if (similarMemories.Count >= 2) // Mindestens 2 ähnliche + neue = 3 total
        {
            similarMemories.Add(newMemory);
            await ConsolidateMemoriesAsync(character, similarMemories, "Automatic consolidation of similar memories", cancellationToken);
        }
    }

    private async Task<List<CharacterMemory>> FindSimilarMemoriesAsync(Guid characterId, CharacterMemory targetMemory, CancellationToken cancellationToken)
    {
        var memories = await _memoryRepository.GetMemoriesByCharacterAsync(characterId, cancellationToken: cancellationToken);
        var similarMemories = new List<CharacterMemory>();

        foreach (var memory in memories)
        {
            if (memory.Id != targetMemory.Id)
            {
                var similarity = await CalculateMemorySimilarityAsync(targetMemory, memory, cancellationToken);
                if (similarity > 0.6f) // Ähnlich genug für Konsolidierung
                {
                    similarMemories.Add(memory);
                }
            }
        }

        return similarMemories;
    }

    private async Task EnforceMemoryLimitsAsync(Character character, CancellationToken cancellationToken)
    {
        var memoryCount = await _memoryRepository.GetMemoryCountByCharacterAsync(character.Id, cancellationToken);
        
        if (memoryCount > ApplicationConstants.Characters.MaxMemoriesPerCharacter)
        {
            await ArchiveOldMemoriesAsync(character, ApplicationConstants.Characters.MaxMemoriesPerCharacter, cancellationToken);
        }
    }

    private async Task<List<ScoredMemory>> ScoreMemoryRelevanceAsync(List<CharacterMemory> memories, string context, CancellationToken cancellationToken)
    {
        var scoredMemories = new List<ScoredMemory>();

        foreach (var memory in memories)
        {
            var score = CalculateBasicRelevanceScore(memory, context);
            
            // TODO: Hier könnte AI-basierte Relevanz-Bewertung eingesetzt werden
            
            scoredMemories.Add(new ScoredMemory { Memory = memory, RelevanceScore = score });
        }

        return scoredMemories;
    }

    private float CalculateBasicRelevanceScore(CharacterMemory memory, string context)
    {
        float score = 0f;

        // Keyword-Matching
        var contextWords = context.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var memoryText = $"{memory.Title} {memory.Summary} {string.Join(" ", memory.Tags)}".ToLower();
        
        foreach (var word in contextWords)
        {
            if (memoryText.Contains(word))
                score += 0.1f;
        }

        // Importance bonus
        score += memory.Importance / 10f * 0.3f;

        // Recent access bonus
        if (memory.LastAccessedAt.HasValue && (DateTime.UtcNow - memory.LastAccessedAt.Value).TotalDays < 7)
            score += 0.2f;

        return Math.Min(1.0f, score);
    }

    private async Task<ConsolidatedMemoryContent> GenerateConsolidatedMemoryAsync(List<CharacterMemory> memories, string reason, CancellationToken cancellationToken)
    {
        // TODO: Hier würde AI verwendet um eine sinnvolle Konsolidierung zu erstellen
        // Für jetzt eine einfache Implementierung
        
        var titles = string.Join(", ", memories.Select(m => m.Title));
        var summaries = string.Join(". ", memories.Select(m => m.Summary));
        
        return new ConsolidatedMemoryContent
        {
            Title = $"Consolidated Memory: {titles}",
            Summary = $"Combined memory from {memories.Count} related experiences: {summaries}",
            FullContent = string.Join("\n\n", memories.Select(m => $"{m.Title}: {m.FullContent}"))
        };
    }

    private MemoryType DetermineConsolidatedType(List<CharacterMemory> memories)
    {
        // Nimm den häufigsten Type oder den wichtigsten
        return memories.GroupBy(m => m.MemoryType)
                      .OrderByDescending(g => g.Count())
                      .ThenByDescending(g => g.Max(m => m.Importance))
                      .First().Key;
    }

    private int CalculateConsolidatedImportance(List<CharacterMemory> memories)
    {
        // Nimm die höchste Wichtigkeit, aber maximal 10
        return Math.Min(10, memories.Max(m => m.Importance) + 1);
    }

    private float CalculateDecayFactor(CharacterMemory memory, double daysSinceLastAccess)
    {
        var baseFactor = (float)(daysSinceLastAccess / 30.0); // 30 Tage für vollständigen Decay
        var resistanceFactor = memory.DecayResistance / 10f;
        var importanceFactor = memory.Importance / 10f;
        
        return Math.Max(0f, baseFactor - resistanceFactor - importanceFactor);
    }

    private async Task ArchiveMemoryAsync(CharacterMemory memory, string reason, CancellationToken cancellationToken)
    {
        memory.Archive(reason);
        await _memoryRepository.SaveMemoryAsync(memory, cancellationToken);
        _logger.LogDebug("Archived memory '{Title}': {Reason}", memory.Title, reason);
    }

    private float CalculateTextSimilarity(string text1, string text2)
    {
        // Einfache Jaccard-Ähnlichkeit
        var words1 = text1.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var words2 = text2.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        
        var intersection = words1.Intersect(words2).Count();
        var union = words1.Union(words2).Count();
        
        return union > 0 ? (float)intersection / union : 0f;
    }

    private int CalculateSearchRelevance(CharacterMemory memory, string query)
    {
        var score = 0;
        var queryLower = query.ToLower();
        
        if (memory.Title.ToLower().Contains(queryLower)) score += 3;
        if (memory.Summary.ToLower().Contains(queryLower)) score += 2;
        if (memory.Tags.Any(tag => tag.ToLower().Contains(queryLower))) score += 2;
        if (memory.FullContent.ToLower().Contains(queryLower)) score += 1;
        
        return score;
    }

    private float CalculateInfluenceScore(CharacterMemory memory)
    {
        return (memory.Importance * 0.4f) + 
               (memory.AccessCount * 0.3f) + 
               (memory.DecayResistance * 0.2f) + 
               (memory.LinkedMemoryIds.Count * 0.1f);
    }

    private List<string> GenerateRecommendedActions(List<CharacterMemory> memories)
    {
        var recommendations = new List<string>();
        
        if (memories.Count > 80)
            recommendations.Add("Consider consolidating old memories to improve performance");
        
        if (memories.Count(m => m.AccessCount == 0) > 20)
            recommendations.Add("Archive unused memories to reduce clutter");
        
        if (memories.GroupBy(m => m.MemoryType).Count() == 1)
            recommendations.Add("Diversify memory types for richer character development");
        
        return recommendations;
    }
}

// Supporting Classes

public class ScoredMemory
{
    public CharacterMemory Memory { get; set; } = null!;
    public float RelevanceScore { get; set; }
}

public class ConsolidatedMemoryContent
{
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string FullContent { get; set; } = string.Empty;
}

public class MemoryAnalysis
{
    public int TotalMemories { get; set; }
    public int ConsolidatedMemories { get; set; }
    public Dictionary<string, int> MemoryTypeDistribution { get; set; } = new();
    public Dictionary<int, int> ImportanceDistribution { get; set; } = new();
    public double AverageImportance { get; set; }
    public Dictionary<string, int> MostFrequentTags { get; set; } = new();
    public DateTime? OldestMemory { get; set; }
    public DateTime? NewestMemory { get; set; }
    public CharacterMemory? MostAccessedMemory { get; set; }
    public int LeastAccessedMemories { get; set; }
    public List<string> RecommendedActions { get; set; } = new();
}

// Extensions für CharacterMemory
public static class CharacterMemoryExtensions
{
    public static void IncrementAccessCount(this CharacterMemory memory)
    {
        // TODO: Diese Funktionalität müsste in CharacterMemory ValueObject implementiert werden
    }

    public static void AddTags(this CharacterMemory memory, IEnumerable<string> tags)
    {
        // TODO: Diese Funktionalität müsste in CharacterMemory ValueObject implementiert werden
    }

    public static void AddAssociatedCharacters(this CharacterMemory memory, IEnumerable<string> characters)
    {
        // TODO: Diese Funktionalität müsste in CharacterMemory ValueObject implementiert werden
    }

    public static void AddEmotionalContext(this CharacterMemory memory, IEnumerable<string> emotions)
    {
        // TODO: Diese Funktionalität müsste in CharacterMemory ValueObject implementiert werden
    }

    public static void LinkMemory(this CharacterMemory memory, Guid memoryId)
    {
        // TODO: Diese Funktionalität müsste in CharacterMemory ValueObject implementiert werden
    }

    public static void MarkAsConsolidated(this CharacterMemory memory, Guid consolidatedMemoryId)
    {
        // TODO: Diese Funktionalität müsste in CharacterMemory ValueObject implementiert werden
    }

    public static void Archive(this CharacterMemory memory, string reason)
    {
        // TODO: Diese Funktionalität müsste in CharacterMemory ValueObject implementiert werden
    }
}