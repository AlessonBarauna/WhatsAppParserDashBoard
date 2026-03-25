using WhatsAppParser.Domain.Enums;
using WhatsAppParser.Application.Services;

namespace WhatsAppParser.Application.Features.Products.Queries.GetProducts;

public sealed record ProductDto(
    Guid Id,
    Brand Brand,
    string BrandName,
    string Model,
    string Category,
    string? StorageCapacity,
    string? Color,
    Condition Condition,
    string ConditionName,
    string NormalizedName,
    decimal? LatestPrice,
    string? OriginFlag
);
