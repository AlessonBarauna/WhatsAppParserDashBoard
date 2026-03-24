using MediatR;
using WhatsAppParser.Application.Common;
using WhatsAppParser.Application.DTOs;
using WhatsAppParser.Application.Interfaces;

namespace WhatsAppParser.Application.Features.Insights.Queries.GetInsights;

public sealed class GetInsightsQueryHandler(IPricingEngine pricingEngine)
    : IRequestHandler<GetInsightsQuery, Result<IReadOnlyList<PriceInsightDto>>>
{
    public async Task<Result<IReadOnlyList<PriceInsightDto>>> Handle(
        GetInsightsQuery request,
        CancellationToken cancellationToken)
    {
        var insights = await pricingEngine.GetInsightsAsync();
        return Result<IReadOnlyList<PriceInsightDto>>.Success(
            insights.ToList().AsReadOnly());
    }
}
