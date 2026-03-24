using System.Text.RegularExpressions;
using WhatsAppParser.Application.DTOs;
using WhatsAppParser.Application.Interfaces;
using WhatsAppParser.Domain.Enums;

namespace WhatsAppParser.Application.Services;

public class WhatsappMessageParser : IMessageParser
{
    public IEnumerable<ParseResultDto> ParseMessage(string rawText)
    {
        var results = new List<ParseResultDto>();
        if (string.IsNullOrWhiteSpace(rawText)) return results;

        // The raw message might contain multiple items (e.g., a list of phones)
        // We'll split by newlines and try to extract structured data per line/block.
        var lines = rawText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        ParseResultDto? currentItem = null;

        foreach (var line in lines)
        {
            var upperLine = line.ToUpperInvariant();

            // Try to identify if a line is starting a new product
            if (upperLine.Contains("IPHONE") || upperLine.Contains("XIAOMI") || upperLine.Contains("POCO") || upperLine.Contains("REDMI"))
            {
                if (currentItem != null && currentItem.IsValid)
                {
                    results.Add(currentItem);
                }

                currentItem = new ParseResultDto();
                
                // Extract Brand
                if (upperLine.Contains("IPHONE") || upperLine.Contains("APPLE"))
                    currentItem.Brand = Brand.Apple;
                else if (upperLine.Contains("XIAOMI") || upperLine.Contains("POCO") || upperLine.Contains("REDMI"))
                    currentItem.Brand = Brand.Xiaomi;

                // Extract Storage (e.g. 256GB, 128gb, 512 Gb)
                var storageMatch = Regex.Match(upperLine, @"(\d{2,4})\s*(GB|TB)", RegexOptions.IgnoreCase);
                if (storageMatch.Success)
                {
                    currentItem.StorageCapacity = storageMatch.Groups[1].Value + "GB";
                }

                // Extract Model
                // Simplifying to grab the text around the brand and before storage
                var modelPattern = currentItem.Brand == Brand.Apple 
                    ? @"IPHONE\s+([A-Z0-9\s]+(?:PRO\s+MAX|PRO|PLUS|MINI)?)" 
                    : @"(?:XIAOMI|POCO|REDMI)\s+([A-Z0-9\s]+)";
                
                var modelMatch = Regex.Match(upperLine, modelPattern);
                if (modelMatch.Success)
                {
                    var rawModel = modelMatch.Groups[1].Value.Trim();
                    // Clean up trailing "GB" or random words if regex over-matched
                    rawModel = Regex.Replace(rawModel, @"\d+\s*(GB|TB).*", "").Trim();
                    currentItem.Model = (currentItem.Brand == Brand.Apple ? "iPhone " : "Xiaomi ") + CleanString(rawModel);
                }

                // Extract Condition
                currentItem.Condition = ExtractCondition(upperLine);
            }

            // Extract Price from the current line if we have an active item
            if (currentItem != null)
            {
                var priceMatch = Regex.Match(line, @"(?:R\$|RS|\$)\s*(\d+[.,]?\d*)");
                if (priceMatch.Success)
                {
                    if (decimal.TryParse(priceMatch.Groups[1].Value.Replace(",", "."), out var price))
                    {
                        currentItem.Price = price;
                    }
                }
                else
                {
                    // Fallback to finding just a 3-5 digit number at the end of a line, indicating price
                    var barePriceMatch = Regex.Match(line, @"(?:\s|^)(\d{3,5})(?:\s|$)");
                    if (barePriceMatch.Success && currentItem.Price == 0)
                    {
                        if (decimal.TryParse(barePriceMatch.Groups[1].Value, out var price))
                        {
                            currentItem.Price = price;
                        }
                    }
                }

                // Extract Color
                var colors = new[] { "AZUL", "BRANCO", "PRETO", "VERDE", "ROSA", "AMARELO", "ROXO", "PRATA", "OURO", "GOLD", "SILVER", "BLUE", "BLACK", "WHITE", "GREEN", "PINK", "PURPLE", "YELLOW" };
                foreach (var color in colors)
                {
                    if (upperLine.Contains(color) && string.IsNullOrEmpty(currentItem.Color))
                    {
                        currentItem.Color = color;
                        break;
                    }
                }

                // Append any extra condition indicators found on subsequent lines
                if (currentItem.Condition == Condition.Unknown)
                {
                    currentItem.Condition = ExtractCondition(upperLine);
                }
            }
        }

        // Add the last item parsed if valid
        if (currentItem != null && currentItem.IsValid)
        {
            results.Add(currentItem);
        }

        return results;
    }

    private Condition ExtractCondition(string text)
    {
        if (text.Contains("LACRADO") || text.Contains("NOVO")) return Condition.New;
        if (text.Contains("SEMINOVO") || text.Contains("SEMI")) return Condition.Used;
        if (text.Contains("VITRINE") || text.Contains("SWAP")) return Condition.Refurbished;
        if (text.Contains("100%") || text.Contains("🇺🇸") || text.Contains("BATERIA 100")) return Condition.Battery100;
        return Condition.Unknown;
    }

    private string CleanString(string input)
    {
        // Remove emojis and multiple spaces
        var noEmojis = Regex.Replace(input, @"\p{Cs}", "");
        return Regex.Replace(noEmojis, @"\s+", " ").Trim();
    }
}
