using WhatsAppParser.Domain.Entities;

namespace WhatsAppParser.Application.Interfaces;

public interface ISupplierRepository
{
    Task<Supplier?> FindByPhoneOrNameAsync(
        string? phoneNumber,
        string? name,
        CancellationToken cancellationToken = default);

    void Add(Supplier supplier);
}
