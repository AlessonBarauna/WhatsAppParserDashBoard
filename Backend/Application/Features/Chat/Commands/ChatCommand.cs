using MediatR;
using WhatsAppParser.Application.Common;

namespace WhatsAppParser.Application.Features.Chat.Commands;

public sealed record ChatTurn(string Role, string Content);

public sealed record ChatCommand(
    string UserMessage,
    IReadOnlyList<ChatTurn> History
) : IRequest<Result<string>>;
