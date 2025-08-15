namespace Avatales.Shared.Extensions;

/// <summary>
/// DateTime-Erweiterungen für Formatierung und Berechnungen
/// Spezielle Funktionen für kinderfreundliche Zeitangaben
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Konvertiert DateTime zu kinderfreundlichem Text
    /// </summary>
    public static string ToChildFriendlyString(this DateTime dateTime)
    {
        var now = DateTime.UtcNow;
        var timeSpan = now - dateTime;

        return timeSpan.TotalSeconds switch
        {
            < 60 => "gerade eben",
            < 300 => "vor ein paar Minuten", // 5 Minuten
            < 1800 => "vor einer halben Stunde", // 30 Minuten
            < 3600 => "vor einer Stunde",
            < 7200 => "vor ein paar Stunden", // 2 Stunden
            < 86400 => "heute", // 24 Stunden
            < 172800 => "gestern", // 48 Stunden
            < 604800 => "diese Woche", // 7 Tage
            < 1209600 => "letzte Woche", // 14 Tage
            < 2592000 => "diesen Monat", // 30 Tage
            _ => "vor längerer Zeit"
        };
    }

    /// <summary>
    /// Prüft ob das Datum heute ist
    /// </summary>
    public static bool IsToday(this DateTime dateTime)
    {
        return dateTime.Date == DateTime.UtcNow.Date;
    }

    /// <summary>
    /// Prüft ob das Datum gestern war
    /// </summary>
    public static bool IsYesterday(this DateTime dateTime)
    {
        return dateTime.Date == DateTime.UtcNow.Date.AddDays(-1);
    }

    /// <summary>
    /// Prüft ob das Datum in dieser Woche liegt
    /// </summary>
    public static bool IsThisWeek(this DateTime dateTime)
    {
        var now = DateTime.UtcNow;
        var startOfWeek = now.AddDays(-(int)now.DayOfWeek);
        var endOfWeek = startOfWeek.AddDays(7);

        return dateTime >= startOfWeek && dateTime < endOfWeek;
    }

    /// <summary>
    /// Prüft ob das Datum in diesem Monat liegt
    /// </summary>
    public static bool IsThisMonth(this DateTime dateTime)
    {
        var now = DateTime.UtcNow;
        return dateTime.Year == now.Year && dateTime.Month == now.Month;
    }

    /// <summary>
    /// Berechnet das Alter in Jahren
    /// </summary>
    public static int CalculateAge(this DateTime birthDate)
    {
        var today = DateTime.UtcNow;
        var age = today.Year - birthDate.Year;

        if (birthDate.Date > today.AddYears(-age))
            age--;

        return Math.Max(0, age);
    }

    /// <summary>
    /// Gibt den Wochentag auf Deutsch zurück
    /// </summary>
    public static string ToGermanDayOfWeek(this DateTime dateTime)
    {
        return dateTime.DayOfWeek switch
        {
            DayOfWeek.Monday => "Montag",
            DayOfWeek.Tuesday => "Dienstag",
            DayOfWeek.Wednesday => "Mittwoch",
            DayOfWeek.Thursday => "Donnerstag",
            DayOfWeek.Friday => "Freitag",
            DayOfWeek.Saturday => "Samstag",
            DayOfWeek.Sunday => "Sonntag",
            _ => dateTime.DayOfWeek.ToString()
        };
    }

    /// <summary>
    /// Gibt den Monat auf Deutsch zurück
    /// </summary>
    public static string ToGermanMonth(this DateTime dateTime)
    {
        return dateTime.Month switch
        {
            1 => "Januar",
            2 => "Februar",
            3 => "März",
            4 => "April",
            5 => "Mai",
            6 => "Juni",
            7 => "Juli",
            8 => "August",
            9 => "September",
            10 => "Oktober",
            11 => "November",
            12 => "Dezember",
            _ => dateTime.Month.ToString()
        };
    }

    /// <summary>
    /// Formatiert Datum für Kinder (ohne Uhrzeit)
    /// </summary>
    public static string ToChildFriendlyDate(this DateTime dateTime)
    {
        var now = DateTime.UtcNow;

        if (dateTime.IsToday())
            return "Heute";

        if (dateTime.IsYesterday())
            return "Gestern";

        if (dateTime.IsThisWeek())
            return dateTime.ToGermanDayOfWeek();

        if (dateTime.IsThisMonth())
            return $"{dateTime.Day}. {dateTime.ToGermanMonth()}";

        return $"{dateTime.Day}. {dateTime.ToGermanMonth()} {dateTime.Year}";
    }

    /// <summary>
    /// Berechnet Tage seit einem Datum
    /// </summary>
    public static int DaysSince(this DateTime dateTime)
    {
        return (int)(DateTime.UtcNow - dateTime).TotalDays;
    }

    /// <summary>
    /// Berechnet Stunden seit einem Datum
    /// </summary>
    public static int HoursSince(this DateTime dateTime)
    {
        return (int)(DateTime.UtcNow - dateTime).TotalHours;
    }

    /// <summary>
    /// Prüft ob ein Datum in der Zukunft liegt
    /// </summary>
    public static bool IsInFuture(this DateTime dateTime)
    {
        return dateTime > DateTime.UtcNow;
    }

    /// <summary>
    /// Prüft ob ein Datum in den letzten X Tagen liegt
    /// </summary>
    public static bool IsInLastDays(this DateTime dateTime, int days)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);
        return dateTime >= cutoff;
    }

    /// <summary>
    /// Gibt den Anfang des Tages zurück (00:00:00)
    /// </summary>
    public static DateTime StartOfDay(this DateTime dateTime)
    {
        return dateTime.Date;
    }

    /// <summary>
    /// Gibt das Ende des Tages zurück (23:59:59)
    /// </summary>
    public static DateTime EndOfDay(this DateTime dateTime)
    {
        return dateTime.Date.AddDays(1).AddTicks(-1);
    }

    /// <summary>
    /// Gibt den Anfang der Woche zurück (Montag)
    /// </summary>
    public static DateTime StartOfWeek(this DateTime dateTime)
    {
        var diff = (7 + (dateTime.DayOfWeek - DayOfWeek.Monday)) % 7;
        return dateTime.AddDays(-diff).Date;
    }

    /// <summary>
    /// Gibt den Anfang des Monats zurück
    /// </summary>
    public static DateTime StartOfMonth(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, 1);
    }

    /// <summary>
    /// Prüft ob es ein Schultag ist (Montag-Freitag, nicht Wochenende)
    /// </summary>
    public static bool IsSchoolDay(this DateTime dateTime)
    {
        return dateTime.DayOfWeek != DayOfWeek.Saturday &&
               dateTime.DayOfWeek != DayOfWeek.Sunday;
    }

    /// <summary>
    /// Prüft ob es Wochenende ist
    /// </summary>
    public static bool IsWeekend(this DateTime dateTime)
    {
        return dateTime.DayOfWeek == DayOfWeek.Saturday ||
               dateTime.DayOfWeek == DayOfWeek.Sunday;
    }

    /// <summary>
    /// Gibt eine kinderfreundliche Tageszeit zurück
    /// </summary>
    public static string GetTimeOfDayForChildren(this DateTime dateTime)
    {
        return dateTime.Hour switch
        {
            >= 6 and < 12 => "Morgen",
            >= 12 and < 17 => "Nachmittag",
            >= 17 and < 21 => "Abend",
            _ => "Nacht"
        };
    }

    /// <summary>
    /// Prüft ob es Zeit für eine Geschichte ist (kinderfreundliche Zeiten)
    /// </summary>
    public static bool IsStoryTime(this DateTime dateTime)
    {
        // Geschichten sind zwischen 7:00 und 20:00 Uhr erlaubt
        return dateTime.Hour >= 7 && dateTime.Hour < 20;
    }

    /// <summary>
    /// Konvertiert zu Unix Timestamp
    /// </summary>
    public static long ToUnixTimestamp(this DateTime dateTime)
    {
        return ((DateTimeOffset)dateTime).ToUnixTimeSeconds();
    }

    /// <summary>
    /// Konvertiert von Unix Timestamp
    /// </summary>
    public static DateTime FromUnixTimestamp(long unixTimestamp)
    {
        return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).DateTime;
    }
}