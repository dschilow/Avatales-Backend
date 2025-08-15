using Microsoft.Extensions.Logging;
using AutoMapper;
using Avatales.Domain.Characters.Entities;
using Avatales.Domain.Characters.Services;
using Avatales.Domain.Characters.ValueObjects;
using Avatales.Application.Interfaces.Repositories;
using Avatales.Application.Common.Interfaces;
using Avatales.Application.Characters.DTOs;
using Avatales.Shared.Models;

namespace Avatales.Application.Characters.Services;

/// <summary>
/// Application Service für Character-Management
/// Orchestriert Domain Services und Repository-Operationen
/// </summary>
public interface ICharacterApplicationService : IApplicationService
{
    // Character CRUD
    Task<CharacterDetailDto> CreateCharacterAsync(CreateCharacterDto dto, CancellationToken cancellationToken = default);
    Task<CharacterDetailDto> AdoptCharacterAsync(AdoptCharacterDto dto, CancellationToken cancellationToken = default);
    Task<CharacterDetailDto> UpdateCharacterAsync(UpdateCharacterDto dto, CancellationToken cancellationToken = default);
    Task DeleteCharacterAsync(Guid characterId, string reason, CancellationToken cancellationToken = default);
    
    // Character Development
    Task<List<TraitDevelopmentResultDto>> AddStoryExperienceAsync(Guid characterId, Guid storyId, int experienceGained, Dictionary<string, object> context, CancellationToken cancellationToken = default);
    Task<TraitDevelopmentResultDto> ReinforceTraitAsync(Guid characterId, CharacterTraitType traitType, string reason, float multiplier = 1.5f, CancellationToken cancellationToken = default);
    Task ProcessCharacterInteractionAsync(Guid characterId, string interactionType, Dictionary<string, object> data, CancellationToken cancellationToken = default);
    
    // Memory Management
    Task<CharacterMemoryDto> AddMemoryAsync(Guid characterId, AddCharacterMemoryDto dto, CancellationToken cancellationToken = default);
    Task<CharacterMemoryDto> ConsolidateMemoriesAsync(Guid characterId, ConsolidateCharacterMemoriesDto dto, CancellationToken cancellationToken = default);
    Task<List<CharacterMemoryDto>> GetRelevantMemoriesAsync(Guid characterId, string context, int maxResults = 10, CancellationToken cancellationToken = default);
    
    // Character Analytics
    Task<CharacterDetailDto> GetCharacterDetailsAsync(Guid characterId, bool includeMemories = true, CancellationToken cancellationToken = default);
    Task<List<CharacterRecommendationDto>> GetCharacterRecommendationsAsync(Guid characterId, int maxResults = 10, CancellationToken cancellationToken = default);
    Task<MemoryAnalysis> AnalyzeCharacterMemoriesAsync(Guid characterId, CancellationToken cancellationToken = default);
    
    // Character Community
    Task ShareCharacterToCommunityAsync(Guid characterId, ShareCharacterDto dto, CancellationToken cancellationToken = default);
    Task<List<CharacterCommunityDto>> GetCommunityCharactersAsync(CharacterSearchDto searchDto, CancellationToken cancellationToken = default);
    Task<List<CharacterDto>> GetUserCharactersAsync(Guid userId, bool includeInactive = false, CancellationToken cancellationToken = default);
    
    // Batch Operations
    Task<List<CharacterDto>> ProcessBatchOperationAsync(BatchCharacterOperationDto dto, CancellationToken cancellationToken = default);
}

public class CharacterApplicationService : ICharacterApplicationService
{
    private readonly ICharacterRepository _characterRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICharacterMemoryService _memoryService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly ILogger<CharacterApplicationService> _logger;
    private readonly ICacheService _cacheService;
    private readonly IImageGenerationService _imageService;
    private readonly IContentModerationService _moderationService;

    public CharacterApplicationService(
        ICharacterRepository characterRepository,
        IUserRepository userRepository,
        ICharacterMemoryService memoryService,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher eventDispatcher,
        ICurrentUserService currentUserService,
        IMapper mapper,
        ILogger<CharacterApplicationService> logger,
        ICacheService cacheService,
        IImageGenerationService imageService,
        IContentModerationService moderationService)
    {
        _characterRepository = characterRepository;
        _userRepository = userRepository;
        _memoryService = memoryService;
        _unitOfWork = unitOfWork;
        _eventDispatcher = eventDispatcher;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _logger = logger;
        _cacheService = cacheService;
        _imageService = imageService;
        _moderationService = moderationService;
    }

    public async Task<CharacterDetailDto> CreateCharacterAsync(CreateCharacterDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating character '{Name}' for user {UserId}", dto.Name, dto.OwnerId);

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            // 1. Validierungen
            await ValidateCharacterCreationAsync(dto, cancellationToken);

            // 2. Prüfe Benutzer-Limits
            await ValidateUserCharacterLimitsAsync(dto.OwnerId, cancellationToken);

            // 3. Erstelle Character DNA
            var dna = await CreateCharacterDNAAsync(dto, cancellationToken);

            // 4. Erstelle Character Entity
            var character = new Character(
                name: dto.Name,
                description: dto.Description,
                ownerId: dto.OwnerId,
                dna: dna,
                avatarImageUrl: dto.AvatarImageUrl ?? "",
                originalCharacterId: dto.OriginalCharacterId);

            // 5. Generiere Avatar falls angefordert
            if (dto.GenerateAvatar && string.IsNullOrEmpty(dto.AvatarImageUrl))
            {
                var avatarUrl = await GenerateCharacterAvatarAsync(character, cancellationToken);
                character.UpdateAvatar(avatarUrl);
            }

            // 6. Speichere Character
            var savedCharacter = await _characterRepository.AddAsync(character, cancellationToken);

            // 7. Aktualisiere User-Statistiken
            var user = await _userRepository.GetByIdAsync(dto.OwnerId, cancellationToken);
            user?.IncrementCharactersCreated();
            if (user != null)
            {
                await _userRepository.UpdateAsync(user, cancellationToken);
            }

            // 8. Speichere Änderungen
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 9. Dispatch Events
            await _eventDispatcher.DispatchEventsAsync(savedCharacter.DomainEvents, cancellationToken);
            savedCharacter.ClearDomainEvents();

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // 10. Erstelle Welcome Memory
            await CreateWelcomeMemoryAsync(savedCharacter, cancellationToken);

            _logger.LogInformation("Successfully created character {CharacterId} for user {UserId}", 
                savedCharacter.Id, dto.OwnerId);

            return await MapToDetailDtoAsync(savedCharacter, cancellationToken);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to create character '{Name}' for user {UserId}", dto.Name, dto.OwnerId);
            throw;
        }
    }

    public async Task<CharacterDetailDto> AdoptCharacterAsync(AdoptCharacterDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adopting character {OriginalCharacterId} for user {NewOwnerId}", 
            dto.OriginalCharacterId, dto.NewOwnerId);

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            // 1. Lade Original-Character
            var originalCharacter = await _characterRepository.GetWithFullDetailsAsync(dto.OriginalCharacterId, cancellationToken);
            if (originalCharacter == null)
            {
                throw new InvalidOperationException("Original character not found");
            }

            // 2. Prüfe Adoption-Berechtigung
            await ValidateCharacterAdoptionAsync(originalCharacter, dto.NewOwnerId, cancellationToken);

            // 3. Erstelle DNA-Kopie (mit möglichen Anpassungen)
            var newDna = await CreateAdoptedCharacterDNAAsync(originalCharacter.DNA, dto, cancellationToken);

            // 4. Erstelle neuen Character
            var adoptedCharacter = new Character(
                name: dto.NewName,
                description: dto.NewDescription ?? originalCharacter.Description,
                ownerId: dto.NewOwnerId,
                dna: newDna,
                avatarImageUrl: dto.KeepOriginalAppearance ? originalCharacter.AvatarImageUrl : "",
                originalCharacterId: dto.OriginalCharacterId);

            // 5. Übertrage Traits falls gewünscht
            if (dto.KeepOriginalTraits)
            {
                await TransferCharacterTraitsAsync(originalCharacter, adoptedCharacter, cancellationToken);
            }

            // 6. Übertrage Memories falls gewünscht
            if (dto.KeepOriginalMemories)
            {
                await TransferCharacterMemoriesAsync(originalCharacter, adoptedCharacter, cancellationToken);
            }

            // 7. Speichere adopted Character
            var savedCharacter = await _characterRepository.AddAsync(adoptedCharacter, cancellationToken);

            // 8. Aktualisiere Original-Character Statistiken
            originalCharacter.IncrementAdoptionCount();
            await _characterRepository.UpdateAsync(originalCharacter, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _eventDispatcher.DispatchEventsAsync(savedCharacter.DomainEvents, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Successfully adopted character {AdoptedCharacterId} from {OriginalCharacterId}", 
                savedCharacter.Id, dto.OriginalCharacterId);

            return await MapToDetailDtoAsync(savedCharacter, cancellationToken);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to adopt character {OriginalCharacterId}", dto.OriginalCharacterId);
            throw;
        }
    }

    public async Task<CharacterDetailDto> UpdateCharacterAsync(UpdateCharacterDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating character {CharacterId}", dto.CharacterId);

        try
        {
            // 1. Lade Character
            var character = await _characterRepository.GetByIdAsync(dto.CharacterId, cancellationToken);
            if (character == null)
            {
                throw new InvalidOperationException("Character not found");
            }

            // 2. Prüfe Berechtigung
            await ValidateCharacterAccessAsync(character, cancellationToken);

            // 3. Content-Moderation für Namen und Beschreibung
            var moderationResult = await _moderationService.ModerateTextAsync(
                $"{dto.Name} {dto.Description}", 
                new ContentModerationOptions { TargetAge = 6, StrictMode = true }, 
                cancellationToken);

            if (moderationResult.Status != ContentModerationStatus.Approved)
            {
                throw new InvalidOperationException("Content not appropriate for children");
            }

            // 4. Aktualisiere Character
            character.UpdateBasicInfo(dto.Name, dto.Description);

            if (!string.IsNullOrEmpty(dto.AvatarImageUrl))
            {
                character.UpdateAvatar(dto.AvatarImageUrl);
            }

            // 5. Tags aktualisieren falls vorhanden
            if (dto.Tags != null)
            {
                character.UpdateTags(dto.Tags);
            }

            // 6. Speichere
            await _characterRepository.UpdateAsync(character, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 7. Cache invalidieren
            await InvalidateCharacterCacheAsync(character.Id, cancellationToken);

            _logger.LogInformation("Successfully updated character {CharacterId}", dto.CharacterId);

            return await MapToDetailDtoAsync(character, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update character {CharacterId}", dto.CharacterId);
            throw;
        }
    }

    public async Task DeleteCharacterAsync(Guid characterId, string reason, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting character {CharacterId}: {Reason}", characterId, reason);

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var character = await _characterRepository.GetByIdAsync(characterId, cancellationToken);
            if (character == null)
            {
                throw new InvalidOperationException("Character not found");
            }

            await ValidateCharacterAccessAsync(character, cancellationToken);

            // Soft Delete
            character.SoftDelete(reason);
            await _characterRepository.UpdateAsync(character, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // Cache invalidieren
            await InvalidateCharacterCacheAsync(characterId, cancellationToken);

            _logger.LogInformation("Successfully deleted character {CharacterId}", characterId);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to delete character {CharacterId}", characterId);
            throw;
        }
    }

    public async Task<List<TraitDevelopmentResultDto>> AddStoryExperienceAsync(
        Guid characterId, 
        Guid storyId, 
        int experienceGained, 
        Dictionary<string, object> context, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding story experience to character {CharacterId} from story {StoryId}", 
            characterId, storyId);

        try
        {
            var character = await _characterRepository.GetWithTraitsAndMemoriesAsync(characterId, cancellationToken);
            if (character == null)
            {
                throw new InvalidOperationException("Character not found");
            }

            // 1. Extrahiere Trait-Einflüsse aus Context
            var traitInfluences = ExtractTraitInfluencesFromContext(context);

            // 2. Extrahiere Story-Informationen
            var storyContext = ExtractStoryContextFromContext(context);

            // 3. Füge Erfahrung hinzu
            var results = character.AddExperienceFromStory(
                experienceGained, 
                storyContext.NewWords, 
                traitInfluences, 
                storyContext.LearningMoments, 
                storyId);

            // 4. Erstelle Story-Memory
            await CreateStoryExperienceMemoryAsync(character, storyId, storyContext, cancellationToken);

            // 5. Speichere Character
            await _characterRepository.UpdateAsync(character, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 6. Cache invalidieren
            await InvalidateCharacterCacheAsync(characterId, cancellationToken);

            _logger.LogInformation("Successfully added story experience to character {CharacterId}", characterId);

            return _mapper.Map<List<TraitDevelopmentResultDto>>(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add story experience to character {CharacterId}", characterId);
            throw;
        }
    }

    public async Task<TraitDevelopmentResultDto> ReinforceTraitAsync(
        Guid characterId, 
        CharacterTraitType traitType, 
        string reason, 
        float multiplier = 1.5f, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Reinforcing trait {TraitType} for character {CharacterId}: {Reason}", 
            traitType, characterId, reason);

        try
        {
            var character = await _characterRepository.GetWithTraitsAndMemoriesAsync(characterId, cancellationToken);
            if (character == null)
            {
                throw new InvalidOperationException("Character not found");
            }

            var result = character.ReinforceTraitPositively(traitType, reason, multiplier);

            await _characterRepository.UpdateAsync(character, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Cache invalidieren
            await InvalidateCharacterCacheAsync(characterId, cancellationToken);

            _logger.LogInformation("Successfully reinforced trait {TraitType} for character {CharacterId}", 
                traitType, characterId);

            return _mapper.Map<TraitDevelopmentResultDto>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reinforce trait {TraitType} for character {CharacterId}", 
                traitType, characterId);
            throw;
        }
    }

    public async Task ProcessCharacterInteractionAsync(
        Guid characterId, 
        string interactionType, 
        Dictionary<string, object> data, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing interaction '{InteractionType}' for character {CharacterId}", 
            interactionType, characterId);

        try
        {
            var character = await _characterRepository.GetWithTraitsAndMemoriesAsync(characterId, cancellationToken);
            if (character == null)
            {
                throw new InvalidOperationException("Character not found");
            }

            // Verarbeite verschiedene Interaktionstypen
            switch (interactionType.ToLower())
            {
                case "user_praise":
                    await ProcessUserPraiseInteractionAsync(character, data, cancellationToken);
                    break;
                
                case "story_choice":
                    await ProcessStoryChoiceInteractionAsync(character, data, cancellationToken);
                    break;
                
                case "learning_achievement":
                    await ProcessLearningAchievementInteractionAsync(character, data, cancellationToken);
                    break;
                
                case "social_interaction":
                    await ProcessSocialInteractionAsync(character, data, cancellationToken);
                    break;
                
                default:
                    _logger.LogWarning("Unknown interaction type: {InteractionType}", interactionType);
                    return;
            }

            await _characterRepository.UpdateAsync(character, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully processed interaction '{InteractionType}' for character {CharacterId}", 
                interactionType, characterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process interaction '{InteractionType}' for character {CharacterId}", 
                interactionType, characterId);
            throw;
        }
    }

    public async Task<CharacterMemoryDto> AddMemoryAsync(Guid characterId, AddCharacterMemoryDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding memory '{Title}' to character {CharacterId}", dto.Title, characterId);

        try
        {
            var character = await _characterRepository.GetByIdAsync(characterId, cancellationToken);
            if (character == null)
            {
                throw new InvalidOperationException("Character not found");
            }

            await ValidateCharacterAccessAsync(character, cancellationToken);

            // Content-Moderation
            var contentToModerate = $"{dto.Title} {dto.Summary} {dto.FullContent}";
            var moderationResult = await _moderationService.ModerateTextAsync(
                contentToModerate, 
                new ContentModerationOptions { TargetAge = 6, StrictMode = true }, 
                cancellationToken);

            if (moderationResult.Status != ContentModerationStatus.Approved)
            {
                throw new InvalidOperationException("Memory content not appropriate");
            }

            // Erstelle Memory
            var memory = new CharacterMemory(
                title: dto.Title,
                summary: dto.Summary,
                memoryType: dto.MemoryType,
                importance: dto.Importance,
                occurredAt: dto.OccurredAt,
                storyId: dto.StoryId,
                fullContent: dto.FullContent);

            // Füge Tags und Kontext hinzu
            foreach (var tag in dto.Tags)
            {
                memory.AddTag(tag);
            }

            foreach (var character in dto.AssociatedCharacters)
            {
                memory.AddAssociatedCharacter(character);
            }

            foreach (var emotion in dto.EmotionalContext)
            {
                memory.AddEmotionalContext(emotion);
            }

            // Füge Memory über Service hinzu (für intelligente Verarbeitung)
            var savedMemory = await _memoryService.AddMemoryAsync(character, memory, cancellationToken);

            _logger.LogInformation("Successfully added memory '{Title}' to character {CharacterId}", 
                dto.Title, characterId);

            return _mapper.Map<CharacterMemoryDto>(savedMemory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add memory '{Title}' to character {CharacterId}", dto.Title, characterId);
            throw;
        }
    }

    public async Task<CharacterMemoryDto> ConsolidateMemoriesAsync(Guid characterId, ConsolidateCharacterMemoriesDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Consolidating {Count} memories for character {CharacterId}", 
            dto.MemoryIds.Count, characterId);

        try
        {
            var character = await _characterRepository.GetWithTraitsAndMemoriesAsync(characterId, cancellationToken);
            if (character == null)
            {
                throw new InvalidOperationException("Character not found");
            }

            await ValidateCharacterAccessAsync(character, cancellationToken);

            // Lade Memories
            var memoriesToConsolidate = new List<CharacterMemory>();
            foreach (var memoryId in dto.MemoryIds)
            {
                var memory = character.Memories.FirstOrDefault(m => m.Id == memoryId);
                if (memory != null)
                {
                    memoriesToConsolidate.Add(memory);
                }
            }

            if (memoriesToConsolidate.Count < 2)
            {
                throw new InvalidOperationException("At least 2 memories required for consolidation");
            }

            // Konsolidiere über Memory Service
            var consolidatedMemory = await _memoryService.ConsolidateMemoriesAsync(
                character, 
                memoriesToConsolidate, 
                dto.ConsolidationReason, 
                cancellationToken);

            _logger.LogInformation("Successfully consolidated {Count} memories into '{Title}' for character {CharacterId}", 
                dto.MemoryIds.Count, consolidatedMemory.Title, characterId);

            return _mapper.Map<CharacterMemoryDto>(consolidatedMemory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to consolidate memories for character {CharacterId}", characterId);
            throw;
        }
    }

    public async Task<List<CharacterMemoryDto>> GetRelevantMemoriesAsync(
        Guid characterId, 
        string context, 
        int maxResults = 10, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting relevant memories for character {CharacterId} with context: {Context}", 
            characterId, context);

        try
        {
            var character = await _characterRepository.GetByIdAsync(characterId, cancellationToken);
            if (character == null)
            {
                throw new InvalidOperationException("Character not found");
            }

            await ValidateCharacterAccessAsync(character, cancellationToken);

            var memories = await _memoryService.GetRelevantMemoriesAsync(character, context, maxResults, cancellationToken);

            return _mapper.Map<List<CharacterMemoryDto>>(memories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get relevant memories for character {CharacterId}", characterId);
            throw;
        }
    }

    public async Task<CharacterDetailDto> GetCharacterDetailsAsync(Guid characterId, bool includeMemories = true, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting character details for {CharacterId}", characterId);

        try
        {
            // Cache prüfen
            var cacheKey = $"character_details:{characterId}:{includeMemories}";
            var cachedDetails = await _cacheService.GetAsync<CharacterDetailDto>(cacheKey, cancellationToken);
            
            if (cachedDetails != null)
            {
                return cachedDetails;
            }

            var character = includeMemories 
                ? await _characterRepository.GetWithFullDetailsAsync(characterId, cancellationToken)
                : await _characterRepository.GetWithTraitsAndMemoriesAsync(characterId, cancellationToken);

            if (character == null)
            {
                throw new InvalidOperationException("Character not found");
            }

            await ValidateCharacterAccessAsync(character, cancellationToken);

            var detailDto = await MapToDetailDtoAsync(character, cancellationToken);

            // Cache für 5 Minuten
            await _cacheService.SetAsync(cacheKey, detailDto, TimeSpan.FromMinutes(5), cancellationToken);

            return detailDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get character details for {CharacterId}", characterId);
            throw;
        }
    }

    public async Task<List<CharacterRecommendationDto>> GetCharacterRecommendationsAsync(Guid characterId, int maxResults = 10, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting character recommendations for {CharacterId}", characterId);

        try
        {
            var character = await _characterRepository.GetWithFullDetailsAsync(characterId, cancellationToken);
            if (character == null)
            {
                throw new InvalidOperationException("Character not found");
            }

            // Finde ähnliche Charaktere
            var similarCharacters = await _characterRepository.GetSimilarCharactersAsync(characterId, maxResults * 2, cancellationToken);
            
            var recommendations = new List<CharacterRecommendationDto>();

            foreach (var similarChar in similarCharacters)
            {
                if (similarChar.SharingStatus == CharacterSharingStatus.Community || 
                    similarChar.SharingStatus == CharacterSharingStatus.Featured)
                {
                    var compatibility = character.DNA.CalculateCompatibility(similarChar.DNA);
                    
                    var recommendation = new CharacterRecommendationDto
                    {
                        Character = _mapper.Map<CharacterDto>(similarChar),
                        CompatibilityScore = (float)compatibility,
                        RecommendationReasons = GenerateRecommendationReasons(character, similarChar),
                        RecommendationType = DetermineRecommendationType(character, similarChar),
                        IsFromSameArchetype = character.DNA.Archetype == similarChar.DNA.Archetype
                    };

                    recommendations.Add(recommendation);
                }
            }

            // Sortiere nach Kompatibilität
            recommendations = recommendations
                .OrderByDescending(r => r.CompatibilityScore)
                .Take(maxResults)
                .ToList();

            return recommendations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get character recommendations for {CharacterId}", characterId);
            throw;
        }
    }

    public async Task<MemoryAnalysis> AnalyzeCharacterMemoriesAsync(Guid characterId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing memories for character {CharacterId}", characterId);

        try
        {
            var character = await _characterRepository.GetWithTraitsAndMemoriesAsync(characterId, cancellationToken);
            if (character == null)
            {
                throw new InvalidOperationException("Character not found");
            }

            await ValidateCharacterAccessAsync(character, cancellationToken);

            return await _memoryService.AnalyzeMemoryPatternsAsync(character, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze memories for character {CharacterId}", characterId);
            throw;
        }
    }

    public async Task ShareCharacterToCommunityAsync(Guid characterId, ShareCharacterDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sharing character {CharacterId} to community with status {SharingStatus}", 
            characterId, dto.SharingStatus);

        try
        {
            var character = await _characterRepository.GetByIdAsync(characterId, cancellationToken);
            if (character == null)
            {
                throw new InvalidOperationException("Character not found");
            }

            await ValidateCharacterAccessAsync(character, cancellationToken);

            // Prüfe ob Character für Sharing berechtigt ist
            if (!character.CanSharePublicly() && dto.SharingStatus == CharacterSharingStatus.Community)
            {
                throw new InvalidOperationException("Character not eligible for public sharing");
            }

            character.UpdateSharingStatus(dto.SharingStatus);

            await _characterRepository.UpdateAsync(character, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Cache invalidieren
            await InvalidateCharacterCacheAsync(characterId, cancellationToken);

            _logger.LogInformation("Successfully shared character {CharacterId} to community", characterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to share character {CharacterId} to community", characterId);
            throw;
        }
    }

    public async Task<List<CharacterCommunityDto>> GetCommunityCharactersAsync(CharacterSearchDto searchDto, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting community characters with search criteria");

        try
        {
            var characters = await _characterRepository.GetCommunityCharactersAsync(
                searchDto.Page, 
                searchDto.PageSize, 
                cancellationToken);

            // Wende Filter an
            if (!string.IsNullOrEmpty(searchDto.NameQuery))
            {
                characters = characters.Where(c => c.Name.Contains(searchDto.NameQuery, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrEmpty(searchDto.Archetype))
            {
                characters = characters.Where(c => c.DNA.Archetype.Equals(searchDto.Archetype, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (searchDto.MinLevel.HasValue)
            {
                characters = characters.Where(c => c.Level >= searchDto.MinLevel.Value).ToList();
            }

            if (searchDto.MaxLevel.HasValue)
            {
                characters = characters.Where(c => c.Level <= searchDto.MaxLevel.Value).ToList();
            }

            var communityDtos = _mapper.Map<List<CharacterCommunityDto>>(characters);

            // Sortierung anwenden
            communityDtos = ApplySorting(communityDtos, searchDto.SortBy, searchDto.SortDescending);

            return communityDtos.Take(searchDto.PageSize).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get community characters");
            throw;
        }
    }

    public async Task<List<CharacterDto>> GetUserCharactersAsync(Guid userId, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting characters for user {UserId}", userId);

        try
        {
            var characters = await _characterRepository.GetByOwnerIdAsync(userId, includeInactive, cancellationToken);
            return _mapper.Map<List<CharacterDto>>(characters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get characters for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<CharacterDto>> ProcessBatchOperationAsync(BatchCharacterOperationDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing batch operation '{Operation}' on {Count} characters", 
            dto.Operation, dto.CharacterIds.Count);

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var processedCharacters = new List<Character>();

            foreach (var characterId in dto.CharacterIds)
            {
                var character = await _characterRepository.GetByIdAsync(characterId, cancellationToken);
                if (character == null) continue;

                await ValidateCharacterAccessAsync(character, cancellationToken);

                switch (dto.Operation.ToLower())
                {
                    case "deactivate":
                        character.SoftDelete(dto.Reason ?? "Batch deactivation");
                        break;
                    
                    case "reactivate":
                        character.Restore();
                        break;
                    
                    case "share":
                        if (dto.OperationParameters?.ContainsKey("sharingStatus") == true)
                        {
                            var sharingStatus = Enum.Parse<CharacterSharingStatus>(dto.OperationParameters["sharingStatus"].ToString()!);
                            character.UpdateSharingStatus(sharingStatus);
                        }
                        break;
                    
                    default:
                        _logger.LogWarning("Unknown batch operation: {Operation}", dto.Operation);
                        continue;
                }

                await _characterRepository.UpdateAsync(character, cancellationToken);
                processedCharacters.Add(character);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Successfully processed batch operation '{Operation}' on {Count} characters", 
                dto.Operation, processedCharacters.Count);

            return _mapper.Map<List<CharacterDto>>(processedCharacters);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to process batch operation '{Operation}'", dto.Operation);
            throw;
        }
    }

    // Private Helper Methods

    private async Task ValidateCharacterCreationAsync(CreateCharacterDto dto, CancellationToken cancellationToken)
    {
        // Content-Moderation
        var contentToModerate = $"{dto.Name} {dto.Description}";
        var moderationResult = await _moderationService.ModerateTextAsync(
            contentToModerate, 
            new ContentModerationOptions { TargetAge = dto.ChildAge, StrictMode = true }, 
            cancellationToken);

        if (moderationResult.Status != ContentModerationStatus.Approved)
        {
            throw new InvalidOperationException("Character name or description contains inappropriate content");
        }

        // Prüfe Namens-Duplikate für User
        var existingCharacters = await _characterRepository.GetByOwnerIdAsync(dto.OwnerId, cancellationToken: cancellationToken);
        if (existingCharacters.Any(c => c.Name.Equals(dto.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Character with this name already exists for user");
        }
    }

    private async Task ValidateUserCharacterLimitsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        var limits = user.GetCurrentLimits();
        var characterCount = await _characterRepository.GetCharacterCountByOwnerAsync(userId, cancellationToken);

        if (characterCount >= limits.MaxCharacters)
        {
            throw new InvalidOperationException($"Character limit reached. Maximum: {limits.MaxCharacters}");
        }
    }

    private async Task<CharacterDNA> CreateCharacterDNAAsync(CreateCharacterDto dto, CancellationToken cancellationToken)
    {
        if (dto.UseRandomGeneration)
        {
            return CharacterDNA.CreateRandom(
                dto.PreferredArchetype,
                dto.EmphasizedTraits?.Select(t => Enum.Parse<CharacterTraitType>(t)).ToList(),
                dto.ChildAge);
        }
        else
        {
            var customTraits = dto.CustomTraits?.ToDictionary(
                kvp => Enum.Parse<CharacterTraitType>(kvp.Key), 
                kvp => kvp.Value) ?? new Dictionary<CharacterTraitType, int>();

            return CharacterDNA.CreateCustom(
                dto.PreferredArchetype ?? "Explorer",
                customTraits,
                dto.PersonalityKeywords ?? new List<string>(),
                dto.PrimaryMotivation ?? "Neue Abenteuer erleben",
                dto.LearningStyle ?? "Mixed",
                dto.ChildAge);
        }
    }

    private async Task<string> GenerateCharacterAvatarAsync(Character character, CancellationToken cancellationToken)
    {
        try
        {
            var prompt = $"A friendly, cartoon-style avatar of a {character.DNA.Archetype.ToLower()} character named {character.Name}. " +
                        $"The character should look {string.Join(", ", character.DNA.CorePersonalityKeywords)} and child-friendly.";

            var imageUrl = await _imageService.GenerateImageAsync(prompt, new ImageGenerationOptions
            {
                Style = "cartoon",
                Size = "512x512",
                Quality = "standard"
            }, cancellationToken);

            return imageUrl;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate avatar for character {CharacterId}", character.Id);
            return ""; // Fallback auf kein Avatar
        }
    }

    private async Task ValidateCharacterAccessAsync(Character character, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId != character.OwnerId && !_currentUserService.IsInRole("Admin"))
        {
            throw new UnauthorizedAccessException("Access denied to this character");
        }
    }

    private async Task ValidateCharacterAdoptionAsync(Character originalCharacter, Guid newOwnerId, CancellationToken cancellationToken)
    {
        if (originalCharacter.SharingStatus != CharacterSharingStatus.Community && 
            originalCharacter.SharingStatus != CharacterSharingStatus.Featured)
        {
            throw new InvalidOperationException("Character is not available for adoption");
        }

        if (originalCharacter.OwnerId == newOwnerId)
        {
            throw new InvalidOperationException("Cannot adopt your own character");
        }

        await ValidateUserCharacterLimitsAsync(newOwnerId, cancellationToken);
    }

    private async Task<CharacterDetailDto> MapToDetailDtoAsync(Character character, CancellationToken cancellationToken)
    {
        var detailDto = _mapper.Map<CharacterDetailDto>(character);
        
        // Zusätzliche Berechnungen
        detailDto.CanSharePublicly = character.CanSharePublicly();
        detailDto.CanAdoptNewCharacters = character.CanAdoptNewCharacters();
        detailDto.DisplayInfo = character.GetDisplayInfo();

        return detailDto;
    }

    private async Task InvalidateCharacterCacheAsync(Guid characterId, CancellationToken cancellationToken)
    {
        await _cacheService.RemovePatternAsync($"character*:{characterId}*", cancellationToken);
    }

    // Weitere Helper-Methods würden hier implementiert werden...
    // (Aus Platzgründen nicht alle vollständig ausgeführt)

    private Dictionary<CharacterTraitType, float> ExtractTraitInfluencesFromContext(Dictionary<string, object> context)
    {
        var influences = new Dictionary<CharacterTraitType, float>();
        
        if (context.ContainsKey("traitInfluences") && context["traitInfluences"] is Dictionary<string, object> traitData)
        {
            foreach (var kvp in traitData)
            {
                if (Enum.TryParse<CharacterTraitType>(kvp.Key, out var traitType) && 
                    float.TryParse(kvp.Value.ToString(), out var influence))
                {
                    influences[traitType] = influence;
                }
            }
        }

        return influences;
    }

    private StoryExperienceContext ExtractStoryContextFromContext(Dictionary<string, object> context)
    {
        return new StoryExperienceContext
        {
            NewWords = ExtractListFromContext(context, "newWords"),
            LearningMoments = ExtractListFromContext(context, "learningMoments"),
            EmotionalExperiences = ExtractListFromContext(context, "emotionalExperiences")
        };
    }

    private List<string> ExtractListFromContext(Dictionary<string, object> context, string key)
    {
        if (context.ContainsKey(key) && context[key] is IEnumerable<object> list)
        {
            return list.Select(x => x.ToString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList();
        }
        return new List<string>();
    }

    private async Task CreateWelcomeMemoryAsync(Character character, CancellationToken cancellationToken)
    {
        var welcomeMemory = new CharacterMemory(
            title: "Meine Erschaffung",
            summary: $"Ich bin {character.Name} und wurde heute geboren! Ich bin ein {character.DNA.Archetype} und freue mich auf viele Abenteuer.",
            memoryType: MemoryType.UserInteraction,
            importance: 8); // Wichtige erste Erinnerung

        welcomeMemory.AddTag("Erstellung");
        welcomeMemory.AddTag("Anfang");
        welcomeMemory.AddEmotionalContext("Aufregung");
        welcomeMemory.AddEmotionalContext("Neugier");

        await _memoryService.AddMemoryAsync(character, welcomeMemory, cancellationToken);
    }

    private List<string> GenerateRecommendationReasons(Character character, Character recommended)
    {
        var reasons = new List<string>();
        
        if (character.DNA.Archetype == recommended.DNA.Archetype)
        {
            reasons.Add($"Gleicher Archetyp: {character.DNA.Archetype}");
        }

        var sharedTraits = character.Traits
            .Join(recommended.Traits, 
                  c => c.TraitType, 
                  r => r.TraitType, 
                  (c, r) => new { Type = c.TraitType, Diff = Math.Abs(c.CurrentValue - r.CurrentValue) })
            .Where(x => x.Diff <= 2)
            .Select(x => x.Type.ToDisplayString())
            .ToList();

        if (sharedTraits.Any())
        {
            reasons.Add($"Ähnliche Eigenschaften: {string.Join(", ", sharedTraits.Take(3))}");
        }

        return reasons;
    }

    private string DetermineRecommendationType(Character character, Character recommended)
    {
        if (character.DNA.Archetype == recommended.DNA.Archetype)
            return "similar";
        
        var compatibility = character.DNA.CalculateCompatibility(recommended.DNA);
        if (compatibility > 0.7)
            return "complementary";
        
        return "popular";
    }

    private List<CharacterCommunityDto> ApplySorting(List<CharacterCommunityDto> characters, string? sortBy, bool descending)
    {
        return sortBy?.ToLower() switch
        {
            "popularity" => descending 
                ? characters.OrderByDescending(c => c.PopularityScore).ToList()
                : characters.OrderBy(c => c.PopularityScore).ToList(),
            "level" => descending 
                ? characters.OrderByDescending(c => c.Level).ToList()
                : characters.OrderBy(c => c.Level).ToList(),
            "recent" => descending 
                ? characters.OrderByDescending(c => c.SharedAt).ToList()
                : characters.OrderBy(c => c.SharedAt).ToList(),
            "name" => descending 
                ? characters.OrderByDescending(c => c.Name).ToList()
                : characters.OrderBy(c => c.Name).ToList(),
            _ => characters.OrderByDescending(c => c.PopularityScore).ToList()
        };
    }

    // Process specific interaction types
    private async Task ProcessUserPraiseInteractionAsync(Character character, Dictionary<string, object> data, CancellationToken cancellationToken)
    {
        var praiseType = data.GetValueOrDefault("praiseType")?.ToString() ?? "general";
        var reason = data.GetValueOrDefault("reason")?.ToString() ?? "User praised character";

        // Verstärke relevante Traits basierend auf Lob-Type
        var traitToReinforce = praiseType.ToLower() switch
        {
            "courage" => CharacterTraitType.Courage,
            "kindness" => CharacterTraitType.Kindness,
            "creativity" => CharacterTraitType.Creativity,
            "intelligence" => CharacterTraitType.Intelligence,
            _ => CharacterTraitType.Optimism
        };

        character.ReinforceTraitPositively(traitToReinforce, reason, 1.3f);
    }

    private async Task ProcessStoryChoiceInteractionAsync(Character character, Dictionary<string, object> data, CancellationToken cancellationToken)
    {
        // TODO: Implementiere Story-Choice Verarbeitung
        await Task.CompletedTask;
    }

    private async Task ProcessLearningAchievementInteractionAsync(Character character, Dictionary<string, object> data, CancellationToken cancellationToken)
    {
        // TODO: Implementiere Learning-Achievement Verarbeitung
        await Task.CompletedTask;
    }

    private async Task ProcessSocialInteractionAsync(Character character, Dictionary<string, object> data, CancellationToken cancellationToken)
    {
        // TODO: Implementiere Social-Interaction Verarbeitung
        await Task.CompletedTask;
    }

    private async Task<CharacterDNA> CreateAdoptedCharacterDNAAsync(CharacterDNA originalDna, AdoptCharacterDto dto, CancellationToken cancellationToken)
    {
        // TODO: Implementiere DNA-Adaption für adoptierte Charaktere
        return originalDna; // Placeholder
    }

    private async Task TransferCharacterTraitsAsync(Character from, Character to, CancellationToken cancellationToken)
    {
        // TODO: Implementiere Trait-Transfer
        await Task.CompletedTask;
    }

    private async Task TransferCharacterMemoriesAsync(Character from, Character to, CancellationToken cancellationToken)
    {
        // TODO: Implementiere Memory-Transfer
        await Task.CompletedTask;
    }

    private async Task CreateStoryExperienceMemoryAsync(Character character, Guid storyId, StoryExperienceContext context, CancellationToken cancellationToken)
    {
        // TODO: Implementiere Story-Experience Memory Creation
        await Task.CompletedTask;
    }
}

// Supporting Classes
public class StoryExperienceContext
{
    public List<string> NewWords { get; set; } = new();
    public List<string> LearningMoments { get; set; } = new();
    public List<string> EmotionalExperiences { get; set; } = new();
}