using WhatsAppParser.Application.DTOs;

namespace WhatsAppParser.Application.Interfaces;

public interface IMessageParser
{
    IEnumerable<ParseResultDto> ParseMessage(string rawText);
}
