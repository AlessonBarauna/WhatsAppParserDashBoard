namespace WhatsAppParser.Application.Features.Suppliers.Queries.GetSuppliers;

public sealed record SupplierDto(
    Guid Id,
    string Name,
    string? PhoneNumber,
    int ReliabilityScore,
    int TotalMessages,
    int TotalPricesLogged
);
