using FluentValidation;
using Microsoft.Extensions.Logging;
using Avatales.Application.Common.Interfaces;
using Avatales.Application.Interfaces.Repositories;
using Avatales.Application.Users.Commands;
using Avatales.Application.Users.DTOs;
using Avatales.Domain.Users.Entities;
using Avatales.Shared.Models;
using Avatales.Shared.Extensions;

namespace Avatales.Application.Users.Handlers;

/// <summary>
/// Command Handler für Benutzer-Registrierung
/// Implementiert die vollständige Geschäftslogik für neue Benutzer-Registrierung
/// </summary>
public class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly IEmailService _emailService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly IValidator<RegisterUserCommand> _validator;
    private readonly ILogger<RegisterUserCommandHandler> _logger;
    private readonly IAuditService _auditService;
    private readonly IRateLimitService _rateLimitService;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHashingService passwordHashingService,
        IEmailService emailService,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IDomainEventDispatcher domainEventDispatcher,
        IValidator<RegisterUserCommand> validator,
        ILogger<RegisterUserCommandHandler> logger,
        IAuditService auditService,
        IRateLimitService rateLimitService)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHashingService = passwordHashingService;
        _emailService = emailService;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _domainEventDispatcher = domainEventDispatcher;
        _validator = validator;
        _logger = logger;
        _auditService = auditService;
        _rateLimitService = rateLimitService;
    }

    public async Task<ApiResponse<UserDto>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting user registration for email: {Email}", request.Email);

            // 1. Rate Limiting prüfen
            var rateLimitResult = await CheckRateLimitAsync(request, cancellationToken);
            if (!rateLimitResult.IsSuccess)
            {
                return rateLimitResult;
            }

            // 2. Eingabe-Validierung
            var validationResult = await ValidateRequestAsync(request, cancellationToken);
            if (!validationResult.IsSuccess)
            {
                return validationResult;
            }

            // 3. Geschäftsregeln prüfen
            var businessRulesResult = await ValidateBusinessRulesAsync(request, cancellationToken);
            if (!businessRulesResult.IsSuccess)
            {
                return businessRulesResult;
            }

            // 4. Transaktion starten
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // 5. Benutzer erstellen
                var user = await CreateUserAsync(request, cancellationToken);

                // 6. Benutzer speichern
                var savedUser = await _userRepository.AddAsync(user, cancellationToken);

                // 7. Änderungen persistieren
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 8. Domain Events verarbeiten
                await _domainEventDispatcher.DispatchEventsAsync(savedUser.DomainEvents, cancellationToken);
                savedUser.ClearDomainEvents();

                // 9. Transaktion committen
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // 10. Nach-Registrierungs-Aktivitäten
                await HandlePostRegistrationAsync(savedUser, request, cancellationToken);

                // 11. Audit-Log erstellen
                await _auditService.LogActionAsync(
                    "UserRegistration",
                    savedUser.Id,
                    "User successfully registered",
                    new { Email = request.Email, Role = request.Role },
                    savedUser.Id,
                    cancellationToken);

                _logger.LogInformation("User registration completed successfully for: {Email}, UserId: {UserId}", 
                    request.Email, savedUser.Id);

                // 12. DTO erstellen und zurückgeben
                var userDto = MapToDto(savedUser);
                return ApiResponse<UserDto>.SuccessResult(userDto, "Registrierung erfolgreich abgeschlossen");
            }
            catch (Exception ex)
            {
                // Rollback bei Fehlern
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
        catch (ValidationException vex)
        {
            _logger.LogWarning("Validation failed during user registration: {Errors}", 
                string.Join(", ", vex.Errors.Select(e => e.ErrorMessage)));
            
            return ApiResponse<UserDto>.ValidationFailure(
                vex.Errors.Select(e => e.ErrorMessage).ToList());
        }
        catch (InvalidOperationException iex)
        {
            _logger.LogWarning("Business rule violation during user registration: {Message}", iex.Message);
            return ApiResponse<UserDto>.FailureResult("Registrierung nicht möglich", iex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during user registration for email: {Email}", request.Email);
            
            await _auditService.LogActionAsync(
                "UserRegistrationFailed",
                null,
                "User registration failed with error",
                new { Email = request.Email, Error = ex.Message },
                cancellationToken: cancellationToken);

            return ApiResponse<UserDto>.FailureResult(
                "Ein unerwarteter Fehler ist aufgetreten", 
                "Bitte versuchen Sie es später erneut oder kontaktieren Sie den Support");
        }
    }

    private async Task<ApiResponse<UserDto>> CheckRateLimitAsync(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // Rate Limiting für Registrierungen (z.B. max 3 pro Stunde pro IP)
        var rateLimitKey = $"registration:{request.IpAddress}";
        var isAllowed = await _rateLimitService.IsAllowedAsync(rateLimitKey, 3, TimeSpan.FromHours(1), cancellationToken);
        
        if (!isAllowed)
        {
            _logger.LogWarning("Rate limit exceeded for registration from IP: {IpAddress}", request.IpAddress);
            return ApiResponse<UserDto>.FailureResult(
                "Zu viele Registrierungsversuche", 
                "Bitte warten Sie eine Stunde bevor Sie es erneut versuchen");
        }

        return ApiResponse<UserDto>.SuccessResult(null!);
    }

    private async Task<ApiResponse<UserDto>> ValidateRequestAsync(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        
        if (!validationResult.IsValid)
        {
            return ApiResponse<UserDto>.ValidationFailure(
                validationResult.Errors.Select(e => e.ErrorMessage).ToList());
        }

        // Zusätzliche Validierungen
        if (!request.Email.IsChildFriendly())
        {
            return ApiResponse<UserDto>.FailureResult("Ungültige E-Mail-Adresse");
        }

        if (!request.FirstName.IsChildFriendly() || !request.LastName.IsChildFriendly())
        {
            return ApiResponse<UserDto>.FailureResult("Name enthält ungeeignete Inhalte");
        }

        return ApiResponse<UserDto>.SuccessResult(null!);
    }

    private async Task<ApiResponse<UserDto>> ValidateBusinessRulesAsync(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // 1. E-Mail bereits vorhanden?
        var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser != null)
        {
            _logger.LogWarning("Registration attempt with existing email: {Email}", request.Email);
            return ApiResponse<UserDto>.FailureResult(
                "E-Mail-Adresse bereits registriert", 
                "Diese E-Mail-Adresse ist bereits mit einem Account verknüpft");
        }

        // 2. Altersvalidierung
        if (request.DateOfBirth.HasValue)
        {
            var age = request.DateOfBirth.Value.CalculateAge();
            
            if (age < 13 && request.Role != UserRole.FamilyMember)
            {
                return ApiResponse<UserDto>.FailureResult(
                    "Alter nicht ausreichend", 
                    "Kinder unter 13 Jahren benötigen einen Eltern-Account");
            }

            if (age > 120)
            {
                return ApiResponse<UserDto>.FailureResult("Ungültiges Geburtsdatum");
            }
        }

        // 3. Geschäftsbedingungen akzeptiert?
        if (!request.AcceptTerms || !request.AcceptPrivacyPolicy)
        {
            return ApiResponse<UserDto>.FailureResult(
                "Zustimmung erforderlich", 
                "Sie müssen den Geschäftsbedingungen und der Datenschutzerklärung zustimmen");
        }

        // 4. Referral-Code validieren (falls vorhanden)
        if (!string.IsNullOrWhiteSpace(request.ReferralCode))
        {
            var isValidReferral = await ValidateReferralCodeAsync(request.ReferralCode, cancellationToken);
            if (!isValidReferral)
            {
                return ApiResponse<UserDto>.FailureResult("Ungültiger Empfehlungscode");
            }
        }

        return ApiResponse<UserDto>.SuccessResult(null!);
    }

    private async Task<User> CreateUserAsync(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // Passwort hashen
        var passwordHash = await _passwordHashingService.HashPasswordAsync(request.Password);

        // Benutzer erstellen
        var user = new User(
            email: request.Email.ToLower().Trim(),
            passwordHash: passwordHash,
            firstName: request.FirstName.Trim(),
            lastName: request.LastName.Trim(),
            role: request.Role);

        // Optionale Eigenschaften setzen
        if (request.DateOfBirth.HasValue)
        {
            user.UpdateProfile(
                firstName: request.FirstName,
                lastName: request.LastName,
                dateOfBirth: request.DateOfBirth);
        }

        // Präferenzen setzen
        if (!string.IsNullOrWhiteSpace(request.PreferredLanguage))
        {
            user.SetPreference("language", request.PreferredLanguage);
        }

        // Referral-Code verarbeiten
        if (!string.IsNullOrWhiteSpace(request.ReferralCode))
        {
            user.SetPreference("referral_code", request.ReferralCode);
            await ProcessReferralAsync(request.ReferralCode, user.Id, cancellationToken);
        }

        // Tracking-Informationen
        user.SetCreatedBy(user.Id); // Selbst-Registrierung

        return user;
    }

    private async Task HandlePostRegistrationAsync(User user, RegisterUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Willkommens-E-Mail senden (ohne await für bessere Performance)
            _ = Task.Run(async () =>
            {
                await SendWelcomeEmailAsync(user, request.PreferredLanguage ?? "de-DE", cancellationToken);
            }, cancellationToken);

            // 2. E-Mail-Verifikation senden (falls erforderlich)
            if (!user.IsEmailVerified && user.Role != UserRole.FamilyMember)
            {
                _ = Task.Run(async () =>
                {
                    await SendEmailVerificationAsync(user, cancellationToken);
                }, cancellationToken);
            }

            // 3. Default-Charakter erstellen für bestimmte Rollen
            if (user.Role == UserRole.FamilyMember && user.Age.HasValue && user.Age <= 12)
            {
                _ = Task.Run(async () =>
                {
                    await CreateDefaultCharacterAsync(user, cancellationToken);
                }, cancellationToken);
            }

            // 4. Analytics-Event senden
            await _auditService.TrackUserActionAsync(
                user.Id,
                "UserRegistered",
                new Dictionary<string, object>
                {
                    ["Role"] = user.Role.ToString(),
                    ["Age"] = user.Age ?? 0,
                    ["HasReferral"] = !string.IsNullOrWhiteSpace(request.ReferralCode),
                    ["Language"] = request.PreferredLanguage ?? "de-DE"
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Post-registration activities failed for user: {UserId}", user.Id);
            // Fehler werden hier nicht weitergeworfen, da die Registrierung bereits erfolgreich war
        }
    }

    private async Task<bool> ValidateReferralCodeAsync(string referralCode, CancellationToken cancellationToken)
    {
        // TODO: Implementiere Referral-Code-Validierung
        // Könnte gegen eine ReferralCode-Entität oder externe Service validieren
        await Task.Delay(1, cancellationToken); // Placeholder
        return true; // Vorläufig alle Codes als gültig betrachten
    }

    private async Task ProcessReferralAsync(string referralCode, Guid newUserId, CancellationToken cancellationToken)
    {
        // TODO: Implementiere Referral-Verarbeitung
        // - Finde Referrer
        // - Vergebe Belohnungen
        // - Erstelle Referral-Datensatz
        await Task.Delay(1, cancellationToken); // Placeholder
    }

    private async Task SendWelcomeEmailAsync(User user, string language, CancellationToken cancellationToken)
    {
        try
        {
            var emailTemplate = language.StartsWith("en") ? "welcome_en" : "welcome_de";
            
            await _emailService.SendTemplateEmailAsync(
                to: user.Email,
                templateName: emailTemplate,
                templateData: new Dictionary<string, object>
                {
                    ["FirstName"] = user.FirstName,
                    ["LastName"] = user.LastName,
                    ["LoginUrl"] = "https://avatales.app/login" // TODO: Aus Konfiguration laden
                },
                cancellationToken: cancellationToken);

            _logger.LogInformation("Welcome email sent to: {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send welcome email to: {Email}", user.Email);
        }
    }

    private async Task SendEmailVerificationAsync(User user, CancellationToken cancellationToken)
    {
        try
        {
            // Verifikations-Token generieren
            var verificationToken = Guid.NewGuid().ToString("N");
            
            // TODO: Token speichern (z.B. in Cache oder Datenbank)
            
            var verificationUrl = $"https://avatales.app/verify-email?token={verificationToken}&userId={user.Id}";
            
            await _emailService.SendTemplateEmailAsync(
                to: user.Email,
                templateName: "email_verification",
                templateData: new Dictionary<string, object>
                {
                    ["FirstName"] = user.FirstName,
                    ["VerificationUrl"] = verificationUrl
                },
                cancellationToken: cancellationToken);

            _logger.LogInformation("Email verification sent to: {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send email verification to: {Email}", user.Email);
        }
    }

    private async Task CreateDefaultCharacterAsync(User user, CancellationToken cancellationToken)
    {
        try
        {
            // TODO: Implementiere Default-Charakter-Erstellung
            // - Verwende MediateR um CreateCharacterCommand zu senden
            // - Erstelle altersgerechten Standard-Charakter
            await Task.Delay(1, cancellationToken); // Placeholder
            
            _logger.LogInformation("Default character creation initiated for user: {UserId}", user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create default character for user: {UserId}", user.Id);
        }
    }

    private UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DisplayName = user.DisplayName,
            AvatarUrl = user.AvatarUrl,
            DateOfBirth = user.DateOfBirth,
            Age = user.Age,
            PrimaryRole = user.PrimaryRole,
            SubscriptionType = user.SubscriptionType,
            IsActive = user.IsActive,
            IsEmailVerified = user.IsEmailVerified,
            IsChildAccount = user.IsChildAccount,
            ParentUserId = user.ParentUserId,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }
}

/// <summary>
/// Validator für RegisterUserCommand
/// </summary>
public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-Mail-Adresse ist erforderlich")
            .EmailAddress().WithMessage("Ungültige E-Mail-Adresse")
            .MaximumLength(100).WithMessage("E-Mail-Adresse zu lang");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Passwort ist erforderlich")
            .MinimumLength(ApplicationConstants.Authentication.MinPasswordLength)
            .WithMessage($"Passwort muss mindestens {ApplicationConstants.Authentication.MinPasswordLength} Zeichen lang sein")
            .Must(BeValidPassword).WithMessage("Passwort entspricht nicht den Sicherheitsanforderungen");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Vorname ist erforderlich")
            .Length(2, 50).WithMessage("Vorname muss zwischen 2 und 50 Zeichen lang sein")
            .Must(BeChildFriendly).WithMessage("Vorname enthält ungeeignete Inhalte");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Nachname ist erforderlich")
            .Length(2, 50).WithMessage("Nachname muss zwischen 2 und 50 Zeichen lang sein")
            .Must(BeChildFriendly).WithMessage("Nachname enthält ungeeignete Inhalte");

        RuleFor(x => x.DateOfBirth)
            .Must(BeValidAge).WithMessage("Ungültiges Geburtsdatum")
            .When(x => x.DateOfBirth.HasValue);

        RuleFor(x => x.AcceptTerms)
            .Equal(true).WithMessage("Geschäftsbedingungen müssen akzeptiert werden");

        RuleFor(x => x.AcceptPrivacyPolicy)
            .Equal(true).WithMessage("Datenschutzerklärung muss akzeptiert werden");
    }

    private bool BeValidPassword(string password)
    {
        if (string.IsNullOrEmpty(password)) return false;
        
        // Mindestens ein Buchstabe und eine Zahl
        var hasLetter = password.Any(char.IsLetter);
        var hasDigit = password.Any(char.IsDigit);
        
        return hasLetter && hasDigit;
    }

    private bool BeChildFriendly(string text)
    {
        return !string.IsNullOrEmpty(text) && text.IsChildFriendly();
    }

    private bool BeValidAge(DateTime? dateOfBirth)
    {
        if (!dateOfBirth.HasValue) return true;
        
        var age = dateOfBirth.Value.CalculateAge();
        return age >= 0 && age <= 120;
    }
}