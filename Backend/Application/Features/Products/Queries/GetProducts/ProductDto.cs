using WhatsAppParser.Domain.Enums;

namespace WhatsAppParser.Application.Features.Products.Queries.GetProducts;

public sealed record ProductDto(
    Guid Id,
    Brand Brand,
    string BrandName,
    string Model,
    string? StorageCapacity,
    string? Color,
    Condition Condition,
    string ConditionName,
    string NormalizedName,
    decimal? LatestPrice
);
