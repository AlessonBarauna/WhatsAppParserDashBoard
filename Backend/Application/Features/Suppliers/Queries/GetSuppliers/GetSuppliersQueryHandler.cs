using MediatR;
using Microsoft.EntityFrameworkCore;
using WhatsAppParser.Application.Common;
using WhatsAppParser.Application.Interfaces;
using WhatsAppParser.Application.Services;

namespace WhatsAppParser.Application.Features.Suppliers.Queries.GetSuppliers;

public sealed class GetSuppliersQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetSuppliersQuery, Result<IReadOnlyList<SupplierDto>>>
{
    public async Task<Result<IReadOnlyList<SupplierDto>>> Handle(
        GetSuppliersQuery request,
        CancellationToken cancellationToken)
    {
        var todayUtc = DateTime.UtcNow.Date;

        // Load all suppliers with total counts
        var suppliers = await dbContext.Suppliers
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.PhoneNumber,
                s.ReliabilityScore,
                TotalMessages = s.RawMessages.Count(),
                TotalPricesLogged = s.PriceHistories.Count()
            })
            .ToListAsync(cancellationToken);

        // Load today's price histories per supplier
        var todayHistories = await dbContext.PriceHistories
            .Include(ph => ph.Product)
            .Where(ph => ph.DateLogged >= todayUtc && ph.SupplierId != null)
            .ToListAsync(cancellationToken);

        var historiesBySupplier = todayHistories
            .GroupBy(ph => ph.SupplierId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = suppliers
            .Select(s =>
            {
                var histories = historiesBySupplier.GetValueOrDefault(s.Id, []);

                // One entry per distinct product (lowest price from this supplier today)
                var products = histories
                    .GroupBy(ph => ph.ProductId)
                    .Select(g =>
                    {
                        var best = g.MinBy(h => h.Price)!;
                        return new SupplierProductItem(
                            best.Product.Model,
                            WhatsappMessageParser.DeriveCategory(best.Product.Model),
                            best.Product.StorageCapacity,
                            best.Product.Color,
                            WhatsappMessageParser.DeriveConditionName(best.Product.Condition),
                            best.Price);
                    })
                    .OrderBy(p => p.Category)
                    .ThenBy(p => p.Model)
                    .ThenBy(p => p.StorageCapacity)
                    .ToList();

                var categories = products
                    .Select(p => p.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();

                return new SupplierDto(
                    s.Id,
                    s.Name,
                    s.PhoneNumber,
                    s.ReliabilityScore,
                    s.TotalMessages,
                    s.TotalPricesLogged,
                    products.Count,
                    categories,
                    products);
            })
            .OrderByDescending(s => s.TodayProductCount)
            .ThenBy(s => s.Name)
            .ToList();

        return Result<IReadOnlyList<SupplierDto>>.Success(result);
    }
}
