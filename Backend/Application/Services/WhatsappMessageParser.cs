using System.Globalization;
using System.Text.RegularExpressions;
using WhatsAppParser.Application.DTOs;
using WhatsAppParser.Application.Interfaces;
using WhatsAppParser.Domain.Enums;

namespace WhatsAppParser.Application.Services;

public class WhatsappMessageParser : IMessageParser
{
    // Matches optional R$/RS and then a number with optional BR/US separators
    // Examples: R$840  840  1.200  1.200,00  1,200  1200,00  840,00
    private static readonly Regex PriceWithSymbol =
        new(@"(?:R\$|R\s*\$|RS\$?|\$)\s*([\d.,]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Fallback: standalone number 100–99999, NOT followed by GB/TB/MHz/%
    private static readonly Regex PriceFallback =
        new(@"(?:^|\s)((?:[1-9]\d{2,4})(?:[.,]\d{2})?)(?!\s*(?:GB|TB|MHZ|%|MB))(?:\s|$)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Storage: 64/128/256/512 GB|TB
    private static readonly Regex StorageRegex =
        new(@"\b(\d{2,4})\s*(?:GB|TB)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
                current.Model = ExtractModel(upper, brand);
                current.Condition = ExtractCondition(upper);
            }

            if (current is null) continue;

            // Price — try explicit symbol first, then fallback
            if (current.Price == 0)
            {
                var price = TryExtractPrice(line);
                if (price is > 50 and < 1_000_000)
                    current.Price = price.Value;
            }

            // Storage may appear on a continuation line (e.g. "256GB" on its own)
            if (current.StorageCapacity is null)
            {
                var storage = ExtractStorage(upper);
                if (storage is not null) current.StorageCapacity = storage;
            }

            // Color
            if (current.Color is null) current.Color = ExtractColor(upper);

            // Condition on continuation lines
            if (current.Condition == Condition.Unknown)
                current.Condition = ExtractCondition(upper);
        }

        if (current is { IsValid: true }) results.Add(current);

        return results;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Brand DetectBrand(string upper) =>
        upper.Contains("IPHONE") || upper.Contains("APPLE") ? Brand.Apple
        : upper.Contains("XIAOMI") || upper.Contains("POCO") || upper.Contains("REDMI") ? Brand.Xiaomi
        : upper.Contains("SAMSUNG") || upper.Contains("GALAXY") ? Brand.Samsung
        : upper.Contains("MOTOROLA") || upper.Contains("MOTO ") || upper.Contains(" MOTO") ? Brand.Motorola
        : Brand.Unknown;

    private static string? ExtractStorage(string upper)
    {
        var m = StorageRegex.Match(upper);
        return m.Success ? m.Groups[1].Value + "GB" : null;
    }

    private static string ExtractModel(string upper, Brand brand)
    {
        var pattern = brand switch
        {
            Brand.Apple    => @"IPHONE\s+([A-Z0-9]+(?:\s+(?:PRO\s+MAX|PRO\s+MAX\s+TITANIUM|PRO|PLUS|MINI))?)",
            Brand.Xiaomi   => @"(?:XIAOMI|POCO|REDMI)\s+([A-Z0-9]+(?:\s+[A-Z0-9]+){0,3})",
            Brand.Samsung  => @"(?:SAMSUNG\s+)?(?:GALAXY\s+)?([A-Z][0-9]+[A-Z]?(?:\s+(?:FE|ULTRA|PLUS|5G))?)",
            Brand.Motorola => @"(?:MOTOROLA\s+)?MOTO\s+([A-Z][0-9]+(?:\s+(?:PLUS|G|S))?)",
            _              => @"(\S+)"
        };

        var m = Regex.Match(upper, pattern);
        if (!m.Success) return brand.ToString();

        var raw = m.Groups[1].Value.Trim();
        // Remove trailing storage or garbage
        raw = Regex.Replace(raw, @"\d+\s*(GB|TB).*", "", RegexOptions.IgnoreCase).Trim();

        var prefix = brand switch
        {
            Brand.Apple    => "iPhone ",
            Brand.Xiaomi   => "Xiaomi ",
            Brand.Samsung  => "Samsung ",
            Brand.Motorola => "Motorola ",
            _              => ""
        };

        return prefix + CleanString(raw);
    }

    private static decimal? TryExtractPrice(string line)
    {
        // 1. Try with currency symbol
        var m = PriceWithSymbol.Match(line);
        if (m.Success)
        {
            var price = ParseBrazilianDecimal(m.Groups[1].Value);
            if (price is > 50) return price;
        }

        // 2. Fallback: bare number not followed by GB/TB/%
        m = PriceFallback.Match(line);
        if (m.Success)
            return ParseBrazilianDecimal(m.Groups[1].Value);

        return null;
    }

    /// <summary>
    /// Parses numbers in both Brazilian (1.234,56) and plain (1234) formats.
    /// Never uses thread culture to avoid locale-dependent bugs.
    /// </summary>
    private static decimal? ParseBrazilianDecimal(string raw)
    {
        raw = raw.Trim();
        if (string.IsNullOrEmpty(raw)) return null;

        bool hasComma = raw.Contains(',');
        bool hasDot   = raw.Contains('.');

        string normalized;

        if (hasComma && hasDot)
        {
            // Determine which is decimal separator by which comes last
            if (raw.LastIndexOf(',') > raw.LastIndexOf('.'))
                // BR format: 1.234,56
                normalized = raw.Replace(".", "").Replace(",", ".");
            else
                // US format: 1,234.56
                normalized = raw.Replace(",", "");
        }
        else if (hasComma)
        {
            var parts = raw.Split(',');
            // "840,00" or "1,200,00" — comma as decimal if last part has ≤2 digits
            normalized = parts.Length == 2 && parts[1].Length <= 2
                ? raw.Replace(",", ".")         // decimal: "840,00" → "840.00"
                : raw.Replace(",", "");          // thousands: "1,200" → "1200"
        }
        else if (hasDot)
        {
            var parts = raw.Split('.');
            // "1.200" or "69.000" — dot as thousands separator if last part has exactly 3 digits
            normalized = parts.Length == 2 && parts[1].Length == 3
                ? raw.Replace(".", "")           // thousands: "69.000" → "69000"
                : raw;                           // decimal: "840.50" → "840.50"
        }
        else
        {
            normalized = raw; // plain integer
        }

        return decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    private static Condition ExtractCondition(string upper)
    {
        if (upper.Contains("LACRADO") || upper.Contains("NOVO") || upper.Contains("NEW")) return Condition.New;
        if (upper.Contains("SEMINOVO") || upper.Contains("SEMI NOVO") || upper.Contains("USADO")) return Condition.Used;
        if (upper.Contains("VITRINE") || upper.Contains("SWAP") || upper.Contains("REFURB")) return Condition.Refurbished;
        if (upper.Contains("100%") || upper.Contains("BATERIA 100") || upper.Contains("BAT 100")) return Condition.Battery100;
        return Condition.Unknown;
    }

    private static string? ExtractColor(string upper)
    {
        string[] colors = [
            "PRETO", "BRANCO", "AZUL", "VERDE", "ROSA", "AMARELO", "ROXO",
            "PRATA", "DOURADO", "OURO", "GRAFITE", "TITANIO", "CIANO",
            "BLACK", "WHITE", "BLUE", "GREEN", "PINK", "YELLOW", "PURPLE",
            "SILVER", "GOLD", "GRAPHITE", "TITANIUM"
        ];

        foreach (var color in colors)
            if (upper.Contains(color)) return color;

        return null;
    }

    private static string CleanString(string input) =>
        Regex.Replace(Regex.Replace(input, @"\p{Cs}", ""), @"\s+", " ").Trim();
}
