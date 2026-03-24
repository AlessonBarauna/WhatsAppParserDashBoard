using WhatsAppParser.Domain.Entities;

namespace WhatsAppParser.Application.Interfaces;

public interface IPriceHistoryRepository
{
    void Add(PriceHistory priceHistory);
}
