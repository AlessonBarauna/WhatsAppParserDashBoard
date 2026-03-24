using MediatR;
using WhatsAppParser.Application.Common;
using WhatsAppParser.Application.DTOs;
using WhatsAppParser.Application.Interfaces;
using WhatsAppParser.Application.Services;

namespace WhatsAppParser.Application.Features.Insights.Queries.GetInsights;

public sealed class GetInsightsQueryHandler(IPricingEngine pricingEngine, IMarketPriceService marketPriceService)
    : IRequestHandler<GetInsightsQuery, Result<IReadOnlyList<PriceInsightDto>>>
{
    public async Task<Result<IReadOnlyList<PriceInsightDto>>> Handle(
        GetInsightsQuery request,
        CancellationToken cancellationToken)
    {
        var insights = (await pricingEngine.GetInsightsAsync()).ToList();

        // Enrich each insight with category + Mercado Livre reference price (parallel)
        var priceTasks = insights.Select(i =>
            marketPriceService.GetReferencePriceAsync(i.Model, i.StorageCapacity, cancellationToken));

        var marketPrices = await Task.WhenAll(priceTasks);

        for (int idx = 0; idx < insights.Count; idx++)
        {
            insights[idx].Category = WhatsappMessageParser.DeriveCategory(insights[idx].Model);
            insights[idx].MarketReferencePrice = marketPrices[idx];
        }

        // Sort: alphabetically by category, then model, then storage
        var sorted = insights
            .OrderBy(i => i.Category)
            .ThenBy(i => i.Model)
            .ThenBy(i => i.StorageCapacity)
            .ToList()
            .AsReadOnly();

        return Result<IReadOnlyList<PriceInsightDto>>.Success(sorted);
    }
}
