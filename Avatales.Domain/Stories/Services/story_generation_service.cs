using Microsoft.Extensions.Logging;
using Avatales.Domain.Stories.Entities;
using Avatales.Domain.Stories.ValueObjects;
using Avatales.Domain.Characters.Entities;
using Avatales.Domain.Characters.Services;
using Avatales.Application.Common.Interfaces;
using Avatales.Shared.Models;
using System.Text.Json;

namespace Avatales.Domain.Stories.Services;

/// <summary>
/// Domain Service für AI-gestützte Story-Generierung
/// Kernfunktion der Avatales-App
/// </summary>
public interface IStoryGenerationService
{
    // Story Generation
    Task<StoryGenerationResult> GenerateStoryAsync(StoryGenerationRequest request, CancellationToken cancellationToken = default);
    Task<Story> ContinueStoryGenerationAsync(Guid storyId, string additionalPrompt, CancellationToken cancellationToken = default);
    Task<List<StoryScene>> RegenerateSceneAsync(Story story, int sceneNumber, string regenerationReason, CancellationToken cancellationToken = default);
    
    // Template-based Generation
    Task<Story> GenerateFromTemplateAsync(StoryTemplate template, Character character, Dictionary<string, object> variables, CancellationToken cancellationToken = default);
    Task<Story> RemixStoryAsync(Story originalStory, Character newCharacter, string remixPrompt, CancellationToken cancellationToken = default);
    
    // Adaptive Generation
    Task<Story> GenerateAdaptiveStoryAsync(Character character, LearningGoalCategory targetLearning, CancellationToken cancellationToken = default);
    Task<List<StoryScene>> GenerateInteractiveChoicesAsync(Story story, int sceneNumber, CancellationToken cancellationToken = default);
    
    // Content Enhancement
    Task<List<string>> GenerateImagePromptsAsync(Story story, CancellationToken cancellationToken = default);
    Task<List<LearningGoal>> GenerateLearningGoalsAsync(Story story, int targetAge, CancellationToken cancellationToken = default);
    Task<string> GenerateStoryTitleAsync(string content, StoryGenre genre, CancellationToken cancellationToken = default);
    Task<string> GenerateStorySummaryAsync(string content, int maxLength = 200, CancellationToken cancellationToken = default);
    
    // Quality & Safety
    Task<ContentModerationResult> ModerateStoryContentAsync(Story story, CancellationToken cancellationToken = default);
    Task<StoryQualityAnalysis> AnalyzeStoryQualityAsync(Story story, CancellationToken cancellationToken = default);
    Task<bool> IsContentAppropriateForAgeAsync(string content, int targetAge, CancellationToken cancellationToken = default);
}

public class StoryGenerationService : IStoryGenerationService
{
    private readonly IAIService _aiService;
    private readonly ICharacterMemoryService _memoryService;
    private readonly IContentModerationService _contentModerationService;
    private readonly ILogger<StoryGenerationService> _logger;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICacheService _cacheService;
    private readonly IFeatureFlagService _featureFlagService;

    public StoryGenerationService(
        IAIService aiService,
        ICharacterMemoryService memoryService,
        IContentModerationService contentModerationService,
        ILogger<StoryGenerationService> logger,
        IDateTimeProvider dateTimeProvider,
        ICacheService cacheService,
        IFeatureFlagService featureFlagService)
    {
        _aiService = aiService;
        _memoryService = memoryService;
        _contentModerationService = contentModerationService;
        _logger = logger;
        _dateTimeProvider = dateTimeProvider;
        _cacheService = cacheService;
        _featureFlagService = featureFlagService;
    }

    public async Task<StoryGenerationResult> GenerateStoryAsync(StoryGenerationRequest request, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting story generation for character {CharacterId} with prompt: {Prompt}", 
            request.Character.Id, request.UserPrompt);

        try
        {
            // 1. Validiere Request
            ValidateGenerationRequest(request);

            // 2. Hole relevante Character-Memories
            var relevantMemories = await _memoryService.GetRelevantMemoriesAsync(
                request.Character, 
                request.UserPrompt, 
                10, 
                cancellationToken);

            // 3. Erstelle AI-Prompt
            var aiPrompt = await BuildStoryPromptAsync(request, relevantMemories, cancellationToken);

            // 4. Generiere Story-Struktur
            var storyStructure = await GenerateStoryStructureAsync(aiPrompt, request, cancellationToken);

            // 5. Generiere Szenen
            var scenes = await GenerateScenesAsync(storyStructure, request, cancellationToken);

            // 6. Generiere Lernziele
            var learningGoals = await GenerateLearningGoalsAsync(storyStructure, request, cancellationToken);

            // 7. Erstelle Story-Entity
            var story = CreateStoryEntity(request, storyStructure, scenes, learningGoals);

            // 8. Content-Moderation
            var moderationResult = await ModerateStoryContentAsync(story, cancellationToken);
            story.UpdateModerationStatus(moderationResult.Status);

            // 9. Erstelle Character-Memory für diese Story-Erfahrung
            await CreateStoryMemoryAsync(request.Character, story, cancellationToken);

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("Story generation completed in {Duration}ms for character {CharacterId}", 
                duration.TotalMilliseconds, request.Character.Id);

            return new StoryGenerationResult
            {
                Story = story,
                GenerationDuration = duration,
                IsSuccess = true,
                WarningsOrSuggestions = moderationResult.Warnings,
                UsedMemories = relevantMemories,
                QualityScore = await CalculateQualityScoreAsync(story, cancellationToken)
            };
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "Story generation failed after {Duration}ms for character {CharacterId}", 
                duration.TotalMilliseconds, request.Character.Id);

            return new StoryGenerationResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                GenerationDuration = duration
            };
        }
    }

    public async Task<Story> ContinueStoryGenerationAsync(Guid storyId, string additionalPrompt, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Continuing story generation for story {StoryId} with additional prompt: {Prompt}", 
            storyId, additionalPrompt);

        // TODO: Implementiere Story-Fortsetzung
        // 1. Lade existierende Story
        // 2. Analysiere bisherigen Verlauf
        // 3. Generiere zusätzliche Szenen
        // 4. Aktualisiere Story
        
        throw new NotImplementedException("Story continuation will be implemented in next iteration");
    }

    public async Task<List<StoryScene>> RegenerateSceneAsync(Story story, int sceneNumber, string regenerationReason, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Regenerating scene {SceneNumber} for story {StoryId}: {Reason}", 
            sceneNumber, story.Id, regenerationReason);

        try
        {
            var existingScene = story.Scenes.FirstOrDefault(s => s.SceneNumber == sceneNumber);
            if (existingScene == null)
            {
                throw new ArgumentException($"Scene {sceneNumber} not found in story");
            }

            // Erstelle Kontext für Regenerierung
            var context = BuildSceneRegenerationContext(story, existingScene, regenerationReason);
            
            // Generiere neue Szene
            var newScenes = await GenerateSpecificSceneAsync(context, cancellationToken);
            
            _logger.LogInformation("Successfully regenerated scene {SceneNumber} for story {StoryId}", 
                sceneNumber, story.Id);

            return newScenes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to regenerate scene {SceneNumber} for story {StoryId}", 
                sceneNumber, story.Id);
            throw;
        }
    }

    public async Task<Story> GenerateFromTemplateAsync(StoryTemplate template, Character character, Dictionary<string, object> variables, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating story from template {TemplateId} for character {CharacterId}", 
            template.Id, character.Id);

        try
        {
            // 1. Parse Template-Struktur
            var templateStructure = JsonSerializer.Deserialize<StoryTemplateStructure>(template.Structure);
            if (templateStructure == null)
            {
                throw new InvalidOperationException("Invalid template structure");
            }

            // 2. Ersetze Template-Variablen
            var processedStructure = ProcessTemplateVariables(templateStructure, variables, character);

            // 3. Erstelle Generierungs-Request basierend auf Template
            var request = CreateRequestFromTemplate(template, character, processedStructure);

            // 4. Generiere Story
            var result = await GenerateStoryAsync(request, cancellationToken);

            if (!result.IsSuccess || result.Story == null)
            {
                throw new InvalidOperationException($"Template-based generation failed: {result.ErrorMessage}");
            }

            _logger.LogInformation("Successfully generated story from template {TemplateId}", template.Id);
            return result.Story;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate story from template {TemplateId}", template.Id);
            throw;
        }
    }

    public async Task<Story> RemixStoryAsync(Story originalStory, Character newCharacter, string remixPrompt, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating remix of story {OriginalStoryId} for character {CharacterId}", 
            originalStory.Id, newCharacter.Id);

        try
        {
            // 1. Analysiere Original-Story
            var originalAnalysis = await AnalyzeStoryForRemixAsync(originalStory, cancellationToken);

            // 2. Erstelle Remix-Prompt
            var remixRequest = CreateRemixRequest(originalStory, newCharacter, remixPrompt, originalAnalysis);

            // 3. Generiere Remix
            var result = await GenerateStoryAsync(remixRequest, cancellationToken);

            if (!result.IsSuccess || result.Story == null)
            {
                throw new InvalidOperationException($"Story remix failed: {result.ErrorMessage}");
            }

            // 4. Markiere als Remix
            result.Story.MarkAsRemix(originalStory.Id);

            _logger.LogInformation("Successfully created remix of story {OriginalStoryId}", originalStory.Id);
            return result.Story;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create remix of story {OriginalStoryId}", originalStory.Id);
            throw;
        }
    }

    public async Task<Story> GenerateAdaptiveStoryAsync(Character character, LearningGoalCategory targetLearning, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating adaptive story for character {CharacterId} targeting {LearningCategory}", 
            character.Id, targetLearning);

        try
        {
            // 1. Analysiere Character-Entwicklungsstand
            var characterAnalysis = await AnalyzeCharacterDevelopmentAsync(character, cancellationToken);

            // 2. Erstelle zielgerichtetes Prompt
            var adaptivePrompt = await CreateAdaptiveLearningPromptAsync(character, targetLearning, characterAnalysis, cancellationToken);

            // 3. Generiere Story
            var request = new StoryGenerationRequest
            {
                Character = character,
                UserPrompt = adaptivePrompt.Prompt,
                Genre = adaptivePrompt.RecommendedGenre,
                TargetLearningGoals = new List<LearningGoalCategory> { targetLearning },
                DifficultyLevel = adaptivePrompt.RecommendedDifficulty,
                EmotionalTone = adaptivePrompt.EmotionalTone,
                IncludeImages = true,
                EnableLearningMode = true
            };

            var result = await GenerateStoryAsync(request, cancellationToken);

            if (!result.IsSuccess || result.Story == null)
            {
                throw new InvalidOperationException($"Adaptive story generation failed: {result.ErrorMessage}");
            }

            _logger.LogInformation("Successfully generated adaptive story for learning goal {LearningCategory}", targetLearning);
            return result.Story;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate adaptive story for character {CharacterId}", character.Id);
            throw;
        }
    }

    public async Task<List<StoryScene>> GenerateInteractiveChoicesAsync(Story story, int sceneNumber, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating interactive choices for scene {SceneNumber} in story {StoryId}", 
            sceneNumber, story.Id);

        try
        {
            var scene = story.Scenes.FirstOrDefault(s => s.SceneNumber == sceneNumber);
            if (scene == null)
            {
                throw new ArgumentException($"Scene {sceneNumber} not found");
            }

            // 1. Analysiere Szenen-Kontext
            var context = AnalyzeSceneContext(story, scene);

            // 2. Generiere Wahlmöglichkeiten
            var choices = await GenerateSceneChoicesAsync(context, cancellationToken);

            // 3. Generiere mögliche Folge-Szenen
            var alternativeScenes = new List<StoryScene>();
            foreach (var choice in choices)
            {
                var alternativeScene = await GenerateChoiceConsequenceSceneAsync(story, scene, choice, cancellationToken);
                alternativeScenes.Add(alternativeScene);
            }

            _logger.LogInformation("Generated {ChoiceCount} interactive choices for scene {SceneNumber}", 
                choices.Count, sceneNumber);

            return alternativeScenes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate interactive choices for scene {SceneNumber} in story {StoryId}", 
                sceneNumber, story.Id);
            throw;
        }
    }

    public async Task<List<string>> GenerateImagePromptsAsync(Story story, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating image prompts for story {StoryId}", story.Id);

        try
        {
            var imagePrompts = new List<string>();

            foreach (var scene in story.Scenes)
            {
                var prompt = await GenerateImagePromptForSceneAsync(story, scene, cancellationToken);
                imagePrompts.Add(prompt);
            }

            _logger.LogInformation("Generated {Count} image prompts for story {StoryId}", imagePrompts.Count, story.Id);
            return imagePrompts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate image prompts for story {StoryId}", story.Id);
            throw;
        }
    }

    public async Task<List<LearningGoal>> GenerateLearningGoalsAsync(Story story, int targetAge, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating learning goals for story {StoryId} (target age: {Age})", story.Id, targetAge);

        try
        {
            // Analysiere Story-Inhalt für potentielle Lernziele
            var contentAnalysis = await AnalyzeStoryContentForLearningAsync(story, cancellationToken);
            
            // Generiere altersgerechte Lernziele
            var learningGoals = await CreateLearningGoalsFromAnalysisAsync(contentAnalysis, targetAge, cancellationToken);

            _logger.LogInformation("Generated {Count} learning goals for story {StoryId}", learningGoals.Count, story.Id);
            return learningGoals;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate learning goals for story {StoryId}", story.Id);
            throw;
        }
    }

    public async Task<string> GenerateStoryTitleAsync(string content, StoryGenre genre, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating story title for {Genre} genre", genre);

        try
        {
            var prompt = $"""
                Generate a compelling, child-friendly title for this {genre} story.
                The title should be engaging, age-appropriate, and capture the essence of the story.
                
                Story content summary:
                {content.Substring(0, Math.Min(500, content.Length))}...
                
                Requirements:
                - Maximum 8 words
                - Age-appropriate for children
                - Exciting and engaging
                - Genre-appropriate for {genre}
                
                Title:
                """;

            var response = await _aiService.GenerateTextAsync(prompt, new AIGenerationOptions
            {
                MaxTokens = 50,
                Temperature = 0.8f,
                Model = "gpt-4"
            }, cancellationToken);

            var title = response.Trim().Trim('"');
            
            _logger.LogDebug("Generated title: {Title}", title);
            return title;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate story title");
            return $"Eine {genre.ToString().ToDisplayString()} Geschichte"; // Fallback
        }
    }

    public async Task<string> GenerateStorySummaryAsync(string content, int maxLength = 200, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating story summary (max length: {MaxLength})", maxLength);

        try
        {
            var prompt = $"""
                Create a brief, engaging summary of this children's story.
                
                Story content:
                {content}
                
                Requirements:
                - Maximum {maxLength} characters
                - Child-friendly language
                - Capture the main plot and characters
                - Exciting but appropriate
                
                Summary:
                """;

            var response = await _aiService.GenerateTextAsync(prompt, new AIGenerationOptions
            {
                MaxTokens = maxLength / 2, // Rough token estimation
                Temperature = 0.7f,
                Model = "gpt-4"
            }, cancellationToken);

            var summary = response.Trim();
            
            if (summary.Length > maxLength)
            {
                summary = summary.Substring(0, maxLength - 3) + "...";
            }

            _logger.LogDebug("Generated summary of {Length} characters", summary.Length);
            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate story summary");
            return "Eine spannende Geschichte voller Abenteuer und Freundschaft."; // Fallback
        }
    }

    public async Task<ContentModerationResult> ModerateStoryContentAsync(Story story, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Moderating content for story {StoryId}", story.Id);

        try
        {
            var fullContent = string.Join("\n", story.Scenes.Select(s => s.Content));
            
            var moderationResult = await _contentModerationService.ModerateTextAsync(fullContent, new ContentModerationOptions
            {
                CheckForViolence = true,
                CheckForInappropriateLanguage = true,
                CheckForAdultContent = true,
                TargetAge = story.RecommendedAge,
                StrictMode = true
            }, cancellationToken);

            _logger.LogInformation("Content moderation completed for story {StoryId}. Status: {Status}", 
                story.Id, moderationResult.Status);

            return moderationResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to moderate content for story {StoryId}", story.Id);
            
            // Fallback: Mark als pending review
            return new ContentModerationResult
            {
                Status = ContentModerationStatus.Pending,
                Warnings = new List<string> { "Automatic moderation failed, manual review required" }
            };
        }
    }

    public async Task<StoryQualityAnalysis> AnalyzeStoryQualityAsync(Story story, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing quality for story {StoryId}", story.Id);

        try
        {
            var analysis = new StoryQualityAnalysis
            {
                StoryId = story.Id,
                OverallScore = 0f,
                Criteria = new Dictionary<string, float>()
            };

            // 1. Strukturelle Qualität
            analysis.Criteria["Structure"] = AnalyzeStructuralQuality(story);
            
            // 2. Sprach-Qualität
            analysis.Criteria["Language"] = await AnalyzeLanguageQualityAsync(story, cancellationToken);
            
            // 3. Pädagogischer Wert
            analysis.Criteria["Educational"] = AnalyzeEducationalValue(story);
            
            // 4. Altersangemessenheit
            analysis.Criteria["AgeAppropriate"] = await AnalyzeAgeAppropriatenessAsync(story, cancellationToken);
            
            // 5. Charakter-Integration
            analysis.Criteria["CharacterIntegration"] = AnalyzeCharacterIntegration(story);

            // Berechne Gesamt-Score
            analysis.OverallScore = analysis.Criteria.Values.Average();
            
            // Generiere Verbesserungsvorschläge
            analysis.Improvements = GenerateImprovementSuggestions(analysis);

            _logger.LogInformation("Quality analysis completed for story {StoryId}. Overall score: {Score:F2}", 
                story.Id, analysis.OverallScore);

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze quality for story {StoryId}", story.Id);
            throw;
        }
    }

    public async Task<bool> IsContentAppropriateForAgeAsync(string content, int targetAge, CancellationToken cancellationToken = default)
    {
        try
        {
            var moderationResult = await _contentModerationService.ModerateTextAsync(content, new ContentModerationOptions
            {
                TargetAge = targetAge,
                StrictMode = true
            }, cancellationToken);

            return moderationResult.Status == ContentModerationStatus.Approved;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check age appropriateness for target age {Age}", targetAge);
            return false; // Bei Unsicherheit konservativ sein
        }
    }

    // Private Helper Methods

    private void ValidateGenerationRequest(StoryGenerationRequest request)
    {
        if (request.Character == null)
            throw new ArgumentException("Character is required for story generation");
        
        if (string.IsNullOrWhiteSpace(request.UserPrompt))
            throw new ArgumentException("User prompt is required");
        
        if (request.UserPrompt.Length > 500)
            throw new ArgumentException("User prompt too long (max 500 characters)");
        
        if (!request.UserPrompt.IsChildFriendly())
            throw new ArgumentException("User prompt contains inappropriate content");
    }

    private async Task<AIPrompt> BuildStoryPromptAsync(StoryGenerationRequest request, List<CharacterMemory> memories, CancellationToken cancellationToken)
    {
        var character = request.Character;
        var dna = character.DNA;

        var prompt = $"""
            You are a professional children's story writer creating a personalized story.
            
            CHARACTER INFORMATION:
            - Name: {character.Name}
            - Description: {character.Description}
            - Archetype: {dna.Archetype}
            - Level: {character.Level}
            - Primary Motivation: {dna.PrimaryMotivation}
            - Core Fear: {dna.CoreFear}
            - Learning Style: {dna.LearningStyle}
            - Preferred Genres: {string.Join(", ", dna.PreferredStoryGenres)}
            - Strong Traits: {string.Join(", ", character.Traits.Where(t => t.CurrentValue >= 7).Select(t => t.TraitType.ToDisplayString()))}
            
            RELEVANT MEMORIES:
            {string.Join("\n", memories.Take(5).Select(m => $"- {m.Title}: {m.Summary}"))}
            
            STORY REQUIREMENTS:
            - Genre: {request.Genre}
            - User Prompt: "{request.UserPrompt}"
            - Target Age: {request.Character.Age ?? 7}
            - Difficulty Level: {request.DifficultyLevel}/5
            - Emotional Tone: {request.EmotionalTone ?? "positive"}
            - Include Learning Goals: {request.EnableLearningMode}
            - Preferred Happy Ending: {dna.PrefersHappyEndings}
            
            AVOID TOPICS: {string.Join(", ", dna.AvoidedTopics.Concat(request.AvoidTopics ?? new List<string>()))}
            
            Create a {request.TargetWordCount ?? 300}-word story that:
            1. Features {character.Name} as the main character
            2. Incorporates their personality traits and memories
            3. Addresses the user's prompt creatively
            4. Is age-appropriate and engaging
            5. Promotes positive values and learning
            6. Has a clear beginning, middle, and end
            
            Structure the response as JSON with this format:
            {{
                "title": "Story Title",
                "summary": "Brief story summary",
                "scenes": [
                    {{
                        "sceneNumber": 1,
                        "title": "Scene Title",
                        "content": "Scene content...",
                        "imagePrompt": "Description for AI image generation",
                        "primaryEmotion": "Happy|Excited|Curious|etc",
                        "keyWords": ["word1", "word2"],
                        "characterActions": ["action1", "action2"],
                        "traitInfluences": {{"Courage": 0.8, "Kindness": 0.6}}
                    }}
                ],
                "learningGoals": [
                    {{
                        "title": "Learning Goal Title",
                        "category": "Vocabulary|SocialSkills|etc",
                        "description": "What the child will learn"
                    }}
                ],
                "wordCount": 300,
                "readingTimeMinutes": 3
            }}
            """;

        return new AIPrompt { Content = prompt };
    }

    private async Task<StoryStructure> GenerateStoryStructureAsync(AIPrompt prompt, StoryGenerationRequest request, CancellationToken cancellationToken)
    {
        var response = await _aiService.GenerateStructuredTextAsync(prompt.Content, new AIGenerationOptions
        {
            MaxTokens = 2000,
            Temperature = 0.8f,
            Model = "gpt-4",
            ResponseFormat = "json"
        }, cancellationToken);

        var storyStructure = JsonSerializer.Deserialize<StoryStructure>(response);
        if (storyStructure == null)
        {
            throw new InvalidOperationException("Failed to parse AI response into story structure");
        }

        return storyStructure;
    }

    private async Task<List<StoryScene>> GenerateScenesAsync(StoryStructure structure, StoryGenerationRequest request, CancellationToken cancellationToken)
    {
        var scenes = new List<StoryScene>();

        foreach (var sceneData in structure.Scenes)
        {
            var scene = new StoryScene(
                sceneNumber: sceneData.SceneNumber,
                title: sceneData.Title,
                content: sceneData.Content,
                primaryEmotion: Enum.Parse<AvatarEmotion>(sceneData.PrimaryEmotion),
                difficultyLevel: request.DifficultyLevel switch
                {
                    1 => LearningDifficulty.Beginner,
                    2 => LearningDifficulty.Elementary,
                    3 => LearningDifficulty.Intermediate,
                    4 => LearningDifficulty.Advanced,
                    _ => LearningDifficulty.Elementary
                });

            // Setze Image Prompt
            scene.SetImage(null, sceneData.ImagePrompt);

            // Füge Trait-Einflüsse hinzu
            foreach (var influence in sceneData.TraitInfluences)
            {
                if (Enum.TryParse<CharacterTraitType>(influence.Key, out var traitType))
                {
                    scene.AddTraitInfluence(traitType, influence.Value);
                }
            }

            scenes.Add(scene);
        }

        return scenes;
    }

    private async Task<List<LearningGoal>> GenerateLearningGoalsAsync(StoryStructure structure, StoryGenerationRequest request, CancellationToken cancellationToken)
    {
        var learningGoals = new List<LearningGoal>();

        foreach (var goalData in structure.LearningGoals)
        {
            if (Enum.TryParse<LearningGoalCategory>(goalData.Category, out var category))
            {
                var learningGoal = new LearningGoal(
                    title: goalData.Title,
                    description: goalData.Description,
                    category: category,
                    difficulty: LearningDifficulty.Elementary, // TODO: Map from request
                    targetAge: request.Character.Age ?? 7);

                learningGoals.Add(learningGoal);
            }
        }

        return learningGoals;
    }

    private Story CreateStoryEntity(StoryGenerationRequest request, StoryStructure structure, List<StoryScene> scenes, List<LearningGoal> learningGoals)
    {
        var story = new Story(
            title: structure.Title,
            summary: structure.Summary,
            mainCharacterId: request.Character.Id,
            authorUserId: request.Character.OwnerId,
            genre: request.Genre,
            userPrompt: request.UserPrompt);

        // Füge Szenen hinzu
        foreach (var scene in scenes)
        {
            story.AddScene(scene);
        }

        // Füge Lernziele hinzu
        foreach (var goal in learningGoals)
        {
            story.AddLearningGoal(goal);
        }

        // Setze Eigenschaften
        story.SetReadingTime(structure.ReadingTimeMinutes);
        story.SetWordCount(structure.WordCount);
        story.SetRecommendedAge(request.Character.Age ?? 7);

        if (request.IncludeImages)
        {
            story.EnableImages();
        }

        if (request.EnableLearningMode)
        {
            story.EnableLearningMode();
        }

        return story;
    }

    private async Task CreateStoryMemoryAsync(Character character, Story story, CancellationToken cancellationToken)
    {
        var memory = new CharacterMemory(
            title: $"Geschichte erlebt: {story.Title}",
            summary: $"Ich habe eine {story.Genre.ToDisplayString()} Geschichte erlebt: {story.Summary}",
            memoryType: MemoryType.StoryExperience,
            importance: 6, // Mittlere Wichtigkeit für Story-Erfahrungen
            storyId: story.Id);

        memory.AddTag("Story");
        memory.AddTag(story.Genre.ToString());
        memory.AddEmotionalContext("Story Experience");

        await _memoryService.AddMemoryAsync(character, memory, cancellationToken);
    }

    private async Task<float> CalculateQualityScoreAsync(Story story, CancellationToken cancellationToken)
    {
        try
        {
            var analysis = await AnalyzeStoryQualityAsync(story, cancellationToken);
            return analysis.OverallScore;
        }
        catch
        {
            return 0.7f; // Fallback-Score
        }
    }

    // Weitere Helper-Methods würden hier implementiert werden...
    // (Aus Platzgründen nicht alle vollständig ausgeführt)

    private SceneRegenerationContext BuildSceneRegenerationContext(Story story, StoryScene scene, string reason)
    {
        return new SceneRegenerationContext
        {
            Story = story,
            OriginalScene = scene,
            RegenerationReason = reason,
            PreviousScenes = story.Scenes.Where(s => s.SceneNumber < scene.SceneNumber).ToList(),
            NextScenes = story.Scenes.Where(s => s.SceneNumber > scene.SceneNumber).ToList()
        };
    }

    private async Task<List<StoryScene>> GenerateSpecificSceneAsync(SceneRegenerationContext context, CancellationToken cancellationToken)
    {
        // TODO: Implementiere spezifische Szenen-Regenerierung
        throw new NotImplementedException();
    }

    private float AnalyzeStructuralQuality(Story story)
    {
        float score = 0.8f; // Base score
        
        // Prüfe Szenen-Anzahl
        if (story.Scenes.Count < 3) score -= 0.2f;
        if (story.Scenes.Count > 8) score -= 0.1f;
        
        // Prüfe Wort-Verteilung
        var avgWordsPerScene = story.Scenes.Average(s => s.WordCount);
        if (avgWordsPerScene < 50 || avgWordsPerScene > 200) score -= 0.1f;
        
        return Math.Max(0f, Math.Min(1f, score));
    }

    private async Task<float> AnalyzeLanguageQualityAsync(Story story, CancellationToken cancellationToken)
    {
        // TODO: Implementiere Sprach-Qualitäts-Analyse
        return 0.8f; // Placeholder
    }

    private float AnalyzeEducationalValue(Story story)
    {
        float score = 0.5f; // Base score
        
        if (story.LearningGoals.Any()) score += 0.3f;
        if (story.HasLearningMode) score += 0.2f;
        
        return Math.Min(1f, score);
    }

    private async Task<float> AnalyzeAgeAppropriatenessAsync(Story story, CancellationToken cancellationToken)
    {
        var content = string.Join(" ", story.Scenes.Select(s => s.Content));
        var isAppropriate = await IsContentAppropriateForAgeAsync(content, story.RecommendedAge, cancellationToken);
        return isAppropriate ? 1f : 0.3f;
    }

    private float AnalyzeCharacterIntegration(Story story)
    {
        // TODO: Analysiere wie gut der Charakter in die Story integriert ist
        return 0.8f; // Placeholder
    }

    private List<string> GenerateImprovementSuggestions(StoryQualityAnalysis analysis)
    {
        var suggestions = new List<string>();
        
        if (analysis.Criteria["Structure"] < 0.7f)
            suggestions.Add("Verbessere die Story-Struktur mit klareren Szenen");
        
        if (analysis.Criteria["Educational"] < 0.6f)
            suggestions.Add("Füge mehr Lernelemente hinzu");
        
        return suggestions;
    }
}

// Supporting Classes and Records

public record StoryGenerationRequest
{
    public Character Character { get; init; } = null!;
    public string UserPrompt { get; init; } = string.Empty;
    public StoryGenre Genre { get; init; }
    public List<LearningGoalCategory>? TargetLearningGoals { get; init; }
    public int? TargetWordCount { get; init; }
    public int DifficultyLevel { get; init; } = 3;
    public string? EmotionalTone { get; init; }
    public List<string>? AvoidTopics { get; init; }
    public bool IncludeImages { get; init; } = true;
    public bool EnableLearningMode { get; init; } = true;
}

public class StoryGenerationResult
{
    public Story? Story { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan GenerationDuration { get; set; }
    public List<string> WarningsOrSuggestions { get; set; } = new();
    public List<CharacterMemory> UsedMemories { get; set; } = new();
    public float QualityScore { get; set; }
}

public class StoryStructure
{
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<SceneData> Scenes { get; set; } = new();
    public List<LearningGoalData> LearningGoals { get; set; } = new();
    public int WordCount { get; set; }
    public int ReadingTimeMinutes { get; set; }
}

public class SceneData
{
    public int SceneNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string ImagePrompt { get; set; } = string.Empty;
    public string PrimaryEmotion { get; set; } = string.Empty;
    public List<string> KeyWords { get; set; } = new();
    public List<string> CharacterActions { get; set; } = new();
    public Dictionary<string, float> TraitInfluences { get; set; } = new();
}

public class LearningGoalData
{
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class AIPrompt
{
    public string Content { get; set; } = string.Empty;
}

public class StoryQualityAnalysis
{
    public Guid StoryId { get; set; }
    public float OverallScore { get; set; }
    public Dictionary<string, float> Criteria { get; set; } = new();
    public List<string> Improvements { get; set; } = new();
}

public class SceneRegenerationContext
{
    public Story Story { get; set; } = null!;
    public StoryScene OriginalScene { get; set; } = null!;
    public string RegenerationReason { get; set; } = string.Empty;
    public List<StoryScene> PreviousScenes { get; set; } = new();
    public List<StoryScene> NextScenes { get; set; } = new();
}