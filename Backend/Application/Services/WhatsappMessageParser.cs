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

    // Bare number 100–99999, NOT followed by GB/TB/MHz/%/MB
    private static readonly Regex PriceFallback =
        new(@"(?:^|\s)((?:[1-9]\d{2,4})(?:[.,]\d{2})?)(?!\s*(?:GB|TB|MHZ|%|MB))(?:\s|$)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex StorageRegex =
        new(@"\b(\d{2,4})\s*(?:GB|TB)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // RAM: 8GB, 16GB, 32GB — to skip when extracting storage
    private static readonly Regex RamIndicator =
        new(@"\b(?:8|12|16|32|64)\s*GB\s*(?:RAM|DE RAM)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public IEnumerable<ParseResultDto> ParseMessage(string rawText)
    {
        var results = new List<ParseResultDto>();
        if (string.IsNullOrWhiteSpace(rawText)) return results;

        var lines = rawText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        ParseResultDto? current = null;

        foreach (var line in lines)
        {
            var upper = line.ToUpperInvariant();
            var brand = DetectBrand(upper);

            if (brand != Brand.Unknown)
            {
                if (current is { IsValid: true }) results.Add(current);

                current = new ParseResultDto { Brand = brand };
                current.StorageCapacity = ExtractStorage(upper);
                current.Model = ExtractModel(line, upper, brand);
                current.Condition = ExtractCondition(upper);
            }

            if (current is null) continue;

            if (current.Price == 0)
            {
                var price = TryExtractPrice(line);
                if (price is > 50 and < 1_000_000)
                    current.Price = price.Value;
            }

            if (current.StorageCapacity is null)
            {
                var storage = ExtractStorage(upper);
                if (storage is not null) current.StorageCapacity = storage;
            }

            if (current.Color is null) current.Color = ExtractColor(upper);

            if (current.Condition == Condition.Unknown)
                current.Condition = ExtractCondition(upper);
        }

        if (current is { IsValid: true }) results.Add(current);
        return results;
    }

    // ── Brand detection ────────────────────────────────────────────────────

    private static Brand DetectBrand(string upper)
    {
        if (upper.Contains("IPHONE") || upper.Contains("MACBOOK") || upper.Contains("IPAD") ||
            upper.Contains("AIRPODS") || upper.Contains("APPLE WATCH") || upper.Contains("IMAC") ||
            upper.Contains("MAC MINI") || upper.Contains("MAC STUDIO"))
            return Brand.Apple;

        if (upper.Contains("XIAOMI") || upper.Contains("POCO") || upper.Contains("REDMI"))
            return Brand.Xiaomi;

        if (upper.Contains("SAMSUNG") || upper.Contains("GALAXY"))
            return Brand.Samsung;

        if (upper.Contains("MOTOROLA") || Regex.IsMatch(upper, @"\bMOTO\s+[A-Z]"))
            return Brand.Motorola;

        return Brand.Unknown;
    }

    // ── Model extraction ───────────────────────────────────────────────────

    private static string ExtractModel(string original, string upper, Brand brand)
    {
        if (brand == Brand.Apple) return ExtractAppleModel(original, upper);
        if (brand == Brand.Samsung) return ExtractSamsungModel(upper);
        if (brand == Brand.Motorola) return ExtractMotorolaModel(upper);
        return ExtractXiaomiModel(upper);
    }

    private static string ExtractAppleModel(string original, string upper)
    {
        // MacBook
        if (upper.Contains("MACBOOK"))
        {
            var m = Regex.Match(upper, @"MACBOOK\s+(AIR|PRO)?\s*(M\d)?");
            if (m.Success)
            {
                var type = m.Groups[1].Success ? m.Groups[1].Value : "";
                var chip = m.Groups[2].Success ? " " + m.Groups[2].Value : "";
                return CleanString($"MacBook {type}{chip}").Trim();
            }
            return "MacBook";
        }

        // iPad
        if (upper.Contains("IPAD"))
        {
            var m = Regex.Match(upper,
                @"IPAD\s*(PRO|AIR|MINI|STANDARD)?\s*(M\d)?",
                RegexOptions.IgnoreCase);
            if (m.Success)
            {
                var type = m.Groups[1].Success ? " " + m.Groups[1].Value : "";
                return CleanString($"iPad{type}").Trim();
            }
            return "iPad";
        }

        // AirPods
        if (upper.Contains("AIRPODS"))
        {
            if (upper.Contains("MAX")) return "AirPods Max";
            if (upper.Contains("PRO")) return "AirPods Pro";
            return "AirPods";
        }

        // Apple Watch
        if (upper.Contains("APPLE WATCH"))
        {
            var m = Regex.Match(upper, @"APPLE WATCH\s*(ULTRA|SE|SERIES\s*\d+|\d+)?", RegexOptions.IgnoreCase);
            var variant = m.Groups[1].Success ? " " + m.Groups[1].Value.Trim() : "";
            return CleanString($"Apple Watch{variant}");
        }

        // iMac
        if (upper.Contains("IMAC")) return "iMac";

        // Mac Mini / Studio
        if (upper.Contains("MAC MINI")) return "Mac Mini";
        if (upper.Contains("MAC STUDIO")) return "Mac Studio";

        // iPhone
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
        // Galaxy S, A, Note, Tab, etc.
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

    // ── Price ──────────────────────────────────────────────────────────────

    private static string? ExtractStorage(string upper)
    {
        // Remove RAM indicators first so "8GB RAM" doesn't match as storage
        var cleaned = RamIndicator.Replace(upper, " ");
        var m = StorageRegex.Match(cleaned);
        return m.Success ? m.Groups[1].Value + "GB" : null;
    }

    private static decimal? TryExtractPrice(string line)
    {
        var m = PriceWithSymbol.Match(line);
        if (m.Success)
        {
            var price = ParseBrazilianDecimal(m.Groups[1].Value);
            if (price is > 50) return price;
        }

        m = PriceFallback.Match(line);
        if (m.Success)
            return ParseBrazilianDecimal(m.Groups[1].Value);

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

    // ── Helpers ────────────────────────────────────────────────────────────

    private static Condition ExtractCondition(string upper)
    {
        if (upper.Contains("LACRADO") || upper.Contains("NOVO") && !upper.Contains("SEMI") || upper.Contains("NEW")) return Condition.New;
        if (upper.Contains("SEMINOVO") || upper.Contains("SEMI NOVO") || upper.Contains("USADO")) return Condition.Used;
        if (upper.Contains("VITRINE") || upper.Contains("SWAP") || upper.Contains("REFURB")) return Condition.Refurbished;
        if (upper.Contains("100%") || upper.Contains("BATERIA 100") || upper.Contains("BAT 100")) return Condition.Battery100;
        return Condition.Unknown;
    }

    private static string? ExtractColor(string upper)
    {
        string[] colors = [
            "PRETO", "BRANCO", "AZUL", "VERDE", "ROSA", "AMARELO", "ROXO",
            "PRATA", "DOURADO", "OURO", "GRAFITE", "TITANIO", "TITANIUM",
            "CIANO", "CORAL", "LARANJA", "VERMELHO",
            "BLACK", "WHITE", "BLUE", "GREEN", "PINK", "YELLOW", "PURPLE",
            "SILVER", "GOLD", "GRAPHITE", "STARLIGHT", "MIDNIGHT", "NATURAL"
        ];
        foreach (var c in colors)
            if (upper.Contains(c)) return c;
        return null;
    }

    private static string CleanString(string input) =>
        Regex.Replace(Regex.Replace(input, @"\p{Cs}", ""), @"\s+", " ").Trim();

    // ── Category derivation (used by DTOs) ────────────────────────────────

    public static string DeriveCategory(string model) =>
        model.StartsWith("iPhone") ? "iPhone"
        : model.StartsWith("MacBook") ? "MacBook"
        : model.StartsWith("iPad") ? "iPad"
        : model.StartsWith("AirPods") ? "AirPods"
        : model.StartsWith("Apple Watch") ? "Apple Watch"
        : model.StartsWith("iMac") ? "iMac"
        : model.StartsWith("Mac Mini") || model.StartsWith("Mac Studio") ? "Mac"
        : model.StartsWith("Samsung") ? "Samsung"
        : model.StartsWith("Xiaomi") || model.StartsWith("Poco") || model.StartsWith("Redmi") ? "Xiaomi"
        : model.StartsWith("Motorola") ? "Motorola"
        : "Outros";
}
