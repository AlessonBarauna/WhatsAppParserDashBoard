using Microsoft.EntityFrameworkCore;
using WhatsAppParser.Domain.Entities;

namespace WhatsAppParser.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Product> Products { get; }
    DbSet<Supplier> Suppliers { get; }
    DbSet<RawMessage> RawMessages { get; }
    DbSet<PriceHistory> PriceHistories { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
