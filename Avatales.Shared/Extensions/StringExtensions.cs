using System.Text.RegularExpressions;
using System.Globalization;
using Avatales.Shared.Models;

namespace Avatales.Shared.Extensions;

/// <summary>
/// Extension Methods für String-Validierung und -Manipulation
/// </summary>
public static class StringExtensions
{
    // Definiere unangemessene Wörter (erweitere diese Liste nach Bedarf)
    private static readonly HashSet<string> InappropriateWords = new(StringComparer.OrdinalIgnoreCase)
    {
        // Deutsche unangemessene Wörter (stark vereinfacht für Beispiel)
        "blöd", "dumm", "hass", "töten", "krieg", "waffe", "gewalt",
        "scheiß", "verdammt", "hölle", "teufel", "satan",
        // Englische unangemessene Wörter (vereinfacht)
        "stupid", "hate", "kill", "war", "weapon", "violence",
        "damn", "hell", "devil", "satan",
        // Weitere können hinzugefügt werden...
    };

    private static readonly Regex ProfanityPattern = new(@"\b(" + string.Join("|", InappropriateWords) + @")\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Prüft ob ein String kinderfreundlich ist
    /// </summary>
    public static bool IsChildFriendly(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        // Prüfe auf unangemessene Wörter
        if (ProfanityPattern.IsMatch(input))
            return false;

        // Prüfe auf zu viele Großbuchstaben (Schreien)
        var upperCaseCount = input.Count(char.IsUpper);
        var totalLetters = input.Count(char.IsLetter);
        if (totalLetters > 0 && (double)upperCaseCount / totalLetters > 0.5)
            return false;

        // Prüfe auf übermäßige Sonderzeichen
        var specialCharCount = input.Count(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c));
        if (specialCharCount > input.Length * 0.3)
            return false;

        // Prüfe auf übermäßige Wiederholungen
        if (HasExcessiveRepetition(input))
            return false;

        return true;
    }

    /// <summary>
    /// Bereinigt einen String für kinderfreundliche Verwendung
    /// </summary>
    public static string SanitizeForChildren(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Entferne unangemessene Wörter
        var sanitized = ProfanityPattern.Replace(input, "***");

        // Normalisiere Großschreibung
        sanitized = NormalizeCapitalization(sanitized);

        // Entferne übermäßige Sonderzeichen
        sanitized = Regex.Replace(sanitized, @"[^\w\s\.\!\?\,\-\:\;]", "");

        // Entferne übermäßige Wiederholungen
        sanitized = RemoveExcessiveRepetition(sanitized);

        return sanitized.Trim();
    }

    /// <summary>
    /// Prüft auf übermäßige Zeichenwiederholungen
    /// </summary>
    private static bool HasExcessiveRepetition(string input)
    {
        if (input.Length < 3) return false;

        for (int i = 0; i < input.Length - 2; i++)
        {
            if (input[i] == input[i + 1] && input[i + 1] == input[i + 2])
                return true;
        }
        return false;
    }

    /// <summary>
    /// Entfernt übermäßige Zeichenwiederholungen
    /// </summary>
    private static string RemoveExcessiveRepetition(string input)
    {
        return Regex.Replace(input, @"(.)\1{2,}", "$1$1");
    }

    /// <summary>
    /// Normalisiert die Großschreibung
    /// </summary>
    private static string NormalizeCapitalization(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        // Wenn mehr als 50% Großbuchstaben, wandle zu Title Case um
        var upperCaseCount = input.Count(char.IsUpper);
        var totalLetters = input.Count(char.IsLetter);

        if (totalLetters > 0 && (double)upperCaseCount / totalLetters > 0.5)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input.ToLower());
        }

        return input;
    }

    /// <summary>
    /// Konvertiert String zu einem sicheren Dateinamen
    /// </summary>
    public static string ToSafeFileName(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "untitled";

        var invalidChars = Path.GetInvalidFileNameChars();
        var safeFileName = new string(input.Where(c => !invalidChars.Contains(c)).ToArray());

        // Ersetze Leerzeichen durch Unterstriche
        safeFileName = Regex.Replace(safeFileName, @"\s+", "_");

        // Begrenze Länge
        if (safeFileName.Length > 100)
            safeFileName = safeFileName.Substring(0, 100);

        return string.IsNullOrWhiteSpace(safeFileName) ? "untitled" : safeFileName;
    }

    /// <summary>
    /// Verkürzt Text auf eine bestimmte Länge mit Ellipsis
    /// </summary>
    public static string Truncate(this string input, int maxLength, string ellipsis = "...")
    {
        if (string.IsNullOrWhiteSpace(input) || input.Length <= maxLength)
            return input ?? string.Empty;

        return input.Substring(0, maxLength - ellipsis.Length) + ellipsis;
    }

    /// <summary>
    /// Entfernt HTML-Tags aus einem String
    /// </summary>
    public static string StripHtml(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        return Regex.Replace(input, "<.*?>", string.Empty);
    }
}

/// <summary>
/// Extension Methods für Enum-Operationen
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// Konvertiert Enum zu benutzerfreundlichem String
    /// </summary>
    public static string ToDisplayString(this Enum enumValue)
    {
        return enumValue.ToString().ToDisplayString();
    }

    /// <summary>
    /// Konvertiert CamelCase/PascalCase zu benutzerfreundlichem Format
    /// </summary>
    public static string ToDisplayString(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Füge Leerzeichen vor Großbuchstaben hinzu
        var result = Regex.Replace(input, @"(?<!^)([A-Z])", " $1");

        // Behandle Unterstriche
        result = result.Replace("_", " ");

        return result.Trim();
    }
}

/// <summary>
/// Extension Methods für DateTime-Operationen
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Konvertiert DateTime zu altersgerechtem Format
    /// </summary>
    public static string ToChildFriendlyString(this DateTime dateTime, int childAge)
    {
        var now = DateTime.UtcNow;
        var difference = now - dateTime;

        if (childAge <= 6)
        {
            // Sehr einfache Begriffe für kleine Kinder
            if (difference.TotalDays < 1)
                return "heute";
            else if (difference.TotalDays < 2)
                return "gestern";
            else if (difference.TotalDays < 7)
                return "vor ein paar Tagen";
            else
                return "vor langer Zeit";
        }
        else if (childAge <= 10)
        {
            // Mittlere Komplexität
            if (difference.TotalHours < 1)
                return "vor wenigen Minuten";
            else if (difference.TotalDays < 1)
                return $"vor {(int)difference.TotalHours} Stunden";
            else if (difference.TotalDays < 7)
                return $"vor {(int)difference.TotalDays} Tagen";
            else if (difference.TotalDays < 30)
                return $"vor {(int)(difference.TotalDays / 7)} Wochen";
            else
                return $"vor {(int)(difference.TotalDays / 30)} Monaten";
        }
        else
        {
            // Komplexere Begriffe für ältere Kinder
            return dateTime.ToString("dd.MM.yyyy");
        }
    }

    /// <summary>
    /// Prüft ob ein Datum in einem sicheren Bereich für Kinder liegt
    /// </summary>
    public static bool IsValidForChildren(this DateTime dateTime)
    {
        var now = DateTime.UtcNow;
        var minDate = new DateTime(2000, 1, 1);
        var maxDate = now.AddDays(1);

        return dateTime >= minDate && dateTime <= maxDate;
    }
}

/// <summary>
/// Extension Methods für Collections
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Mischt eine Liste zufällig (Fisher-Yates Shuffle)
    /// </summary>
    public static void Shuffle<T>(this IList<T> list)
    {
        var random = new Random();
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    /// <summary>
    /// Nimmt zufällige Elemente aus einer Collection
    /// </summary>
    public static IEnumerable<T> TakeRandom<T>(this IEnumerable<T> source, int count)
    {
        var list = source.ToList();
        list.Shuffle();
        return list.Take(count);
    }

    /// <summary>
    /// Prüft ob eine Collection null oder leer ist
    /// </summary>
    public static bool IsNullOrEmpty<T>(this ICollection<T>? collection)
    {
        return collection == null || collection.Count == 0;
    }

    /// <summary>
    /// Fügt Element nur hinzu wenn es nicht bereits existiert
    /// </summary>
    public static bool AddIfNotExists<T>(this ICollection<T> collection, T item)
    {
        if (!collection.Contains(item))
        {
            collection.Add(item);
            return true;
        }
        return false;
    }
}