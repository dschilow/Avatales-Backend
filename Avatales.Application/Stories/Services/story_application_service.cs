using Microsoft.Extensions.Logging;
using AutoMapper;
using Avatales.Domain.Stories.Entities;
using Avatales.Domain.Stories.Services;
using Avatales.Domain.Characters.Entities;
using Avatales.Application.Interfaces.Repositories;
using Avatales.Application.Common.Interfaces;
using Avatales.Application.Stories.DTOs;
using Avatales.Shared.Models;

namespace Avatales.Application.Stories.Services;

/// <summary>
/// Application Service für Story-Management und -Generierung
/// Orchestriert die komplexe Story-Generierung und -Verwaltung
/// </summary>
public interface IStoryApplicationService : IApplicationService
{
    // Story Generation
    Task<GenerateStoryResponseDto> GenerateStoryAsync(GenerateStoryRequestDto request, CancellationToken cancellationToken = default);
    Task<GenerateStoryResponseDto> GenerateFromTemplateAsync(Guid templateId, Guid characterId, Dictionary<string, object> variables, CancellationToken cancellationToken = default);
    Task<GenerateStoryResponseDto> RemixStoryAsync(Guid originalStoryId, Guid characterId, string remixPrompt, CancellationToken cancellationToken = default);
    Task<GenerateStoryResponseDto> GenerateAdaptiveStoryAsync(Guid characterId, LearningGoalCategory targetLearning, CancellationToken cancellationToken = default);
    
    // Story Management
    Task<StoryDetailDto> GetStoryDetailsAsync(Guid storyId, CancellationToken cancellationToken = default);
    Task<StoryDetailDto> UpdateStoryAsync(UpdateStoryDto dto, CancellationToken cancellationToken = default);
    Task<StorySceneDto> UpdateStorySceneAsync(UpdateStorySceneDto dto, CancellationToken cancellationToken = default);
    Task DeleteStoryAsync(Guid storyId, string reason, CancellationToken cancellationToken = default);
    
    // Story Reading & Progress
    Task<StoryReadingProgressDto> StartReadingAsync(Guid storyId, Guid userId, Guid characterId, CancellationToken cancellationToken = default);
    Task<StoryReadingProgressDto> UpdateReadingProgressAsync(ReadStoryCommand command, CancellationToken cancellationToken = default);
    Task CompleteStoryReadingAsync(Guid storyId, Guid userId, Guid characterId, TimeSpan totalTime, CancellationToken cancellationToken = default);
    
    // Story Interaction
    Task RateStoryAsync(RateStoryDto dto, CancellationToken cancellationToken = default);
    Task ShareStoryAsync(ShareStoryDto dto, CancellationToken cancellationToken = default);
    Task<string> ExportStoryAsync(ExportStoryDto dto, CancellationToken cancellationToken = default);
    
    // Story Enhancement
    Task<List<string>> GenerateStoryImagesAsync(Guid storyId, CancellationToken cancellationToken = default);
    Task<List<LearningGoalDto>> GenerateAdditionalLearningGoalsAsync(Guid storyId, int targetAge, CancellationToken cancellationToken = default);
    Task<List<StorySceneDto>> GenerateAlternativeEndingAsync(Guid storyId, string alternativePrompt, CancellationToken cancellationToken = default);
    
    // Story Discovery
    Task<List<StoryDto>> GetUserStoriesAsync(Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<List<StoryCommunityDto>> GetCommunityStoriesAsync(StorySearchDto searchDto, CancellationToken cancellationToken = default);
    Task<List<StoryRecommendationDto>> GetStoryRecommendationsAsync(Guid userId, Guid? characterId = null, CancellationToken cancellationToken = default);
    Task<List<StoryDto>> SearchStoriesAsync(StorySearchDto searchDto, CancellationToken cancellationToken = default);
    
    // Story Analytics
    Task<StoryAnalysisDto> AnalyzeStoryAsync(Guid storyId, CancellationToken cancellationToken = default);
    Task<StoryStatisticsDto> GetStoryStatisticsAsync(Guid storyId, CancellationToken cancellationToken = default);
    Task UpdateStoryStatisticsAsync(UpdateStoryStatisticsDto dto, CancellationToken cancellationToken = default);
    
    // Learning Goals
    Task UpdateLearningGoalProgressAsync(UpdateLearningGoalProgressDto dto, CancellationToken cancellationToken = default);
    Task<List<LearningGoalProgressDto>> GetLearningGoalProgressAsync(Guid storyId, Guid userId, CancellationToken cancellationToken = default);
    
    // Batch Operations
    Task<List<StoryDto>> ProcessBatchOperationAsync(BatchStoryOperationDto dto, CancellationToken cancellationToken = default);
}

public class StoryApplicationService : IStoryApplicationService
{
    private readonly IStoryRepository _storyRepository;
    private readonly ICharacterRepository _characterRepository;
    private readonly IUserRepository _userRepository;
    private readonly IStoryInteractionRepository _interactionRepository;
    private readonly IStoryGenerationService _generationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly ILogger<StoryApplicationService> _logger;
    private readonly ICacheService _cacheService;
    private readonly IImageGenerationService _imageService;
    private readonly IContentModerationService _moderationService;
    private readonly INotificationService _notificationService;
    private readonly IAnalyticsService _analyticsService;

    public StoryApplicationService(
        IStoryRepository storyRepository,
        ICharacterRepository characterRepository,
        IUserRepository userRepository,
        IStoryInteractionRepository interactionRepository,
        IStoryGenerationService generationService,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher eventDispatcher,
        ICurrentUserService currentUserService,
        IMapper mapper,
        ILogger<StoryApplicationService> logger,
        ICacheService cacheService,
        IImageGenerationService imageService,
        IContentModerationService moderationService,
        INotificationService notificationService,
        IAnalyticsService analyticsService)
    {
        _storyRepository = storyRepository;
        _characterRepository = characterRepository;
        _userRepository = userRepository;
        _interactionRepository = interactionRepository;
        _generationService = generationService;
        _unitOfWork = unitOfWork;
        _eventDispatcher = eventDispatcher;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _logger = logger;
        _cacheService = cacheService;
        _imageService = imageService;
        _moderationService = moderationService;
        _notificationService = notificationService;
        _analyticsService = analyticsService;
    }

    public async Task<GenerateStoryResponseDto> GenerateStoryAsync(GenerateStoryRequestDto request, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting story generation for character {CharacterId} with prompt: {Prompt}", 
            request.MainCharacterId, request.UserPrompt);

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            // 1. Validierungen
            await ValidateStoryGenerationRequestAsync(request, cancellationToken);

            // 2. Lade Character
            var character = await _characterRepository.GetWithFullDetailsAsync(request.MainCharacterId, cancellationToken);
            if (character == null)
            {
                throw new InvalidOperationException("Character not found");
            }

            // 3. Prüfe User-Limits
            await ValidateUserStoryLimitsAsync(character.OwnerId, cancellationToken);

            // 4. Erstelle Story-Entity (initial)
            var story = new Story(
                title: "Generating...", // Wird später aktualisiert
                summary: "Story wird generiert...",
                mainCharacterId: request.MainCharacterId,
                authorUserId: character.OwnerId,
                genre: Enum.Parse<StoryGenre>(request.Genre),
                userPrompt: request.UserPrompt);

            story.SetGenerationStatus(StoryGenerationStatus.InProgress);

            // 5. Speichere Story (für Progress-Tracking)
            var savedStory = await _storyRepository.AddAsync(story, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // 6. Erstelle Generation-Request für Domain Service
            var generationRequest = new StoryGenerationRequest
            {
                Character = character,
                UserPrompt = request.UserPrompt,
                Genre = Enum.Parse<StoryGenre>(request.Genre),
                TargetLearningGoals = request.RequestedLearningGoals?.Select(lg => Enum.Parse<LearningGoalCategory>(lg)).ToList(),
                TargetWordCount = request.TargetLength,
                DifficultyLevel = request.DifficultyLevel ?? 3,
                EmotionalTone = request.EmotionalTone,
                AvoidTopics = request.AvoidTopics,
                IncludeImages = request.IncludeImages,
                EnableLearningMode = request.EnableInteractiveElements
            };

            // 7. Generiere Story (Async im Hintergrund für bessere UX)
            var generationResult = await _generationService.GenerateStoryAsync(generationRequest, cancellationToken);

            if (!generationResult.IsSuccess || generationResult.Story == null)
            {
                // Fehler-Status setzen
                savedStory.SetGenerationStatus(StoryGenerationStatus.Failed);
                await _storyRepository.UpdateAsync(savedStory, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return new GenerateStoryResponseDto
                {
                    IsSuccess = false,
                    ErrorMessage = generationResult.ErrorMessage ?? "Story generation failed",
                    GenerationDuration = generationResult.GenerationDuration,
                    WarningsOrSuggestions = generationResult.WarningsOrSuggestions
                };
            }

            // 8. Aktualisiere Story mit generierten Inhalten
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            
            var generatedStory = generationResult.Story;
            savedStory.UpdateFromGeneration(generatedStory);
            savedStory.SetGenerationStatus(StoryGenerationStatus.Completed);
            savedStory.SetCompletedAt(DateTime.UtcNow);

            await _storyRepository.UpdateAsync(savedStory, cancellationToken);

            // 9. Aktualisiere User-Statistiken
            var user = await _userRepository.GetByIdAsync(character.OwnerId, cancellationToken);
            user?.IncrementStoriesGenerated();
            if (user != null)
            {
                await _userRepository.UpdateAsync(user, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 10. Dispatch Events
            await _eventDispatcher.DispatchEventsAsync(savedStory.DomainEvents, cancellationToken);
            savedStory.ClearDomainEvents();

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // 11. Post-Generation Tasks (Async)
            _ = Task.Run(async () =>
            {
                await ProcessPostGenerationTasksAsync(savedStory, character, cancellationToken);
            }, cancellationToken);

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("Story generation completed successfully in {Duration}ms for character {CharacterId}", 
                duration.TotalMilliseconds, request.MainCharacterId);

            return new GenerateStoryResponseDto
            {
                IsSuccess = true,
                StoryId = savedStory.Id,
                GenerationDuration = duration,
                WarningsOrSuggestions = generationResult.WarningsOrSuggestions
            };
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "Story generation failed after {Duration}ms for character {CharacterId}", 
                duration.TotalMilliseconds, request.MainCharacterId);

            return new GenerateStoryResponseDto
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                GenerationDuration = duration
            };
        }
    }

    public async Task<GenerateStoryResponseDto> GenerateFromTemplateAsync(Guid templateId, Guid characterId, Dictionary<string, object> variables, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating story from template {TemplateId} for character {CharacterId}", 
            templateId, characterId);

        try
        {
            // 1. Lade Template
            var template = await LoadStoryTemplateAsync(templateId, cancellationToken);
            if (template == null)
            {
                throw new InvalidOperationException("Story template not found");
            }

            // 2. Lade Character
            var character = await _characterRepository.GetWithFullDetailsAsync(characterId, cancellationToken);
            if (character == null)
            {
                throw new InvalidOperationException("Character not found");
            }

            // 3. Validiere Berechtigung
            await ValidateCharacterAccessAsync(character, cancellationToken);

            // 4. Generiere über Domain Service
            var generatedStory = await _generationService.GenerateFromTemplateAsync(template, character, variables, cancellationToken);

            // 5. Speichere und verarbeite
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            
            var savedStory = await _storyRepository.AddAsync(generatedStory, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Successfully generated story from template {TemplateId}", templateId);

            return new GenerateStoryResponseDto
            {
                IsSuccess = true,
                StoryId = savedStory.Id,
                GenerationDuration = TimeSpan.FromSeconds(2) // Template-basiert ist schneller
            };
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to generate story from template {TemplateId}", templateId);
            
            return new GenerateStoryResponseDto
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<GenerateStoryResponseDto> RemixStoryAsync(Guid originalStoryId, Guid characterId, string remixPrompt, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating remix of story {OriginalStoryId} for character {CharacterId}", 
            originalStoryId, characterId);

        try
        {
            // 1. Lade Original-Story
            var originalStory = await _storyRepository.GetWithFullDetailsAsync(originalStoryId, cancellationToken);
            if (originalStory == null)
            {
                throw new InvalidOperationException("Original story not found");
            }

            // 2. Prüfe Remix-Berechtigung
            if (!originalStory.AllowsRemixing())
            {
                throw new InvalidOperationException("Story cannot be remixed");
            }

            // 3. Lade Character
            var character = await _characterRepository.GetWithFullDetailsAsync(characterId, cancellationToken);
            if (character == null)
            {
                throw new InvalidOperationException("Character not found");
            }

            await ValidateCharacterAccessAsync(character, cancellationToken);

            // 4. Generiere Remix
            var remixedStory = await _generationService.RemixStoryAsync(originalStory, character, remixPrompt, cancellationToken);

            // 5. Speichere
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            
            var savedStory = await _storyRepository.AddAsync(remixedStory, cancellationToken);
            
            // 6. Aktualisiere Original-Story Statistiken
            originalStory.IncrementRemixCount();
            await _storyRepository.UpdateAsync(originalStory, cancellationToken);
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Successfully created remix of story {OriginalStoryId}", originalStoryId);

            return new GenerateStoryResponseDto
            {
                IsSuccess = true,
                StoryId = savedStory.Id,
                GenerationDuration = TimeSpan.FromSeconds(5) // Remix ist mittelmäßig schnell
            };
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to create remix of story {OriginalStoryId}", originalStoryId);
            
            return new GenerateStoryResponseDto
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<GenerateStoryResponseDto> GenerateAdaptiveStoryAsync(Guid characterId, LearningGoalCategory targetLearning, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating adaptive story for character {CharacterId} targeting {LearningCategory}", 
            characterId, targetLearning);

        try
        {
            var character = await _characterRepository.GetWithFullDetailsAsync(characterId, cancellationToken);
            if (character == null)
            {
                throw new InvalidOperationException("Character not found");
            }

            await ValidateCharacterAccessAsync(character, cancellationToken);

            var adaptiveStory = await _generationService.GenerateAdaptiveStoryAsync(character, targetLearning, cancellationToken);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            var savedStory = await _storyRepository.AddAsync(adaptiveStory, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Successfully generated adaptive story for learning goal {LearningCategory}", targetLearning);

            return new GenerateStoryResponseDto
            {
                IsSuccess = true,
                StoryId = savedStory.Id,
                GenerationDuration = TimeSpan.FromSeconds(7) // Adaptive Generation ist komplex
            };
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to generate adaptive story for character {CharacterId}", characterId);
            
            return new GenerateStoryResponseDto
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<StoryDetailDto> GetStoryDetailsAsync(Guid storyId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting story details for {StoryId}", storyId);

        try
        {
            // Cache prüfen
            var cacheKey = $"story_details:{storyId}";
            var cachedDetails = await _cacheService.GetAsync<StoryDetailDto>(cacheKey, cancellationToken);
            
            if (cachedDetails != null)
            {
                return cachedDetails;
            }

            var story = await _storyRepository.GetWithFullDetailsAsync(storyId, cancellationToken);
            if (story == null)
            {
                throw new InvalidOperationException("Story not found");
            }

            await ValidateStoryAccessAsync(story, cancellationToken);

            // Lade zusätzliche Daten
            var statistics = await _interactionRepository.GetStoryStatisticsAsync(storyId, cancellationToken);
            var interactions = await GetRecentInteractionsAsync(storyId, cancellationToken);

            var detailDto = _mapper.Map<StoryDetailDto>(story);
            detailDto.Statistics = _mapper.Map<StoryStatisticsDto>(statistics);
            detailDto.Interactions = _mapper.Map<List<StoryInteractionDto>>(interactions);

            // Berechne Berechtigungen
            detailDto.CanEdit = await CanEditStoryAsync(story, cancellationToken);
            detailDto.CanShare = await CanShareStoryAsync(story, cancellationToken);
            detailDto.CanDelete = await CanDeleteStoryAsync(story, cancellationToken);

            // Cache für 5 Minuten
            await _cacheService.SetAsync(cacheKey, detailDto, TimeSpan.FromMinutes(5), cancellationToken);

            return detailDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get story details for {StoryId}", storyId);
            throw;
        }
    }

    public async Task<StoryDetailDto> UpdateStoryAsync(UpdateStoryDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating story {StoryId}", dto.StoryId);

        try
        {
            var story = await _storyRepository.GetByIdAsync(dto.StoryId, cancellationToken);
            if (story == null)
            {
                throw new InvalidOperationException("Story not found");
            }

            await ValidateStoryEditAccessAsync(story, cancellationToken);

            // Content-Moderation
            var contentToModerate = $"{dto.Title} {dto.Summary}";
            var moderationResult = await _moderationService.ModerateTextAsync(
                contentToModerate, 
                new ContentModerationOptions { TargetAge = story.RecommendedAge, StrictMode = true }, 
                cancellationToken);

            if (moderationResult.Status != ContentModerationStatus.Approved)
            {
                throw new InvalidOperationException("Updated content not appropriate");
            }

            // Aktualisiere Story
            story.UpdateBasicInfo(dto.Title, dto.Summary);
            
            if (dto.Tags != null)
            {
                story.UpdateTags(dto.Tags);
            }

            if (dto.RecommendedAge.HasValue)
            {
                story.SetRecommendedAge(dto.RecommendedAge.Value);
            }

            story.SetPublicVisibility(dto.IsPublic);

            await _storyRepository.UpdateAsync(story, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Cache invalidieren
            await InvalidateStoryCacheAsync(dto.StoryId, cancellationToken);

            _logger.LogInformation("Successfully updated story {StoryId}", dto.StoryId);

            return await GetStoryDetailsAsync(dto.StoryId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update story {StoryId}", dto.StoryId);
            throw;
        }
    }

    public async Task<StoryReadingProgressDto> StartReadingAsync(Guid storyId, Guid userId, Guid characterId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting reading session for story {StoryId} by user {UserId} with character {CharacterId}", 
            storyId, userId, characterId);

        try
        {
            var story = await _storyRepository.GetWithScenesAndLearningGoalsAsync(storyId, cancellationToken);
            if (story == null)
            {
                throw new InvalidOperationException("Story not found");
            }

            await ValidateStoryAccessAsync(story, cancellationToken);

            // Prüfe ob bereits gelesen wird
            var existingProgress = await _interactionRepository.GetReadingProgressAsync(storyId, userId, cancellationToken);
            if (existingProgress != null)
            {
                return _mapper.Map<StoryReadingProgressDto>(existingProgress);
            }

            // Erstelle neuen Progress
            var progress = new StoryReadingProgress
            {
                StoryId = storyId,
                UserId = userId,
                CharacterUsedId = characterId,
                CurrentScene = 1,
                TotalScenes = story.Scenes.Count,
                ReadingTimeElapsed = TimeSpan.Zero,
                LastReadAt = DateTime.UtcNow,
                IsCompleted = false
            };

            await _interactionRepository.SaveReadingProgressAsync(progress, cancellationToken);

            // Statistiken aktualisieren
            await _interactionRepository.IncrementStoryViewAsync(storyId, userId, cancellationToken);

            _logger.LogInformation("Started reading session for story {StoryId}", storyId);

            return _mapper.Map<StoryReadingProgressDto>(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start reading session for story {StoryId}", storyId);
            throw;
        }
    }

    public async Task<StoryReadingProgressDto> UpdateReadingProgressAsync(ReadStoryCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var progress = await _interactionRepository.GetReadingProgressAsync(command.StoryId, command.UserId, cancellationToken);
            if (progress == null)
            {
                throw new InvalidOperationException("Reading progress not found");
            }

            // Aktualisiere Progress
            progress.CurrentScene = command.CurrentScene;
            progress.ReadingTimeElapsed = progress.ReadingTimeElapsed.Add(command.SessionDuration);
            progress.LastReadAt = DateTime.UtcNow;
            progress.IsCompleted = command.CompletedReading;

            if (command.CompletedReading)
            {
                progress.CompletedAt = DateTime.UtcNow;
                
                // Character-Erfahrung hinzufügen
                await AddStoryExperienceToCharacterAsync(command.StoryId, command.CharacterUsedId, progress.ReadingTimeElapsed, cancellationToken);
            }

            await _interactionRepository.SaveReadingProgressAsync(progress, cancellationToken);

            return _mapper.Map<StoryReadingProgressDto>(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update reading progress for story {StoryId}", command.StoryId);
            throw;
        }
    }

    public async Task CompleteStoryReadingAsync(Guid storyId, Guid userId, Guid characterId, TimeSpan totalTime, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Completing story reading for story {StoryId} by user {UserId}", storyId, userId);

        try
        {
            var progress = await _interactionRepository.GetReadingProgressAsync(storyId, userId, cancellationToken);
            if (progress == null)
            {
                throw new InvalidOperationException("Reading progress not found");
            }

            if (!progress.IsCompleted)
            {
                progress.IsCompleted = true;
                progress.CompletedAt = DateTime.UtcNow;
                progress.ReadingTimeElapsed = totalTime;

                await _interactionRepository.SaveReadingProgressAsync(progress, cancellationToken);

                // Character-Erfahrung hinzufügen
                await AddStoryExperienceToCharacterAsync(storyId, characterId, totalTime, cancellationToken);

                // Analytics-Event
                await _analyticsService.TrackUserActionAsync(userId, "StoryCompleted", new Dictionary<string, object>
                {
                    ["StoryId"] = storyId,
                    ["CharacterId"] = characterId,
                    ["ReadingTime"] = totalTime.TotalMinutes,
                    ["CompletedAt"] = DateTime.UtcNow
                }, cancellationToken);

                _logger.LogInformation("Story reading completed for story {StoryId} by user {UserId}", storyId, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete story reading for story {StoryId}", storyId);
            throw;
        }
    }

    public async Task RateStoryAsync(RateStoryDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Rating story {StoryId} with {Rating} stars by user {UserId}", 
            dto.StoryId, dto.Rating, dto.UserId);

        try
        {
            var story = await _storyRepository.GetByIdAsync(dto.StoryId, cancellationToken);
            if (story == null)
            {
                throw new InvalidOperationException("Story not found");
            }

            // Prüfe ob User bereits bewertet hat
            var existingRating = await _interactionRepository.GetStoryRatingAsync(dto.StoryId, dto.UserId, cancellationToken);
            
            var rating = existingRating ?? new StoryRating
            {
                StoryId = dto.StoryId,
                UserId = dto.UserId
            };

            rating.Rating = dto.Rating;
            rating.ReviewComment = dto.ReviewComment;
            rating.PositiveAspects = dto.PositiveAspects ?? new List<string>();
            rating.NegativeAspects = dto.NegativeAspects ?? new List<string>();
            rating.UpdatedAt = DateTime.UtcNow;

            await _interactionRepository.SaveStoryRatingAsync(rating, cancellationToken);

            // Story-Statistiken aktualisieren
            await UpdateStoryRatingStatisticsAsync(dto.StoryId, cancellationToken);

            _logger.LogInformation("Successfully rated story {StoryId}", dto.StoryId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rate story {StoryId}", dto.StoryId);
            throw;
        }
    }

    public async Task ShareStoryAsync(ShareStoryDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sharing story {StoryId} via {ShareMethod} by user {SharedByUserId}", 
            dto.StoryId, dto.ShareMethod, dto.SharedByUserId);

        try
        {
            var story = await _storyRepository.GetByIdAsync(dto.StoryId, cancellationToken);
            if (story == null)
            {
                throw new InvalidOperationException("Story not found");
            }

            await ValidateStoryShareAccessAsync(story, dto.SharedByUserId, cancellationToken);

            // Verarbeite verschiedene Share-Methoden
            switch (dto.ShareMethod.ToLower())
            {
                case "community":
                    await ShareToCommunityAsync(story, dto, cancellationToken);
                    break;
                
                case "direct":
                    await ShareDirectlyAsync(story, dto, cancellationToken);
                    break;
                
                case "export":
                    await ShareAsExportAsync(story, dto, cancellationToken);
                    break;
                
                default:
                    throw new InvalidOperationException($"Unknown share method: {dto.ShareMethod}");
            }

            // Teilen-Statistik aktualisieren
            await _interactionRepository.RecordStoryShareAsync(dto.StoryId, dto.SharedByUserId, dto.ShareMethod, cancellationToken);

            _logger.LogInformation("Successfully shared story {StoryId} via {ShareMethod}", dto.StoryId, dto.ShareMethod);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to share story {StoryId} via {ShareMethod}", dto.StoryId, dto.ShareMethod);
            throw;
        }
    }

    public async Task<string> ExportStoryAsync(ExportStoryDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting story {StoryId} as {Format} for user {RequestedByUserId}", 
            dto.StoryId, dto.Format, dto.RequestedByUserId);

        try
        {
            var story = await _storyRepository.GetWithFullDetailsAsync(dto.StoryId, cancellationToken);
            if (story == null)
            {
                throw new InvalidOperationException("Story not found");
            }

            await ValidateStoryAccessAsync(story, cancellationToken);

            // Delegiere an Export-Service basierend auf Format
            string exportResult = dto.Format.ToLower() switch
            {
                "pdf" => await ExportAsPdfAsync(story, dto, cancellationToken),
                "epub" => await ExportAsEpubAsync(story, dto, cancellationToken),
                "html" => await ExportAsHtmlAsync(story, dto, cancellationToken),
                "docx" => await ExportAsDocxAsync(story, dto, cancellationToken),
                "json" => await ExportAsJsonAsync(story, dto, cancellationToken),
                _ => throw new InvalidOperationException($"Unsupported export format: {dto.Format}")
            };

            _logger.LogInformation("Successfully exported story {StoryId} as {Format}", dto.StoryId, dto.Format);
            return exportResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export story {StoryId} as {Format}", dto.StoryId, dto.Format);
            throw;
        }
    }

    // Weitere Methods...
    // (Aus Platzgründen werden nicht alle Methods vollständig implementiert)

    public async Task<List<string>> GenerateStoryImagesAsync(Guid storyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating images for story {StoryId}", storyId);

        try
        {
            var story = await _storyRepository.GetWithScenesAndLearningGoalsAsync(storyId, cancellationToken);
            if (story == null)
            {
                throw new InvalidOperationException("Story not found");
            }

            await ValidateStoryEditAccessAsync(story, cancellationToken);

            var imageUrls = await _generationService.GenerateImagePromptsAsync(story, cancellationToken);

            // Generiere tatsächliche Images
            var generatedImageUrls = new List<string>();
            foreach (var prompt in imageUrls)
            {
                try
                {
                    var imageUrl = await _imageService.GenerateImageAsync(prompt, new ImageGenerationOptions
                    {
                        Style = "children_book",
                        Size = "1024x1024",
                        Quality = "high"
                    }, cancellationToken);
                    
                    generatedImageUrls.Add(imageUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate image for prompt: {Prompt}", prompt);
                }
            }

            // Aktualisiere Story mit Images
            story.AddImageUrls(generatedImageUrls);
            await _storyRepository.UpdateAsync(story, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Generated {Count} images for story {StoryId}", generatedImageUrls.Count, storyId);
            return generatedImageUrls;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate images for story {StoryId}", storyId);
            throw;
        }
    }

    // Private Helper Methods

    private async Task ValidateStoryGenerationRequestAsync(GenerateStoryRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserPrompt))
            throw new ArgumentException("User prompt is required");

        if (request.UserPrompt.Length > 500)
            throw new ArgumentException("User prompt too long");

        if (!request.UserPrompt.IsChildFriendly())
            throw new ArgumentException("User prompt contains inappropriate content");

        var moderationResult = await _moderationService.ModerateTextAsync(
            request.UserPrompt, 
            new ContentModerationOptions { TargetAge = 6, StrictMode = true }, 
            cancellationToken);

        if (moderationResult.Status != ContentModerationStatus.Approved)
        {
            throw new ArgumentException("User prompt not appropriate for children");
        }
    }

    private async Task ValidateUserStoryLimitsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        if (!user.CanGenerateMoreStories())
        {
            var limits = user.GetCurrentLimits();
            throw new InvalidOperationException($"Monthly story limit reached. Limit: {limits.MonthlyStories}");
        }
    }

    private async Task ValidateStoryAccessAsync(Story story, CancellationToken cancellationToken)
    {
        if (!story.IsPublic && 
            story.AuthorUserId != _currentUserService.UserId && 
            !_currentUserService.IsInRole("Admin"))
        {
            throw new UnauthorizedAccessException("Access denied to this story");
        }
    }

    private async Task ValidateCharacterAccessAsync(Character character, CancellationToken cancellationToken)
    {
        if (character.OwnerId != _currentUserService.UserId && !_currentUserService.IsInRole("Admin"))
        {
            throw new UnauthorizedAccessException("Access denied to this character");
        }
    }

    private async Task ValidateStoryEditAccessAsync(Story story, CancellationToken cancellationToken)
    {
        if (story.AuthorUserId != _currentUserService.UserId && !_currentUserService.IsInRole("Admin"))
        {
            throw new UnauthorizedAccessException("Cannot edit this story");
        }
    }

    private async Task ValidateStoryShareAccessAsync(Story story, Guid userId, CancellationToken cancellationToken)
    {
        if (story.AuthorUserId != userId && !_currentUserService.IsInRole("Admin"))
        {
            throw new UnauthorizedAccessException("Cannot share this story");
        }
    }

    private async Task ProcessPostGenerationTasksAsync(Story story, Character character, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Benachrichtigung senden
            await _notificationService.SendNotificationAsync(
                character.OwnerId,
                NotificationType.NewStoryGenerated,
                "Deine neue Geschichte ist fertig!",
                new Dictionary<string, object> { ["StoryId"] = story.Id, ["StoryTitle"] = story.Title },
                cancellationToken);

            // 2. Images generieren falls gewünscht
            if (story.HasImages)
            {
                _ = Task.Run(async () => await GenerateStoryImagesAsync(story.Id, cancellationToken));
            }

            // 3. Analytics-Event
            await _analyticsService.TrackUserActionAsync(character.OwnerId, "StoryGenerated", new Dictionary<string, object>
            {
                ["StoryId"] = story.Id,
                ["CharacterId"] = character.Id,
                ["Genre"] = story.Genre.ToString(),
                ["WordCount"] = story.WordCount,
                ["HasImages"] = story.HasImages
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Post-generation tasks failed for story {StoryId}", story.Id);
        }
    }

    private async Task<StoryTemplate?> LoadStoryTemplateAsync(Guid templateId, CancellationToken cancellationToken)
    {
        // TODO: Implementiere Template-Loading
        return null; // Placeholder
    }

    private async Task<bool> CanEditStoryAsync(Story story, CancellationToken cancellationToken)
    {
        return story.AuthorUserId == _currentUserService.UserId || _currentUserService.IsInRole("Admin");
    }

    private async Task<bool> CanShareStoryAsync(Story story, CancellationToken cancellationToken)
    {
        return story.AuthorUserId == _currentUserService.UserId || _currentUserService.IsInRole("Admin");
    }

    private async Task<bool> CanDeleteStoryAsync(Story story, CancellationToken cancellationToken)
    {
        return story.AuthorUserId == _currentUserService.UserId || _currentUserService.IsInRole("Admin");
    }

    private async Task<List<object>> GetRecentInteractionsAsync(Guid storyId, CancellationToken cancellationToken)
    {
        // TODO: Implementiere Recent Interactions Loading
        return new List<object>();
    }

    private async Task InvalidateStoryCacheAsync(Guid storyId, CancellationToken cancellationToken)
    {
        await _cacheService.RemovePatternAsync($"story*:{storyId}*", cancellationToken);
    }

    private async Task AddStoryExperienceToCharacterAsync(Guid storyId, Guid characterId, TimeSpan readingTime, CancellationToken cancellationToken)
    {
        try
        {
            // TODO: Implementiere Character-Experience-Addition
            // Würde Character Application Service aufrufen
            _logger.LogDebug("Adding story experience to character {CharacterId} from story {StoryId}", characterId, storyId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to add story experience to character {CharacterId}", characterId);
        }
    }

    private async Task UpdateStoryRatingStatisticsAsync(Guid storyId, CancellationToken cancellationToken)
    {
        var averageRating = await _interactionRepository.GetAverageRatingAsync(storyId, cancellationToken);
        var statistics = await _interactionRepository.GetStoryStatisticsAsync(storyId, cancellationToken);
        
        if (statistics != null)
        {
            statistics.AverageRating = averageRating;
            await _interactionRepository.UpdateStoryStatisticsAsync(statistics, cancellationToken);
        }
    }

    private async Task ShareToCommunityAsync(Story story, ShareStoryDto dto, CancellationToken cancellationToken)
    {
        story.SetPublicVisibility(true);
        if (dto.ShareCategories?.Any() == true)
        {
            story.UpdateTags(dto.ShareCategories);
        }
        await _storyRepository.UpdateAsync(story, cancellationToken);
    }

    private async Task ShareDirectlyAsync(Story story, ShareStoryDto dto, CancellationToken cancellationToken)
    {
        // TODO: Implementiere Direct Sharing (z.B. per E-Mail)
        await Task.CompletedTask;
    }

    private async Task ShareAsExportAsync(Story story, ShareStoryDto dto, CancellationToken cancellationToken)
    {
        // TODO: Implementiere Export-basiertes Sharing
        await Task.CompletedTask;
    }

    private async Task<string> ExportAsPdfAsync(Story story, ExportStoryDto dto, CancellationToken cancellationToken)
    {
        // TODO: Implementiere PDF-Export
        return "pdf-export-url";
    }

    private async Task<string> ExportAsEpubAsync(Story story, ExportStoryDto dto, CancellationToken cancellationToken)
    {
        // TODO: Implementiere EPUB-Export
        return "epub-export-url";
    }

    private async Task<string> ExportAsHtmlAsync(Story story, ExportStoryDto dto, CancellationToken cancellationToken)
    {
        // TODO: Implementiere HTML-Export
        return "html-export-url";
    }

    private async Task<string> ExportAsDocxAsync(Story story, ExportStoryDto dto, CancellationToken cancellationToken)
    {
        // TODO: Implementiere DOCX-Export
        return "docx-export-url";
    }

    private async Task<string> ExportAsJsonAsync(Story story, ExportStoryDto dto, CancellationToken cancellationToken)
    {
        // TODO: Implementiere JSON-Export
        return "json-export-url";
    }

    // TODO: Implementiere die restlichen Interface-Methods
    public Task<StorySceneDto> UpdateStorySceneAsync(UpdateStorySceneDto dto, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteStoryAsync(Guid storyId, string reason, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<List<LearningGoalDto>> GenerateAdditionalLearningGoalsAsync(Guid storyId, int targetAge, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<List<StorySceneDto>> GenerateAlternativeEndingAsync(Guid storyId, string alternativePrompt, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<List<StoryDto>> GetUserStoriesAsync(Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<List<StoryCommunityDto>> GetCommunityStoriesAsync(StorySearchDto searchDto, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<List<StoryRecommendationDto>> GetStoryRecommendationsAsync(Guid userId, Guid? characterId = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<List<StoryDto>> SearchStoriesAsync(StorySearchDto searchDto, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<StoryAnalysisDto> AnalyzeStoryAsync(Guid storyId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<StoryStatisticsDto> GetStoryStatisticsAsync(Guid storyId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task UpdateStoryStatisticsAsync(UpdateStoryStatisticsDto dto, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task UpdateLearningGoalProgressAsync(UpdateLearningGoalProgressDto dto, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<List<LearningGoalProgressDto>> GetLearningGoalProgressAsync(Guid storyId, Guid userId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<List<StoryDto>> ProcessBatchOperationAsync(BatchStoryOperationDto dto, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}

// Extension Methods für Story Entity
public static class StoryExtensions
{
    public static void UpdateFromGeneration(this Story story, Story generatedStory)
    {
        // TODO: Diese Funktionalität müsste in Story Entity implementiert werden
    }

    public static void SetGenerationStatus(this Story story, StoryGenerationStatus status)
    {
        // TODO: Diese Funktionalität müsste in Story Entity implementiert werden
    }

    public static void SetCompletedAt(this Story story, DateTime completedAt)
    {
        // TODO: Diese Funktionalität müsste in Story Entity implementiert werden
    }

    public static bool AllowsRemixing(this Story story)
    {
        // TODO: Diese Funktionalität müsste in Story Entity implementiert werden
        return true; // Placeholder
    }

    public static void IncrementRemixCount(this Story story)
    {
        // TODO: Diese Funktionalität müsste in Story Entity implementiert werden
    }

    public static void UpdateBasicInfo(this Story story, string title, string summary)
    {
        // TODO: Diese Funktionalität müsste in Story Entity implementiert werden
    }

    public static void UpdateTags(this Story story, List<string> tags)
    {
        // TODO: Diese Funktionalität müsste in Story Entity implementiert werden
    }

    public static void SetPublicVisibility(this Story story, bool isPublic)
    {
        // TODO: Diese Funktionalität müsste in Story Entity implementiert werden
    }

    public static void AddImageUrls(this Story story, List<string> imageUrls)
    {
        // TODO: Diese Funktionalität müsste in Story Entity implementiert werden
    }
}