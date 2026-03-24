using Microsoft.EntityFrameworkCore;
using WhatsAppParser.Application.DTOs;
using WhatsAppParser.Application.Interfaces;
using WhatsAppParser.Domain.Entities;
using WhatsAppParser.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        // Fetch all recent price histories (e.g. last 30 days)
        var recentHistories = await _dbContext.PriceHistories
            .Include(p => p.Product)
            .Where(p => p.DateLogged >= DateTime.UtcNow.AddDays(-30))
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
        var prices = histories.Select(h => h.Price).ToList();
        
        if (!prices.Any()) return new PriceInsightDto();

        var avgPrice = prices.Average();
        var lowestPrice = prices.Min();

        return new PriceInsightDto
        {
            Brand = product.Brand,
            Model = product.Model,
            StorageCapacity = product.StorageCapacity,
            AveragePrice = Math.Round(avgPrice, 2),
            LowestPrice = lowestPrice,
            SuggestedResalePrice = Math.Round(avgPrice * RESALE_MARGIN_MULTIPLIER, 2),
            ListingCount = prices.Count
        };
    }
}
