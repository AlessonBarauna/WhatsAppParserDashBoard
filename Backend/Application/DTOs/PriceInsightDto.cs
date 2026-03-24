using WhatsAppParser.Domain.Enums;

namespace WhatsAppParser.Application.DTOs;

public class PriceInsightDto
{
    public Brand Brand { get; set; }
    public string Model { get; set; } = string.Empty;
    public string? StorageCapacity { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal LowestPrice { get; set; }
    public decimal SuggestedResalePrice { get; set; }
    public decimal ProfitMargin => SuggestedResalePrice - LowestPrice;
    public int ListingCount { get; set; }
}
