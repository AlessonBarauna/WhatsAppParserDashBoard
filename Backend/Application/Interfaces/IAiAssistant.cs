using WhatsAppParser.Application.Features.Chat.Commands;

namespace WhatsAppParser.Application.Interfaces;

public interface IAiAssistant
{
    Task<string> CompleteAsync(
        string systemPrompt,
        IReadOnlyList<ChatTurn> messages,
        CancellationToken cancellationToken = default);
}
