using Microsoft.Extensions.Logging;
using AutoMapper;
using Avatales.Application.Common.Interfaces;
using Avatales.Application.Interfaces.Repositories;
using Avatales.Application.Users.Queries;
using Avatales.Application.Users.DTOs;
using Avatales.Shared.Models;

namespace Avatales.Application.Users.Handlers;

/// <summary>
/// Query Handler: Benutzer nach ID abrufen
/// </summary>
public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserDetailDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetUserByIdQueryHandler> _logger;
    private readonly ICacheService _cacheService;

    public GetUserByIdQueryHandler(
        IUserRepository userRepository,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ILogger<GetUserByIdQueryHandler> logger,
        ICacheService cacheService)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<ApiResponse<UserDetailDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving user details for UserId: {UserId}", request.UserId);

            // 1. Berechtigung prüfen
            var authorizationResult = CheckAuthorization(request.UserId);
            if (!authorizationResult.IsSuccess)
            {
                return authorizationResult;
            }

            // 2. Cache prüfen
            var cacheKey = $"user_detail:{request.UserId}:{request.IncludeChildren}:{request.IncludePreferences}:{request.IncludeStatistics}";
            var cachedUser = await _cacheService.GetAsync<UserDetailDto>(cacheKey, cancellationToken);
            
            if (cachedUser != null)
            {
                _logger.LogDebug("User details retrieved from cache for UserId: {UserId}", request.UserId);
                return ApiResponse<UserDetailDto>.SuccessResult(cachedUser);
            }

            // 3. Benutzer aus Repository laden
            var user = request.IncludeChildren 
                ? await _userRepository.GetWithChildrenAsync(request.UserId, cancellationToken)
                : await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User not found with UserId: {UserId}", request.UserId);
                return ApiResponse<UserDetailDto>.NotFound("Benutzer");
            }

            // 4. DTO erstellen
            var userDetailDto = await MapToDetailDtoAsync(user, request, cancellationToken);

            // 5. In Cache speichern (5 Minuten)
            await _cacheService.SetAsync(cacheKey, userDetailDto, TimeSpan.FromMinutes(5), cancellationToken);

            _logger.LogInformation("User details successfully retrieved for UserId: {UserId}", request.UserId);
            return ApiResponse<UserDetailDto>.SuccessResult(userDetailDto);
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("Unauthorized access attempt for UserId: {UserId} by User: {CurrentUserId}", 
                request.UserId, _currentUserService.UserId);
            return ApiResponse<UserDetailDto>.Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user details for UserId: {UserId}", request.UserId);
            return ApiResponse<UserDetailDto>.FailureResult("Fehler beim Laden der Benutzerdaten");
        }
    }

    private ApiResponse<UserDetailDto> CheckAuthorization(Guid requestedUserId)
    {
        // Benutzer kann eigene Daten sehen oder Admin/Parent kann Kinder-Daten sehen
        if (_currentUserService.UserId == requestedUserId)
            return ApiResponse<UserDetailDto>.SuccessResult(null!);

        if (_currentUserService.IsInRole("Admin"))
            return ApiResponse<UserDetailDto>.SuccessResult(null!);

        // TODO: Prüfe ob Parent-Child Beziehung besteht
        // Vereinfacht für jetzt
        return ApiResponse<UserDetailDto>.Forbidden("Zugriff verweigert");
    }

    private async Task<UserDetailDto> MapToDetailDtoAsync(
        Domain.Users.Entities.User user, 
        GetUserByIdQuery request, 
        CancellationToken cancellationToken)
    {
        var userDetailDto = _mapper.Map<UserDetailDto>(user);

        // Zusätzliche Daten laden falls angefordert
        if (request.IncludePreferences)
        {
            userDetailDto.Preferences = await _userRepository.GetUserPreferencesAsync(user.Id, cancellationToken);
        }

        if (request.IncludeStatistics)
        {
            userDetailDto.Statistics = await BuildUserStatisticsAsync(user, cancellationToken);
        }

        if (request.IncludeChildren && user.ChildUserIds.Any())
        {
            var children = await _userRepository.GetChildrenByParentIdAsync(user.Id, cancellationToken);
            userDetailDto.ChildUsers = _mapper.Map<List<UserDto>>(children);
        }

        // Current Limits berechnen
        userDetailDto.CurrentLimits = MapToLimitsDto(user.GetCurrentLimits());
        userDetailDto.HasReachedDailyLimit = user.HasReachedDailyLimit();

        return userDetailDto;
    }

    private async Task<UserStatisticsDto> BuildUserStatisticsAsync(
        Domain.Users.Entities.User user, 
        CancellationToken cancellationToken)
    {
        // TODO: Implementiere Statistik-Sammlung aus verschiedenen Quellen
        // Für jetzt verwenden wir die Basis-Statistiken vom User
        return new UserStatisticsDto
        {
            CharactersCreated = user.CharactersCreated,
            StoriesGenerated = user.StoriesGenerated,
            MonthlyStoryCount = user.MonthlyStoryCount,
            MonthlyCountResetDate = user.MonthlyCountResetDate,
            LastActivityAt = user.LastLoginAt,
            CustomStatistics = user.Statistics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };
    }

    private UserLimitsDto MapToLimitsDto(UserLimits limits)
    {
        return new UserLimitsDto
        {
            MaxCharacters = limits.MaxCharacters,
            MonthlyStories = limits.MonthlyStories,
            StoriesRemaining = limits.MonthlyStories == -1 ? -1 : Math.Max(0, limits.MonthlyStories), // TODO: Berechne verbleibende Stories
            HasAdvancedFeatures = limits.HasAdvancedFeatures,
            HasImageGeneration = limits.HasImageGeneration,
            CanShareCharacters = limits.HasAdvancedFeatures,
            CanAccessCommunity = true, // Basis-Feature
            CanCreatePrivateStories = true
        };
    }
}

/// <summary>
/// Query Handler: Benutzer nach E-Mail abrufen
/// </summary>
public class GetUserByEmailQueryHandler : IQueryHandler<GetUserByEmailQuery, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetUserByEmailQueryHandler> _logger;

    public GetUserByEmailQueryHandler(
        IUserRepository userRepository,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ILogger<GetUserByEmailQueryHandler> logger)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ApiResponse<UserDto>> Handle(GetUserByEmailQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving user by email (masked): {Email}", MaskEmail(request.Email));

            // Nur Admins oder der Benutzer selbst kann nach E-Mail suchen
            if (!_currentUserService.IsInRole("Admin"))
            {
                _logger.LogWarning("Non-admin user attempted to search by email: {CurrentUserId}", _currentUserService.UserId);
                return ApiResponse<UserDto>.Forbidden("Zugriff verweigert");
            }

            var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

            if (user == null)
            {
                _logger.LogInformation("User not found with email: {Email}", MaskEmail(request.Email));
                return ApiResponse<UserDto>.NotFound("Benutzer");
            }

            // Prüfe Include-Deleted Flag
            if (user.IsDeleted && !request.IncludeDeleted)
            {
                _logger.LogInformation("Deleted user excluded from results: {Email}", MaskEmail(request.Email));
                return ApiResponse<UserDto>.NotFound("Benutzer");
            }

            var userDto = _mapper.Map<UserDto>(user);
            return ApiResponse<UserDto>.SuccessResult(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by email: {Email}", MaskEmail(request.Email));
            return ApiResponse<UserDto>.FailureResult("Fehler beim Laden der Benutzerdaten");
        }
    }

    private string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains('@'))
            return "***";

        var parts = email.Split('@');
        var localPart = parts[0];
        var domain = parts[1];

        var maskedLocal = localPart.Length <= 2 
            ? "***" 
            : localPart.Substring(0, 2) + "***";

        return $"{maskedLocal}@{domain}";
    }
}

/// <summary>
/// Query Handler: Aktueller Benutzer-Profile
/// </summary>
public class GetCurrentUserProfileQueryHandler : IQueryHandler<GetCurrentUserProfileQuery, UserProfileDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetCurrentUserProfileQueryHandler> _logger;
    private readonly ICacheService _cacheService;

    public GetCurrentUserProfileQueryHandler(
        IUserRepository userRepository,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ILogger<GetCurrentUserProfileQueryHandler> logger,
        ICacheService cacheService)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<ApiResponse<UserProfileDto>> Handle(GetCurrentUserProfileQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
            {
                _logger.LogWarning("Unauthenticated user attempted to access profile");
                return ApiResponse<UserProfileDto>.Unauthorized("Benutzer nicht authentifiziert");
            }

            var userId = _currentUserService.UserId.Value;
            _logger.LogInformation("Retrieving current user profile for UserId: {UserId}", userId);

            // Cache-Key für Current User Profile
            var cacheKey = $"current_user_profile:{userId}:{request.IncludeSubscriptionInfo}:{request.IncludeStatistics}";
            var cachedProfile = await _cacheService.GetAsync<UserProfileDto>(cacheKey, cancellationToken);

            if (cachedProfile != null)
            {
                _logger.LogDebug("Current user profile retrieved from cache for UserId: {UserId}", userId);
                return ApiResponse<UserProfileDto>.SuccessResult(cachedProfile);
            }

            // Benutzer mit Kindern laden
            var user = await _userRepository.GetWithChildrenAsync(userId, cancellationToken);

            if (user == null)
            {
                _logger.LogError("Current authenticated user not found in database: {UserId}", userId);
                return ApiResponse<UserProfileDto>.NotFound("Aktueller Benutzer");
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Inactive user attempted to access profile: {UserId}", userId);
                return ApiResponse<UserProfileDto>.Forbidden("Konto ist deaktiviert");
            }

            // Profile DTO erstellen
            var profileDto = await MapToProfileDtoAsync(user, request, cancellationToken);

            // In Cache speichern (2 Minuten für Current User)
            await _cacheService.SetAsync(cacheKey, profileDto, TimeSpan.FromMinutes(2), cancellationToken);

            _logger.LogInformation("Current user profile successfully retrieved for UserId: {UserId}", userId);
            return ApiResponse<UserProfileDto>.SuccessResult(profileDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current user profile for UserId: {UserId}", _currentUserService.UserId);
            return ApiResponse<UserProfileDto>.FailureResult("Fehler beim Laden des Benutzerprofils");
        }
    }

    private async Task<UserProfileDto> MapToProfileDtoAsync(
        Domain.Users.Entities.User user,
        GetCurrentUserProfileQuery request,
        CancellationToken cancellationToken)
    {
        var profileDto = _mapper.Map<UserProfileDto>(user);

        // Präferenzen laden
        profileDto.Preferences = user.Preferences.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        // Abonnement-Informationen
        if (request.IncludeSubscriptionInfo)
        {
            profileDto.IsSubscriptionActive = user.IsSubscriptionActive;
            profileDto.SubscriptionExpiresAt = user.SubscriptionExpiresAt;
        }

        // Statistiken
        if (request.IncludeStatistics)
        {
            profileDto.Statistics = new UserStatisticsDto
            {
                CharactersCreated = user.CharactersCreated,
                StoriesGenerated = user.StoriesGenerated,
                MonthlyStoryCount = user.MonthlyStoryCount,
                MonthlyCountResetDate = user.MonthlyCountResetDate,
                LastActivityAt = user.LastLoginAt,
                CustomStatistics = user.Statistics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };
        }

        // Current Limits
        var limits = user.GetCurrentLimits();
        profileDto.CurrentLimits = new UserLimitsDto
        {
            MaxCharacters = limits.MaxCharacters,
            MonthlyStories = limits.MonthlyStories,
            StoriesRemaining = limits.MonthlyStories == -1 ? -1 : Math.Max(0, limits.MonthlyStories - user.MonthlyStoryCount),
            HasAdvancedFeatures = limits.HasAdvancedFeatures,
            HasImageGeneration = limits.HasImageGeneration,
            CanShareCharacters = limits.HasAdvancedFeatures,
            CanAccessCommunity = true,
            CanCreatePrivateStories = true
        };

        // Kinder-Benutzer
        if (user.ChildUserIds.Any())
        {
            var children = await _userRepository.GetChildrenByParentIdAsync(user.Id, cancellationToken);
            profileDto.ChildUsers = _mapper.Map<List<UserDto>>(children);
        }

        return profileDto;
    }
}

/// <summary>
/// Query Handler: Kinder eines Eltern-Accounts abrufen
/// </summary>
public class GetUserChildrenQueryHandler : IQueryHandler<GetUserChildrenQuery, List<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetUserChildrenQueryHandler> _logger;

    public GetUserChildrenQueryHandler(
        IUserRepository userRepository,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ILogger<GetUserChildrenQueryHandler> logger)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ApiResponse<List<UserDto>>> Handle(GetUserChildrenQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving children for parent UserId: {ParentUserId}", request.ParentUserId);

            // Berechtigung prüfen - nur Parent oder Admin
            if (_currentUserService.UserId != request.ParentUserId && !_currentUserService.IsInRole("Admin"))
            {
                _logger.LogWarning("Unauthorized access to children for ParentUserId: {ParentUserId} by User: {CurrentUserId}",
                    request.ParentUserId, _currentUserService.UserId);
                return ApiResponse<List<UserDto>>.Forbidden("Zugriff verweigert");
            }

            // Parent-Benutzer existiert?
            var parentUser = await _userRepository.GetByIdAsync(request.ParentUserId, cancellationToken);
            if (parentUser == null)
            {
                return ApiResponse<List<UserDto>>.NotFound("Eltern-Benutzer");
            }

            if (parentUser.IsChildAccount)
            {
                return ApiResponse<List<UserDto>>.FailureResult("Kinder-Accounts können keine eigenen Kinder haben");
            }

            // Kinder laden
            var children = await _userRepository.GetChildrenByParentIdAsync(request.ParentUserId, cancellationToken);

            // Filter inaktive Kinder falls nicht explizit angefordert
            if (!request.IncludeInactive)
            {
                children = children.Where(c => c.IsActive).ToList();
            }

            var childrenDtos = _mapper.Map<List<UserDto>>(children);

            _logger.LogInformation("Successfully retrieved {Count} children for parent UserId: {ParentUserId}",
                childrenDtos.Count, request.ParentUserId);

            return ApiResponse<List<UserDto>>.SuccessResult(childrenDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving children for parent UserId: {ParentUserId}", request.ParentUserId);
            return ApiResponse<List<UserDto>>.FailureResult("Fehler beim Laden der Kinder-Accounts");
        }
    }
}

/// <summary>
/// Query Handler: Benutzer-Präferenzen abrufen
/// </summary>
public class GetUserPreferencesQueryHandler : IQueryHandler<GetUserPreferencesQuery, Dictionary<string, string>>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetUserPreferencesQueryHandler> _logger;
    private readonly ICacheService _cacheService;

    public GetUserPreferencesQueryHandler(
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        ILogger<GetUserPreferencesQueryHandler> logger,
        ICacheService cacheService)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<ApiResponse<Dictionary<string, string>>> Handle(GetUserPreferencesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving preferences for UserId: {UserId}", request.UserId);

            // Berechtigung prüfen
            if (_currentUserService.UserId != request.UserId && !_currentUserService.IsInRole("Admin"))
            {
                _logger.LogWarning("Unauthorized access to preferences for UserId: {UserId} by User: {CurrentUserId}",
                    request.UserId, _currentUserService.UserId);
                return ApiResponse<Dictionary<string, string>>.Forbidden("Zugriff verweigert");
            }

            // Cache prüfen
            var cacheKey = $"user_preferences:{request.UserId}";
            var cachedPreferences = await _cacheService.GetAsync<Dictionary<string, string>>(cacheKey, cancellationToken);

            if (cachedPreferences != null)
            {
                var filteredCached = FilterPreferences(cachedPreferences, request.SpecificKeys);
                return ApiResponse<Dictionary<string, string>>.SuccessResult(filteredCached);
            }

            // Aus Repository laden
            var preferences = await _userRepository.GetUserPreferencesAsync(request.UserId, cancellationToken);

            if (preferences == null)
            {
                _logger.LogWarning("User not found when retrieving preferences: {UserId}", request.UserId);
                return ApiResponse<Dictionary<string, string>>.NotFound("Benutzer");
            }

            // In Cache speichern (10 Minuten)
            await _cacheService.SetAsync(cacheKey, preferences, TimeSpan.FromMinutes(10), cancellationToken);

            // Filter anwenden falls spezifische Keys angefordert
            var filteredPreferences = FilterPreferences(preferences, request.SpecificKeys);

            _logger.LogInformation("Successfully retrieved {Count} preferences for UserId: {UserId}",
                filteredPreferences.Count, request.UserId);

            return ApiResponse<Dictionary<string, string>>.SuccessResult(filteredPreferences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving preferences for UserId: {UserId}", request.UserId);
            return ApiResponse<Dictionary<string, string>>.FailureResult("Fehler beim Laden der Benutzer-Einstellungen");
        }
    }

    private Dictionary<string, string> FilterPreferences(Dictionary<string, string> preferences, List<string>? specificKeys)
    {
        if (specificKeys == null || !specificKeys.Any())
            return preferences;

        return preferences
            .Where(kvp => specificKeys.Contains(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}

/// <summary>
/// Query Handler: Benutzer-Statistiken abrufen
/// </summary>
public class GetUserStatisticsQueryHandler : IQueryHandler<GetUserStatisticsQuery, UserStatisticsDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetUserStatisticsQueryHandler> _logger;
    private readonly ICacheService _cacheService;

    public GetUserStatisticsQueryHandler(
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        ILogger<GetUserStatisticsQueryHandler> logger,
        ICacheService cacheService)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<ApiResponse<UserStatisticsDto>> Handle(GetUserStatisticsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving statistics for UserId: {UserId}", request.UserId);

            // Berechtigung prüfen
            if (_currentUserService.UserId != request.UserId && !_currentUserService.IsInRole("Admin"))
            {
                return ApiResponse<UserStatisticsDto>.Forbidden("Zugriff verweigert");
            }

            // Cache prüfen
            var cacheKey = $"user_statistics:{request.UserId}:{request.FromDate}:{request.ToDate}";
            var cachedStats = await _cacheService.GetAsync<UserStatisticsDto>(cacheKey, cancellationToken);

            if (cachedStats != null)
            {
                return ApiResponse<UserStatisticsDto>.SuccessResult(cachedStats);
            }

            // Benutzer laden
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                return ApiResponse<UserStatisticsDto>.NotFound("Benutzer");
            }

            // Statistiken zusammenstellen
            var statistics = await BuildDetailedStatisticsAsync(user, request, cancellationToken);

            // In Cache speichern (5 Minuten)
            await _cacheService.SetAsync(cacheKey, statistics, TimeSpan.FromMinutes(5), cancellationToken);

            return ApiResponse<UserStatisticsDto>.SuccessResult(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving statistics for UserId: {UserId}", request.UserId);
            return ApiResponse<UserStatisticsDto>.FailureResult("Fehler beim Laden der Benutzer-Statistiken");
        }
    }

    private async Task<UserStatisticsDto> BuildDetailedStatisticsAsync(
        Domain.Users.Entities.User user,
        GetUserStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        // TODO: Hier würden wir aus verschiedenen Quellen Statistiken sammeln
        // Für jetzt verwenden wir die Basis-Statistiken
        
        return new UserStatisticsDto
        {
            CharactersCreated = user.CharactersCreated,
            StoriesGenerated = user.StoriesGenerated,
            MonthlyStoryCount = user.MonthlyStoryCount,
            MonthlyCountResetDate = user.MonthlyCountResetDate,
            LastActivityAt = user.LastLoginAt,
            DaysActive = CalculateDaysActive(user),
            CustomStatistics = user.Statistics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };
    }

    private int CalculateDaysActive(Domain.Users.Entities.User user)
    {
        if (!user.LastLoginAt.HasValue)
            return 0;

        return (DateTime.UtcNow.Date - user.CreatedAt.Date).Days + 1;
    }
}