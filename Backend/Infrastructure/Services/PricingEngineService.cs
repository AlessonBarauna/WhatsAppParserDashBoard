using Microsoft.EntityFrameworkCore;
using WhatsAppParser.Application.DTOs;
using WhatsAppParser.Application.Interfaces;
using WhatsAppParser.Application.Services;
using WhatsAppParser.Domain.Entities;
using WhatsAppParser.Infrastructure.Data;

namespace WhatsAppParser.Infrastructure.Services;

public class PricingEngineService : IPricingEngine
{
    private readonly ApplicationDbContext _dbContext;
    private const decimal RESALE_MARGIN_MULTIPLIER = 1.10m; // 10%

    public PricingEngineService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<PriceInsightDto>> GetInsightsAsync()
    {
        // Use only today's prices for relevant market data
        var todayUtc = DateTime.UtcNow.Date;
        var recentHistories = await _dbContext.PriceHistories
            .Include(p => p.Product)
            .Include(p => p.Supplier)
            .Where(p => p.DateLogged >= todayUtc)
            .ToListAsync();

        var insights = recentHistories
            .GroupBy(ph => ph.Product)
            .Select(g => CalculateInsightsForProduct(g, g.Key))
            .OrderByDescending(i => i.ProfitMargin)
            .ToList();

        return insights;
    }

    public PriceInsightDto CalculateInsightsForProduct(IEnumerable<PriceHistory> histories, Product product)
    {
        var historyList = histories.ToList();

        if (historyList.Count == 0) return new PriceInsightDto();

        var prices = historyList.Select(h => h.Price).ToList();
        var avgPrice = prices.Average();
        var lowestEntry = historyList.MinBy(h => h.Price)!;

        var category = WhatsappMessageParser.DeriveCategory(product.Model);
        var minExpected = WhatsappMessageParser.GetMinimumExpectedPrice(category);

        return new PriceInsightDto
        {
            Brand = product.Brand,
            Model = product.Model,
            StorageCapacity = product.StorageCapacity,
            AveragePrice = Math.Round(avgPrice, 2),
            LowestPrice = lowestEntry.Price,
            SuggestedResalePrice = Math.Round(avgPrice * RESALE_MARGIN_MULTIPLIER, 2),
            ListingCount = prices.Count,
            LowestPriceSupplierName = lowestEntry.Supplier?.Name,
            Color = product.Color,
            ConditionName = WhatsappMessageParser.DeriveConditionName(product.Condition),
            OriginFlag = product.OriginFlag,
            IsAnomaly = lowestEntry.Price < minExpected
        };
    }
}
