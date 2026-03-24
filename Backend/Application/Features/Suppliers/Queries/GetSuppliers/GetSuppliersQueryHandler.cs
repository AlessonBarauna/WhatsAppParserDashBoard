using MediatR;
using Microsoft.EntityFrameworkCore;
using WhatsAppParser.Application.Common;
using WhatsAppParser.Application.Interfaces;

namespace WhatsAppParser.Application.Features.Suppliers.Queries.GetSuppliers;

public sealed class GetSuppliersQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetSuppliersQuery, Result<IReadOnlyList<SupplierDto>>>
{
    public async Task<Result<IReadOnlyList<SupplierDto>>> Handle(
        GetSuppliersQuery request,
        CancellationToken cancellationToken)
    {
        var suppliers = await dbContext.Suppliers
            .Select(s => new SupplierDto(
                s.Id,
                s.Name,
                s.PhoneNumber,
                s.ReliabilityScore,
                s.RawMessages.Count(),
                s.PriceHistories.Count()))
            .OrderByDescending(s => s.TotalPricesLogged)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<SupplierDto>>.Success(suppliers);
    }
}
