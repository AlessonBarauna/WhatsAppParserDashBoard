using MediatR;
using Microsoft.EntityFrameworkCore;
using WhatsAppParser.Application.Common;
using WhatsAppParser.Application.Interfaces;
using WhatsAppParser.Application.Services;

namespace WhatsAppParser.Application.Features.Products.Queries.GetProducts;

public sealed class GetProductsQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetProductsQuery, Result<IReadOnlyList<ProductDto>>>
{
    public async Task<Result<IReadOnlyList<ProductDto>>> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        var todayUtc = DateTime.UtcNow.Date;

        var products = await dbContext.Products
            .Include(p => p.PriceHistories)
            .OrderBy(p => p.Model)
            .ThenBy(p => p.StorageCapacity)
            .Select(p => new ProductDto(
                p.Id,
                p.Brand,
                p.Brand.ToString(),
                p.Model,
                WhatsappMessageParser.DeriveCategory(p.Model),
                p.StorageCapacity,
                p.Color,
                p.Condition,
                WhatsappMessageParser.DeriveConditionName(p.Condition),
                p.NormalizedName,
                p.PriceHistories
                    .Where(ph => ph.DateLogged >= todayUtc)
                    .OrderByDescending(ph => ph.DateLogged)
                    .Select(ph => (decimal?)ph.Price)
                    .FirstOrDefault(),
                p.OriginFlag))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<ProductDto>>.Success(products);
    }
}
