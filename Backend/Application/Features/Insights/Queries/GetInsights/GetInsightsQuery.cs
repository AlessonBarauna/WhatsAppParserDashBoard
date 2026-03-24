using MediatR;
using WhatsAppParser.Application.Common;
using WhatsAppParser.Application.DTOs;

namespace WhatsAppParser.Application.Features.Insights.Queries.GetInsights;

public sealed record GetInsightsQuery : IRequest<Result<IReadOnlyList<PriceInsightDto>>>;
