using WhatsAppParser.Application.Interfaces;
using WhatsAppParser.Domain.Entities;
using WhatsAppParser.Infrastructure.Data;

namespace WhatsAppParser.Infrastructure.Repositories;

public sealed class PriceHistoryRepository(ApplicationDbContext dbContext) : IPriceHistoryRepository
{
    public void Add(PriceHistory priceHistory) => dbContext.PriceHistories.Add(priceHistory);
}
