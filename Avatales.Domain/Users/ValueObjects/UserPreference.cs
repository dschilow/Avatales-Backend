namespace Avatales.Domain.Users.ValueObjects;

/// <summary>
/// UserPreference Value Object für Benutzereinstellungen
/// Unveränderliches Objekt für Schlüssel-Wert-Paare der Benutzer-Präferenzen
/// </summary>
public class UserPreference : IEquatable<UserPreference>
{
    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    protected UserPreference() { } // For EF Core

    public UserPreference(string key, string value, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Preference key cannot be empty", nameof(key));

        if (value == null)
            throw new ArgumentNullException(nameof(value));

        Key = key.Trim();
        Value = value;
        Description = description?.Trim();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        ValidateKey();
        ValidateValue();
    }

    public void UpdateValue(string newValue)
    {
        if (newValue == null)
            throw new ArgumentNullException(nameof(newValue));

        Value = newValue;
        UpdatedAt = DateTime.UtcNow;
        ValidateValue();
    }

    public bool IsEqual(string key, string value)
    {
        return Key.Equals(key, StringComparison.OrdinalIgnoreCase) &&
               Value.Equals(value, StringComparison.Ordinal);
    }

    public bool IsSettingEnabled()
    {
        return Value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
               Value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
               Value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
               Value.Equals("on", StringComparison.OrdinalIgnoreCase);
    }

    public int GetIntValue(int defaultValue = 0)
    {
        return int.TryParse(Value, out var result) ? result : defaultValue;
    }

    public bool GetBoolValue(bool defaultValue = false)
    {
        return bool.TryParse(Value, out var result) ? result : defaultValue;
    }

    public DateTime? GetDateTimeValue()
    {
        return DateTime.TryParse(Value, out var result) ? result : null;
    }

    public decimal GetDecimalValue(decimal defaultValue = 0m)
    {
        return decimal.TryParse(Value, out var result) ? result : defaultValue;
    }

    private void ValidateKey()
    {
        // Erlaubte Präferenz-Schlüssel definieren
        var allowedKeys = new[]
        {
            // Allgemeine Einstellungen
            "language", "timezone", "theme", "notifications_enabled",
            
            // Kinder-Sicherheit
            "content_filter_level", "max_story_length", "allowed_genres",
            "learning_mode_enabled", "screen_time_limit",
            
            // Eltern-Kontrollen
            "requires_parent_approval", "share_stories_public", "allow_character_adoption",
            "moderation_level", "inappropriate_content_blocking",
            
            // Lern-Einstellungen
            "preferred_learning_goals", "difficulty_level", "vocabulary_level",
            "reading_comprehension_level", "age_appropriate_content",
            
            // UI/UX Präferenzen
            "font_size", "high_contrast", "reduce_animations", "voice_enabled",
            "mascot_enabled", "tutorial_completed",
            
            // Datenschutz
            "analytics_enabled", "data_sharing_consent", "marketing_emails",
            "usage_statistics", "crash_reporting",
            
            // Abonnement & Features
            "subscription_auto_renew", "premium_features_used", "trial_completed",
            "onboarding_completed", "feature_flags"
        };

        if (!allowedKeys.Contains(Key.ToLower()))
        {
            throw new ArgumentException($"Invalid preference key: {Key}. Allowed keys: {string.Join(", ", allowedKeys)}");
        }
    }

    private void ValidateValue()
    {
        // Maximale Länge für Werte
        if (Value.Length > 1000)
        {
            throw new ArgumentException("Preference value cannot exceed 1000 characters");
        }

        // Spezielle Validierung basierend auf dem Schlüssel
        switch (Key.ToLower())
        {
            case "language":
                ValidateLanguageCode();
                break;
            case "timezone":
                ValidateTimeZone();
                break;
            case "content_filter_level":
                ValidateIntegerRange(1, 5);
                break;
            case "max_story_length":
                ValidateIntegerRange(100, 10000);
                break;
            case "screen_time_limit":
                ValidateIntegerRange(0, 480); // 0-8 hours in minutes
                break;
            case "font_size":
                ValidateIntegerRange(12, 24);
                break;
            case "difficulty_level":
            case "vocabulary_level":
            case "reading_comprehension_level":
                ValidateIntegerRange(1, 10);
                break;
        }
    }

    private void ValidateLanguageCode()
    {
        var supportedLanguages = new[] { "de", "en", "fr", "es", "it" };
        if (!supportedLanguages.Contains(Value.ToLower()))
        {
            throw new ArgumentException($"Unsupported language code: {Value}. Supported: {string.Join(", ", supportedLanguages)}");
        }
    }

    private void ValidateTimeZone()
    {
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(Value);
        }
        catch (TimeZoneNotFoundException)
        {
            throw new ArgumentException($"Invalid timezone: {Value}");
        }
    }

    private void ValidateIntegerRange(int min, int max)
    {
        if (!int.TryParse(Value, out var intValue) || intValue < min || intValue > max)
        {
            throw new ArgumentException($"Value must be an integer between {min} and {max}. Got: {Value}");
        }
    }

    // Equality implementation
    public bool Equals(UserPreference? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Key == other.Key && Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as UserPreference);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Key, Value);
    }

    public override string ToString()
    {
        return $"{Key}: {Value}";
    }

    public static bool operator ==(UserPreference? left, UserPreference? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(UserPreference? left, UserPreference? right)
    {
        return !Equals(left, right);
    }

    // Factory Methods für häufige Einstellungen
    public static UserPreference CreateLanguagePreference(string languageCode)
    {
        return new UserPreference("language", languageCode, "User's preferred language");
    }

    public static UserPreference CreateNotificationPreference(bool enabled)
    {
        return new UserPreference("notifications_enabled", enabled.ToString().ToLower(),
            "Enable/disable push notifications");
    }

    public static UserPreference CreateContentFilterLevel(int level)
    {
        if (level < 1 || level > 5)
            throw new ArgumentException("Content filter level must be between 1 and 5");

        var description = level switch
        {
            1 => "Minimal filtering - Age 12+",
            2 => "Light filtering - Age 8+",
            3 => "Moderate filtering - Age 6+",
            4 => "Strong filtering - Age 4+",
            5 => "Maximum filtering - Age 3+",
            _ => "Content filtering level"
        };

        return new UserPreference("content_filter_level", level.ToString(), description);
    }

    public static UserPreference CreateLearningModePreference(bool enabled)
    {
        return new UserPreference("learning_mode_enabled", enabled.ToString().ToLower(),
            "Enable educational learning mode in stories");
    }

    public static UserPreference CreateThemePreference(string theme)
    {
        var allowedThemes = new[] { "light", "dark", "auto", "high_contrast" };
        if (!allowedThemes.Contains(theme.ToLower()))
            throw new ArgumentException($"Invalid theme. Allowed: {string.Join(", ", allowedThemes)}");

        return new UserPreference("theme", theme.ToLower(), "UI theme preference");
    }
}