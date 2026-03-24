using WhatsAppParser.Application.Interfaces;
using WhatsAppParser.Domain.Entities;
using WhatsAppParser.Infrastructure.Data;

namespace WhatsAppParser.Infrastructure.Repositories;

public sealed class RawMessageRepository(ApplicationDbContext dbContext) : IRawMessageRepository
{
    public void Add(RawMessage rawMessage) => dbContext.RawMessages.Add(rawMessage);
}
