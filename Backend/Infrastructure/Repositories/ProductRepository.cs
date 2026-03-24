using Microsoft.EntityFrameworkCore;
using WhatsAppParser.Application.Interfaces;
using WhatsAppParser.Domain.Entities;
using WhatsAppParser.Domain.Enums;
using WhatsAppParser.Infrastructure.Data;

namespace WhatsAppParser.Infrastructure.Repositories;

public sealed class ProductRepository(ApplicationDbContext dbContext) : IProductRepository
{
    public Task<Product?> FindByNormalizedNameAndConditionAsync(
        string normalizedName,
        Condition condition,
        CancellationToken cancellationToken = default) =>
        dbContext.Products.FirstOrDefaultAsync(
            p => p.NormalizedName == normalizedName && p.Condition == condition,
            cancellationToken);

    public void Add(Product product) => dbContext.Products.Add(product);
}
