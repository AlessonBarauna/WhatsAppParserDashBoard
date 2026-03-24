using MediatR;
using WhatsAppParser.Application.Common;

namespace WhatsAppParser.Application.Features.Admin.Commands;

/// <summary>
/// Deletes all PriceHistories, Products, RawMessages and Suppliers so data can be re-ingested.
/// </summary>
public sealed record ResetDataCommand : IRequest<Result<ResetDataResponse>>;

public sealed record ResetDataResponse(
    int PriceHistoriesDeleted,
    int ProductsDeleted,
    int RawMessagesDeleted,
    int SuppliersDeleted);
