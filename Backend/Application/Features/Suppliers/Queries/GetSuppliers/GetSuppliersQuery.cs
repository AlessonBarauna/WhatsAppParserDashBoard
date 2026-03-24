using MediatR;
using WhatsAppParser.Application.Common;

namespace WhatsAppParser.Application.Features.Suppliers.Queries.GetSuppliers;

public sealed record GetSuppliersQuery : IRequest<Result<IReadOnlyList<SupplierDto>>>;
