using Avatales.Shared.Models;

namespace Avatales.Domain.Characters.ValueObjects;

/// <summary>
/// CharacterBaseTrait Value Object - Basis-Eigenschaft eines Charakters in der DNA
/// Unveränderlicher Grundwert, der bei Adoption übertragen wird
/// </summary>
public class CharacterBaseTrait : IEquatable<CharacterBaseTrait>
{
    public CharacterTraitType TraitType { get; private set; }
    public int BaseValue { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    protected CharacterBaseTrait() { } // For EF Core

    public CharacterBaseTrait(CharacterTraitType traitType, int baseValue, string? description = null)
    {
        if (baseValue < 1 || baseValue > 10)
            throw new ArgumentException($"Base trait value must be between 1 and 10. Got: {baseValue}", nameof(baseValue));

        TraitType = traitType;
        BaseValue = baseValue;
        Description = description ?? GetDefaultDescription(traitType);
        CreatedAt = DateTime.UtcNow;
    }

    public CharacterBaseTrait CreateCopy()
    {
        return new CharacterBaseTrait(TraitType, BaseValue, Description);
    }

    public string GetTraitName()
    {
        return TraitType switch
        {
            CharacterTraitType.Courage => "Mut",
            CharacterTraitType.Curiosity => "Neugier",
            CharacterTraitType.Kindness => "Freundlichkeit",
            CharacterTraitType.Creativity => "Kreativität",
            CharacterTraitType.Intelligence => "Intelligenz",
            CharacterTraitType.Humor => "Humor",
            CharacterTraitType.Wisdom => "Weisheit",
            CharacterTraitType.Empathy => "Empathie",
            CharacterTraitType.Determination => "Entschlossenheit",
            CharacterTraitType.Optimism => "Optimismus",
            _ => TraitType.ToString()
        };
    }

    public string GetIntensityDescription()
    {
        return BaseValue switch
        {
            10 => "außergewöhnlich",
            9 => "sehr stark",
            8 => "stark",
            7 => "überdurchschnittlich",
            6 => "gut entwickelt",
            5 => "durchschnittlich",
            4 => "weniger ausgeprägt",
            3 => "schwach entwickelt",
            2 => "kaum vorhanden",
            1 => "minimal",
            _ => "unbekannt"
        };
    }

    public string GetFullDescription()
    {
        return $"{GetTraitName()}: {GetIntensityDescription()} ({BaseValue}/10)";
    }

    public bool IsStrongTrait()
    {
        return BaseValue >= 7;
    }

    public bool IsWeakTrait()
    {
        return BaseValue <= 3;
    }

    public bool IsDominantTrait()
    {
        return BaseValue >= 8;
    }

    public List<string> GetAssociatedKeywords()
    {
        return TraitType switch
        {
            CharacterTraitType.Courage => new List<string> { "mutig", "tapfer", "furchtlos", "entschlossen", "wagemutig" },
            CharacterTraitType.Curiosity => new List<string> { "neugierig", "wissbegierig", "forschend", "interessiert", "aufmerksam" },
            CharacterTraitType.Kindness => new List<string> { "freundlich", "hilfsbereit", "mitfühlend", "sanft", "großzügig" },
            CharacterTraitType.Creativity => new List<string> { "kreativ", "einfallsreich", "innovativ", "künstlerisch", "originell" },
            CharacterTraitType.Intelligence => new List<string> { "intelligent", "klug", "schlau", "weise", "scharfsinnig" },
            CharacterTraitType.Humor => new List<string> { "lustig", "witzig", "humorvoll", "fröhlich", "spielerisch" },
            CharacterTraitType.Wisdom => new List<string> { "weise", "erfahren", "bedacht", "vernünftig", "reif" },
            CharacterTraitType.Empathy => new List<string> { "einfühlsam", "verständnisvoll", "mitfühlend", "aufmerksam", "sensibel" },
            CharacterTraitType.Determination => new List<string> { "entschlossen", "ausdauernd", "willensstark", "beharrlich", "zielstrebig" },
            CharacterTraitType.Optimism => new List<string> { "optimistisch", "hoffnungsvoll", "positiv", "zuversichtlich", "fröhlich" },
            _ => new List<string> { TraitType.ToString().ToLower() }
        };
    }

    public List<string> GetStoryPromptSuggestions()
    {
        return TraitType switch
        {
            CharacterTraitType.Courage => new List<string>
            {
                "Ein Abenteuer, bei dem Mut gefragt ist",
                "Eine Situation, in der jemand beschützt werden muss",
                "Eine gefährliche Reise zu einem wichtigen Ziel",
                "Ein Moment, in dem Angst überwunden werden muss"
            },
            CharacterTraitType.Curiosity => new List<string>
            {
                "Ein Rätsel, das gelöst werden will",
                "Eine Entdeckungsreise in unbekanntes Terrain",
                "Ein Geheimnis, das erforscht werden muss",
                "Ein neues Phänomen, das untersucht wird"
            },
            CharacterTraitType.Kindness => new List<string>
            {
                "Eine Gelegenheit, jemandem zu helfen",
                "Ein Konflikt, der mit Güte gelöst wird",
                "Eine Freundschaft, die entstehen kann",
                "Ein Moment der Vergebung und des Verstehens"
            },
            CharacterTraitType.Creativity => new List<string>
            {
                "Ein Problem, das kreativ gelöst werden muss",
                "Eine Kunstwerk, das geschaffen wird",
                "Eine Erfindung, die die Welt verbessert",
                "Eine innovative Lösung für alte Probleme"
            },
            CharacterTraitType.Intelligence => new List<string>
            {
                "Ein komplexes Problem, das Köpfchen braucht",
                "Eine Lernreise zu neuem Wissen",
                "Eine Strategie, die entwickelt werden muss",
                "Ein Puzzle, das Logik erfordert"
            },
            CharacterTraitType.Humor => new List<string>
            {
                "Eine lustige Verwechslung oder Situation",
                "Ein Tag voller Schabernack und Spaß",
                "Eine ernste Situation, die mit Humor aufgelockert wird",
                "Ein Lachen, das andere ansteckt"
            },
            CharacterTraitType.Wisdom => new List<string>
            {
                "Eine wichtige Entscheidung, die getroffen werden muss",
                "Ein Rat, der einem Freund gegeben wird",
                "Eine Lebenserfahrung, die geteilt wird",
                "Eine Weisheit, die durch Erfahrung gewonnen wird"
            },
            CharacterTraitType.Empathy => new List<string>
            {
                "Ein Freund, der Trost braucht",
                "Eine Situation, in der Verständnis gezeigt wird",
                "Ein Konflikt, der durch Einfühlungsvermögen gelöst wird",
                "Eine emotionale Verbindung, die entsteht"
            },
            CharacterTraitType.Determination => new List<string>
            {
                "Ein Ziel, das hartnäckig verfolgt wird",
                "Eine Herausforderung, die Ausdauer erfordert",
                "Ein Traum, der nie aufgegeben wird",
                "Eine Aufgabe, die trotz Hindernissen vollendet wird"
            },
            CharacterTraitType.Optimism => new List<string>
            {
                "Eine hoffnungslose Situation, die Licht bekommt",
                "Ein Tag, an dem alles schief läuft, aber gut endet",
                "Eine positive Wendung in schwierigen Zeiten",
                "Ein Lächeln, das andere zum Lächeln bringt"
            },
            _ => new List<string> { "Ein Abenteuer passend zu dieser Eigenschaft" }
        };
    }

    private static string GetDefaultDescription(CharacterTraitType traitType)
    {
        return traitType switch
        {
            CharacterTraitType.Courage => "Die Fähigkeit, tapfer zu sein und sich Herausforderungen zu stellen",
            CharacterTraitType.Curiosity => "Der Drang, Neues zu entdecken und zu lernen",
            CharacterTraitType.Kindness => "Die natürliche Neigung, anderen zu helfen und freundlich zu sein",
            CharacterTraitType.Creativity => "Die Gabe, originelle Ideen zu entwickeln und Probleme kreativ zu lösen",
            CharacterTraitType.Intelligence => "Die Fähigkeit, schnell zu lernen und komplexe Probleme zu verstehen",
            CharacterTraitType.Humor => "Die Gabe, andere zum Lachen zu bringen und das Leben mit Leichtigkeit zu nehmen",
            CharacterTraitType.Wisdom => "Die Fähigkeit, weise Entscheidungen zu treffen und guten Rat zu geben",
            CharacterTraitType.Empathy => "Die Fähigkeit, die Gefühle anderer zu verstehen und mitzufühlen",
            CharacterTraitType.Determination => "Die Willenskraft, Ziele zu verfolgen und nicht aufzugeben",
            CharacterTraitType.Optimism => "Die Einstellung, immer das Beste zu erwarten und positiv zu bleiben",
            _ => "Eine besondere Charaktereigenschaft"
        };
    }

    public bool Equals(CharacterBaseTrait? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return TraitType == other.TraitType && BaseValue == other.BaseValue;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as CharacterBaseTrait);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(TraitType, BaseValue);
    }

    public override string ToString()
    {
        return $"{GetTraitName()}: {BaseValue}/10";
    }

    public static bool operator ==(CharacterBaseTrait? left, CharacterBaseTrait? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(CharacterBaseTrait? left, CharacterBaseTrait? right)
    {
        return !Equals(left, right);
    }

    // Factory Methods für häufige Trait-Kombinationen
    public static List<CharacterBaseTrait> CreateBalancedTraits()
    {
        return new List<CharacterBaseTrait>
        {
            new(CharacterTraitType.Courage, 5),
            new(CharacterTraitType.Kindness, 5),
            new(CharacterTraitType.Curiosity, 5),
            new(CharacterTraitType.Creativity, 5),
            new(CharacterTraitType.Intelligence, 5)
        };
    }

    public static List<CharacterBaseTrait> CreateHeroicTraits()
    {
        return new List<CharacterBaseTrait>
        {
            new(CharacterTraitType.Courage, 8),
            new(CharacterTraitType.Kindness, 7),
            new(CharacterTraitType.Determination, 8),
            new(CharacterTraitType.Optimism, 6),
            new(CharacterTraitType.Wisdom, 5)
        };
    }

    public static List<CharacterBaseTrait> CreateScholarTraits()
    {
        return new List<CharacterBaseTrait>
        {
            new(CharacterTraitType.Intelligence, 9),
            new(CharacterTraitType.Curiosity, 8),
            new(CharacterTraitType.Wisdom, 7),
            new(CharacterTraitType.Determination, 6),
            new(CharacterTraitType.Kindness, 5)
        };
    }

    public static List<CharacterBaseTrait> CreateArtistTraits()
    {
        return new List<CharacterBaseTrait>
        {
            new(CharacterTraitType.Creativity, 9),
            new(CharacterTraitType.Empathy, 7),
            new(CharacterTraitType.Curiosity, 6),
            new(CharacterTraitType.Optimism, 7),
            new(CharacterTraitType.Intelligence, 5)
        };
    }
}