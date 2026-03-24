using WhatsAppParser.Domain.Entities;
using WhatsAppParser.Domain.Enums;

namespace WhatsAppParser.Application.Interfaces;

public interface IProductRepository
{
    Task<Product?> FindByNormalizedNameAndConditionAsync(
        string normalizedName,
        Condition condition,
        CancellationToken cancellationToken = default);

    void Add(Product product);
}
