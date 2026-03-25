namespace WhatsAppParser.Application.Features.Suppliers.Queries.GetSuppliers;

public sealed record SupplierProductItem(
    string Model,
    string Category,
    string? StorageCapacity,
    string? Color,
    string ConditionName,
    decimal LowestPrice
);

public sealed record SupplierDto(
    Guid Id,
    string Name,
    string? PhoneNumber,
    int ReliabilityScore,
    int TotalMessages,
    int TotalPricesLogged,
    int TodayProductCount,
    IReadOnlyList<string> TodayCategories,
    IReadOnlyList<SupplierProductItem> TodayProducts
);
