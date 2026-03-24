using MediatR;
using Microsoft.EntityFrameworkCore;
using WhatsAppParser.Application.Common;
using WhatsAppParser.Application.Interfaces;

namespace WhatsAppParser.Application.Features.Products.Queries.GetProducts;

public sealed class GetProductsQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetProductsQuery, Result<IReadOnlyList<ProductDto>>>
{
    public async Task<Result<IReadOnlyList<ProductDto>>> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        var products = await dbContext.Products
            .Include(p => p.PriceHistories)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProductDto(
                p.Id,
                p.Brand,
                p.Brand.ToString(),
                p.Model,
                p.StorageCapacity,
                p.Color,
                p.Condition,
                p.Condition.ToString(),
                p.NormalizedName,
                p.PriceHistories
                    .OrderByDescending(ph => ph.DateLogged)
                    .Select(ph => (decimal?)ph.Price)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<ProductDto>>.Success(products);
    }
}
