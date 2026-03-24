using WhatsAppParser.Application.DTOs;
using WhatsAppParser.Domain.Entities;

namespace WhatsAppParser.Application.Interfaces;

public interface IPricingEngine
{
    Task<IEnumerable<PriceInsightDto>> GetInsightsAsync();
    PriceInsightDto CalculateInsightsForProduct(IEnumerable<PriceHistory> histories, Product product);
}
