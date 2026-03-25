using System.Globalization;
using System.Text.RegularExpressions;
using WhatsAppParser.Application.DTOs;
using WhatsAppParser.Application.Interfaces;
using WhatsAppParser.Domain.Enums;

namespace WhatsAppParser.Application.Services;

public class WhatsappMessageParser : IMessageParser
{
    private static readonly Regex PriceWithSymbol =
        new(@"(?:R\$|R\s*\$|RS\$?|\$)\s*([\d.,]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Bare number 100–99999, NOT followed by GB/TB/MHz/%/MB or slash (storage ranges like 128/256GB)
    private static readonly Regex PriceFallback =
        new(@"(?:^|\s)((?:[1-9]\d{2,4})(?:[.,]\d{2})?)(?!\s*(?:/|\||GB|TB|MHZ|%|MB))(?:\s|$)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex YearPattern =
        new(@"^(?:19|20)\d{2}$", RegexOptions.Compiled);

    // Common storage sizes — bare numbers matching these on a GB-line are NOT prices
    private static readonly HashSet<decimal> StorageSizes = [32, 64, 128, 256, 512, 1024, 2048];

    private static readonly Regex StorageRegex =
        new(@"\b(\d{2,4})\s*(?:GB|TB)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex RamIndicator =
        new(@"\b(?:8|12|16|32|64)\s*GB\s*(?:RAM|DE RAM)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Country flag emoji: two Regional Indicator Symbol Letters (surrogate pairs in .NET)
    // Each RISL = \uD83C + \uDDE6-\uDDFF
    private static readonly Regex FlagEmoji =
        new(@"\uD83C[\uDDE6-\uDDFF]\uD83C[\uDDE6-\uDDFF]", RegexOptions.Compiled);

    public IEnumerable<ParseResultDto> ParseMessage(string rawText)
    {
        var results = new List<ParseResultDto>();
        if (string.IsNullOrWhiteSpace(rawText)) return results;

        var lines = rawText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        ParseResultDto? current = null;
        string? pendingFlag = null;
        // Context condition: set by a standalone line ("Todos seminovos", "CPO", etc.)
        // and applied to every subsequent product that has no inline condition
        var contextCondition = Condition.Unknown;

        foreach (var line in lines)
        {
            var upper = line.ToUpperInvariant();
            var brand = DetectBrand(upper);

            // Always check for condition keywords — update context if found on any line
            var lineCondition = ExtractCondition(upper);
            if (lineCondition != Condition.Unknown)
                contextCondition = lineCondition;

            // Capture country flag from any line — associate with next product
            var flag = ExtractFlag(line);
            if (flag is not null) pendingFlag = flag;

            if (brand != Brand.Unknown)
            {
                if (current is { IsValid: true }) results.Add(current);

                current = new ParseResultDto { Brand = brand };
                current.StorageCapacity = ExtractStorage(upper);
                current.Model = ExtractModel(line, upper, brand);
                // Inline condition takes priority; fall back to context condition
                current.Condition = lineCondition != Condition.Unknown ? lineCondition : contextCondition;
                current.Color = ExtractColor(upper);
                current.OriginFlag = pendingFlag ?? flag;
                pendingFlag = null;
            }

            if (current is null) continue;

            if (current.Price == 0)
            {
                var price = TryExtractPrice(line, upper);
                if (price is > 50 and < 1_000_000)
                    current.Price = price.Value;
            }

            if (current.StorageCapacity is null)
            {
                var storage = ExtractStorage(upper);
                if (storage is not null) current.StorageCapacity = storage;
            }

            if (current.Color is null) current.Color = ExtractColor(upper);

            // Update condition from a later line if still Unknown
            if (current.Condition == Condition.Unknown && lineCondition != Condition.Unknown)
                current.Condition = lineCondition;

            if (current.OriginFlag is null)
            {
                var lineflag = ExtractFlag(line);
                if (lineflag is not null) current.OriginFlag = lineflag;
            }
        }

        if (current is { IsValid: true }) results.Add(current);
        return results;
    }

    // ── Brand detection ────────────────────────────────────────────────────

    private static Brand DetectBrand(string upper)
    {
        // Apple devices
        if (upper.Contains("IPHONE") || upper.Contains("MACBOOK") || upper.Contains("IPAD") ||
            upper.Contains("AIRPODS") || upper.Contains("APPLE WATCH") || upper.Contains("IMAC") ||
            upper.Contains("MAC MINI") || upper.Contains("MAC STUDIO") ||
            upper.Contains("MAC AIR") || upper.Contains("MAC PRO") ||
            // Apple accessories
            upper.Contains("APPLE PENCIL") || upper.Contains("EARPODS") ||
            upper.Contains("MAGIC MOUSE") || upper.Contains("MAGIC KEYBOARD") ||
            upper.Contains("SMART KEYBOARD") || upper.Contains("MAGIC TRACKPAD") ||
            // Apple cables/chargers (brand-explicit)
            (upper.Contains("CABO") && (upper.Contains("LIGHTNING") || upper.Contains("MAGSAFE"))) ||
            (upper.Contains("CARREGADOR") && upper.Contains("APPLE")) ||
            (upper.Contains("FONTE") && upper.Contains("APPLE")))
            return Brand.Apple;

        if (upper.Contains("XIAOMI") || upper.Contains("POCO") || upper.Contains("REDMI"))
            return Brand.Xiaomi;

        if (upper.Contains("SAMSUNG") || upper.Contains("GALAXY"))
            return Brand.Samsung;

        if (upper.Contains("MOTOROLA") || Regex.IsMatch(upper, @"\bMOTO\s+[A-Z]"))
            return Brand.Motorola;

        if (upper.Contains("META QUEST") || upper.Contains("QUEST 2") ||
            upper.Contains("QUEST 3") || upper.Contains("QUEST PRO") ||
            upper.Contains("OCULUS"))
            return Brand.Meta;

        return Brand.Unknown;
    }

    // ── Model extraction ───────────────────────────────────────────────────

    private static string ExtractModel(string original, string upper, Brand brand)
    {
        if (brand == Brand.Apple) return ExtractAppleModel(original, upper);
        if (brand == Brand.Samsung) return ExtractSamsungModel(upper);
        if (brand == Brand.Motorola) return ExtractMotorolaModel(upper);
        if (brand == Brand.Meta) return ExtractMetaQuestModel(upper);
        return ExtractXiaomiModel(upper);
    }

    private static string ExtractChip(string upper)
    {
        var m = Regex.Match(upper, @"\b(M[1-5])(?:\s*(PRO|MAX|ULTRA))?\b", RegexOptions.IgnoreCase);
        if (!m.Success) return string.Empty;
        var chip = m.Groups[1].Value.ToUpper();
        var variant = m.Groups[2].Success
            ? " " + char.ToUpper(m.Groups[2].Value[0]) + m.Groups[2].Value[1..].ToLower()
            : "";
        return chip + variant;
    }

    private static string Capitalize(string s) =>
        s.Length == 0 ? s : char.ToUpper(s[0]) + s[1..].ToLower();

    private static string ExtractAppleModel(string original, string upper)
    {
        // ── MacBook (including "Mac Air" / "Mac Pro" aliases) ──────────────
        if (upper.Contains("MACBOOK") || upper.Contains("MAC AIR") || upper.Contains("MAC PRO"))
        {
            string type;
            if (upper.Contains("AIR") || upper.Contains("MAC AIR"))
                type = " Air";
            else if (upper.Contains("PRO"))
                type = " Pro";
            else
                type = "";

            var chip = ExtractChip(upper);
            var chipStr = chip.Length > 0 ? " " + chip : "";
            var sizeMatch = Regex.Match(upper, @"\b(13|14|15|16)\s*(?:INCH|POLEGADAS?)?\b");
            var sizeStr = sizeMatch.Success ? " " + sizeMatch.Groups[1].Value + "\"" : "";
            return CleanString($"MacBook{type}{chipStr}{sizeStr}").Trim();
        }

        // ── iPad ────────────────────────────────────────────────────────────
        if (upper.Contains("IPAD"))
        {
            var typeMatch = Regex.Match(upper, @"IPAD\s*(PRO|AIR|MINI)\b", RegexOptions.IgnoreCase);
            var type = typeMatch.Success ? " " + Capitalize(typeMatch.Groups[1].Value) : "";

            // Generation number or screen size: iPad 10, iPad Pro 12.9, iPad Air 5
            var genMatch = Regex.Match(upper, @"IPAD\s*(?:PRO|AIR|MINI)?\s*(\d+(?:\.\d+)?)\b", RegexOptions.IgnoreCase);
            var gen = "";
            if (genMatch.Success)
            {
                var num = genMatch.Groups[1].Value;
                // Distinguish screen sizes (>= 7.0) from generations (<= 11)
                gen = " " + num + (num.Contains('.') ? "\"" : "");
            }

            var chip = ExtractChip(upper);
            var chipStr = chip.Length > 0 ? " " + chip : "";
            return CleanString($"iPad{type}{gen}{chipStr}").Trim();
        }

        // ── Apple Pencil ────────────────────────────────────────────────────
        if (upper.Contains("APPLE PENCIL"))
        {
            if (upper.Contains("PRO")) return "Apple Pencil Pro";
            if (upper.Contains("USB")) return "Apple Pencil USB-C";
            if (upper.Contains("2") || upper.Contains("SEGUNDA") || upper.Contains("2A")) return "Apple Pencil 2";
            if (upper.Contains("1") || upper.Contains("PRIMEIRA") || upper.Contains("1A")) return "Apple Pencil 1";
            return "Apple Pencil";
        }

        // ── EarPods ─────────────────────────────────────────────────────────
        if (upper.Contains("EARPODS"))
        {
            if (upper.Contains("USB")) return "EarPods USB-C";
            if (upper.Contains("LIGHTNING")) return "EarPods Lightning";
            if (upper.Contains("35") || upper.Contains("3.5") || upper.Contains("P2")) return "EarPods 3.5mm";
            return "EarPods";
        }

        // ── Magic peripherals ───────────────────────────────────────────────
        if (upper.Contains("MAGIC MOUSE"))
        {
            return upper.Contains("3") ? "Magic Mouse 3" : "Magic Mouse";
        }
        if (upper.Contains("MAGIC KEYBOARD"))
        {
            var variant = upper.Contains("TOUCH ID") ? " Touch ID" : upper.Contains("NUMERIC") ? " Numeric" : "";
            return CleanString($"Magic Keyboard{variant}").Trim();
        }
        if (upper.Contains("SMART KEYBOARD"))
        {
            return "Smart Keyboard";
        }
        if (upper.Contains("MAGIC TRACKPAD"))
        {
            return "Magic Trackpad";
        }

        // ── Cabo/Carregador Apple ────────────────────────────────────────────
        if (upper.Contains("CABO") || upper.Contains("CARREGADOR") || upper.Contains("FONTE"))
        {
            if (upper.Contains("MAGSAFE")) return "Cabo MagSafe";
            if (upper.Contains("LIGHTNING") && upper.Contains("USB-C")) return "Cabo USB-C para Lightning";
            if (upper.Contains("LIGHTNING")) return "Cabo Lightning";
            if (upper.Contains("USB-C")) return "Cabo USB-C";
            if (upper.Contains("CARREGADOR") || upper.Contains("FONTE")) return "Carregador Apple";
            return "Acessório Apple";
        }

        // ── AirPods ─────────────────────────────────────────────────────────
        if (upper.Contains("AIRPODS"))
        {
            if (upper.Contains("MAX")) return "AirPods Max";
            if (upper.Contains("PRO"))
            {
                var genMatch = Regex.Match(upper, @"AIRPODS\s+PRO\s*(\d+|USB[- ]?C)?\b", RegexOptions.IgnoreCase);
                var gen = genMatch.Groups[1].Success ? " " + genMatch.Groups[1].Value : "";
                return CleanString($"AirPods Pro{gen}").Trim();
            }
            var apGen = Regex.Match(upper, @"AIRPODS\s*(\d)\b");
            return apGen.Success ? $"AirPods {apGen.Groups[1].Value}" : "AirPods";
        }

        // ── Apple Watch ─────────────────────────────────────────────────────
        if (upper.Contains("APPLE WATCH"))
        {
            var m = Regex.Match(upper, @"APPLE WATCH\s*(ULTRA\s*\d*|SE\s*\d*|SERIES\s*\d+|\d+)\b", RegexOptions.IgnoreCase);
            var variant = m.Groups[1].Success ? " " + m.Groups[1].Value.Trim() : "";
            return CleanString($"Apple Watch{variant}");
        }

        // ── iMac ─────────────────────────────────────────────────────────────
        if (upper.Contains("IMAC"))
        {
            var chip = ExtractChip(upper);
            return chip.Length > 0 ? $"iMac {chip}" : "iMac";
        }

        // ── Mac Mini / Studio ────────────────────────────────────────────────
        if (upper.Contains("MAC MINI"))
        {
            var chip = ExtractChip(upper);
            return chip.Length > 0 ? $"Mac Mini {chip}" : "Mac Mini";
        }
        if (upper.Contains("MAC STUDIO"))
        {
            var chip = ExtractChip(upper);
            return chip.Length > 0 ? $"Mac Studio {chip}" : "Mac Studio";
        }

        // ── iPhone ───────────────────────────────────────────────────────────
        var iPhoneMatch = Regex.Match(upper,
            @"IPHONE\s+(\d+(?:\s*(?:PRO\s*MAX|PRO|PLUS|MINI))?)",
            RegexOptions.IgnoreCase);

        if (iPhoneMatch.Success)
        {
            var raw = iPhoneMatch.Groups[1].Value.Trim();
            raw = Regex.Replace(raw, @"\d+\s*(GB|TB).*", "", RegexOptions.IgnoreCase).Trim();
            return CleanString("iPhone " + raw);
        }

        return "iPhone";
    }

    private static string ExtractSamsungModel(string upper)
    {
        var m = Regex.Match(upper,
            @"(?:SAMSUNG\s+)?GALAXY\s+([A-Z0-9]+(?:\s+(?:ULTRA|PLUS|FE|5G|4G))*)",
            RegexOptions.IgnoreCase);
        if (m.Success)
        {
            var raw = m.Groups[1].Value.Trim();
            raw = Regex.Replace(raw, @"\d+\s*(GB|TB).*", "", RegexOptions.IgnoreCase).Trim();
            return CleanString("Samsung Galaxy " + raw);
        }

        var fallback = Regex.Match(upper, @"SAMSUNG\s+([A-Z0-9]+(?:\s+[A-Z0-9]+)?)", RegexOptions.IgnoreCase);
        return fallback.Success ? CleanString("Samsung " + fallback.Groups[1].Value.Trim()) : "Samsung";
    }

    private static string ExtractMotorolaModel(string upper)
    {
        var m = Regex.Match(upper,
            @"(?:MOTOROLA\s+)?MOTO\s+([A-Z0-9]+(?:\s+(?:PLUS|G|S|5G))*)",
            RegexOptions.IgnoreCase);
        if (m.Success)
        {
            var raw = m.Groups[1].Value.Trim();
            raw = Regex.Replace(raw, @"\d+\s*(GB|TB).*", "", RegexOptions.IgnoreCase).Trim();
            return CleanString("Motorola Moto " + raw);
        }
        return "Motorola";
    }

    private static string ExtractXiaomiModel(string upper)
    {
        var m = Regex.Match(upper,
            @"(?:XIAOMI|POCO|REDMI)\s+([A-Z0-9]+(?:\s+[A-Z0-9]+){0,3})",
            RegexOptions.IgnoreCase);
        if (!m.Success) return "Xiaomi";

        var brand = upper.Contains("POCO") ? "Poco" : upper.Contains("REDMI") ? "Redmi" : "Xiaomi";
        var raw = m.Groups[1].Value.Trim();
        raw = Regex.Replace(raw, @"\d+\s*(GB|TB).*", "", RegexOptions.IgnoreCase).Trim();
        return CleanString(brand + " " + raw);
    }

    private static string ExtractMetaQuestModel(string upper)
    {
        if (upper.Contains("QUEST PRO")) return "Meta Quest Pro";
        if (upper.Contains("QUEST 3S")) return "Meta Quest 3S";
        if (upper.Contains("QUEST 3")) return "Meta Quest 3";
        if (upper.Contains("QUEST 2")) return "Meta Quest 2";
        return "Meta Quest";
    }

    // ── Storage ─────────────────────────────────────────────────────────────

    private static string? ExtractStorage(string upper)
    {
        var cleaned = RamIndicator.Replace(upper, " ");
        var m = StorageRegex.Match(cleaned);
        if (!m.Success) return null;
        var unit = cleaned.Substring(m.Index + m.Length - 2, 2).ToUpper().TrimStart();
        // StorageRegex only matches GB/TB so unit detection via the regex value
        var rawUnit = Regex.Match(m.Value, @"(?:GB|TB)", RegexOptions.IgnoreCase).Value.ToUpper();
        return m.Groups[1].Value + rawUnit;
    }

    // ── Price ────────────────────────────────────────────────────────────────

    private static decimal? TryExtractPrice(string line, string upper)
    {
        var m = PriceWithSymbol.Match(line);
        if (m.Success)
        {
            var price = ParseBrazilianDecimal(m.Groups[1].Value);
            if (price is > 50) return price;
        }

        m = PriceFallback.Match(line);
        if (m.Success)
        {
            var raw = m.Groups[1].Value;

            // Skip years
            if (YearPattern.IsMatch(raw)) return null;

            // Skip common storage sizes when line mentions GB/TB
            if (decimal.TryParse(raw.Replace(",", "").Replace(".", ""), NumberStyles.Any,
                    CultureInfo.InvariantCulture, out var candidate) &&
                StorageSizes.Contains(candidate) &&
                (upper.Contains("GB") || upper.Contains("TB")))
                return null;

            return ParseBrazilianDecimal(raw);
        }

        return null;
    }

    private static decimal? ParseBrazilianDecimal(string raw)
    {
        raw = raw.Trim();
        if (string.IsNullOrEmpty(raw)) return null;

        bool hasComma = raw.Contains(',');
        bool hasDot = raw.Contains('.');

        string normalized;

        if (hasComma && hasDot)
        {
            normalized = raw.LastIndexOf(',') > raw.LastIndexOf('.')
                ? raw.Replace(".", "").Replace(",", ".")   // BR: 1.234,56
                : raw.Replace(",", "");                     // US: 1,234.56
        }
        else if (hasComma)
        {
            var parts = raw.Split(',');
            normalized = parts.Length == 2 && parts[1].Length <= 2
                ? raw.Replace(",", ".")    // decimal: 840,00
                : raw.Replace(",", "");    // thousands: 1,200
        }
        else if (hasDot)
        {
            var parts = raw.Split('.');
            normalized = parts.Length == 2 && parts[1].Length == 3
                ? raw.Replace(".", "")     // thousands: 69.000
                : raw;                     // decimal: 840.50
        }
        else
        {
            normalized = raw;
        }

        return decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Condition ExtractCondition(string upper)
    {
        // CPO / recondicionado
        if (upper.Contains("CPO") || upper.Contains("CERTIFIED PRE") ||
            upper.Contains("RECONDICIONADO") || upper.Contains("RECONDICIONA"))
            return Condition.CPO;

        // Novo / lacrado
        if (upper.Contains("LACRADO") || upper.Contains("LACRADA") ||
            upper.Contains("CAIXA LACRADA") || upper.Contains("NA CAIXA") ||
            upper.Contains("NOVO ") || upper.Contains("NOVOS") || upper.Contains("NOVA ") ||
            upper.Contains("NEW") ||
            (upper.Contains("NOVO") && !upper.Contains("SEMI") && !upper.Contains("SEMI-")))
            return Condition.New;

        // Seminovo / usado
        if (upper.Contains("SEMINOVO") || upper.Contains("SEMI NOVO") || upper.Contains("SEMI-NOVO") ||
            upper.Contains("USADO") || upper.Contains("USADOS") || upper.Contains("USADA") ||
            upper.Contains("SEGUNDA MAO") || upper.Contains("SEGUNDA MÃO") ||
            upper.Contains("2° MAO") || upper.Contains("2ª MAO"))
            return Condition.Used;

        // Vitrine / swap / refurb
        if (upper.Contains("VITRINE") || upper.Contains("SWAP") ||
            upper.Contains("REFURB") || upper.Contains("OUTLET"))
            return Condition.Refurbished;

        // Bateria 100%
        if (upper.Contains("100%") || upper.Contains("BATERIA 100") ||
            upper.Contains("BAT 100") || upper.Contains("BAT. 100"))
            return Condition.Battery100;

        return Condition.Unknown;
    }

    private static string? ExtractColor(string upper)
    {
        string[] colors = [
            // Portuguese
            "PRETO", "BRANCO", "AZUL", "VERDE", "ROSA", "AMARELO", "ROXO",
            "PRATA", "DOURADO", "OURO", "GRAFITE", "TITANIO", "TITANIUM",
            "CIANO", "CORAL", "LARANJA", "VERMELHO", "BEGE", "CINZA",
            // English
            "BLACK", "WHITE", "BLUE", "GREEN", "PINK", "YELLOW", "PURPLE",
            "SILVER", "GOLD", "GRAPHITE", "STARLIGHT", "MIDNIGHT", "NATURAL",
            "DESERT", "TEAL", "ULTRAMARINE", "SAFFRON"
        ];
        foreach (var c in colors)
            if (upper.Contains(c)) return c;
        return null;
    }

    private static string? ExtractFlag(string text)
    {
        var m = FlagEmoji.Match(text);
        return m.Success ? m.Value : null;
    }

    private static string CleanString(string input) =>
        Regex.Replace(Regex.Replace(input, @"\p{Cs}", ""), @"\s+", " ").Trim();

    // ── Minimum price per category ───────────────────────────────────────────

    public static decimal GetMinimumExpectedPrice(string category) => category switch
    {
        "iPhone"          => 500m,
        "iPad"            => 800m,
        "MacBook"         => 1_500m,
        "AirPods"         => 150m,
        "Apple Watch"     => 400m,
        "Apple Pencil"    => 80m,
        "EarPods"         => 50m,
        "iMac"            => 3_000m,
        "Mac"             => 2_000m,
        "Periférico Apple"=> 80m,
        "Cabos e Fontes"  => 20m,
        "Meta Quest"      => 800m,
        "Samsung"         => 250m,
        "Xiaomi"          => 150m,
        "Motorola"        => 150m,
        _                 => 50m
    };

    // ── Category derivation ──────────────────────────────────────────────────

    public static string DeriveCategory(string model) =>
        model.StartsWith("iPhone") ? "iPhone"
        : model.StartsWith("MacBook") ? "MacBook"
        : model.StartsWith("iPad") ? "iPad"
        : model.StartsWith("AirPods") ? "AirPods"
        : model.StartsWith("Apple Watch") ? "Apple Watch"
        : model.StartsWith("Apple Pencil") ? "Apple Pencil"
        : model.StartsWith("EarPods") ? "EarPods"
        : model.StartsWith("iMac") ? "iMac"
        : model.StartsWith("Mac Mini") || model.StartsWith("Mac Studio") ? "Mac"
        : model.StartsWith("Magic") || model.StartsWith("Smart Keyboard") ? "Periférico Apple"
        : model.StartsWith("Cabo") || model.StartsWith("Carregador") || model.StartsWith("Acessório Apple") ? "Cabos e Fontes"
        : model.StartsWith("Meta Quest") ? "Meta Quest"
        : model.StartsWith("Samsung") ? "Samsung"
        : model.StartsWith("Xiaomi") || model.StartsWith("Poco") || model.StartsWith("Redmi") ? "Xiaomi"
        : model.StartsWith("Motorola") ? "Motorola"
        : "Outros";

    public static string DeriveConditionName(Condition condition) => condition switch
    {
        Condition.New => "Novo",
        Condition.Used => "Seminovo",
        Condition.Refurbished => "Vitrine",
        Condition.Battery100 => "Bat. 100%",
        Condition.CPO => "CPO",
        _ => "—"
    };
}
