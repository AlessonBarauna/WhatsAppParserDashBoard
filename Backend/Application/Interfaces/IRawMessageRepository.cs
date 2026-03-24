using WhatsAppParser.Domain.Entities;

namespace WhatsAppParser.Application.Interfaces;

public interface IRawMessageRepository
{
    void Add(RawMessage rawMessage);
}
