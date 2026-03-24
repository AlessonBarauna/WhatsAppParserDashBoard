using Microsoft.EntityFrameworkCore;
using WhatsAppParser.Application.Interfaces;
using WhatsAppParser.Domain.Entities;
using WhatsAppParser.Infrastructure.Data;

namespace WhatsAppParser.Infrastructure.Repositories;

public sealed class SupplierRepository(ApplicationDbContext dbContext) : ISupplierRepository
{
    public Task<Supplier?> FindByPhoneOrNameAsync(
        string? phoneNumber,
        string? name,
        CancellationToken cancellationToken = default) =>
        dbContext.Suppliers.FirstOrDefaultAsync(
            s => s.PhoneNumber == phoneNumber || s.Name == name,
            cancellationToken);

    public void Add(Supplier supplier) => dbContext.Suppliers.Add(supplier);
}
