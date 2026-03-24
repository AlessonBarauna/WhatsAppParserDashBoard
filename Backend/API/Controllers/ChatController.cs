using MediatR;
using Microsoft.AspNetCore.Mvc;
using WhatsAppParser.Application.Features.Chat.Commands;

namespace WhatsAppParser.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController(ISender sender) : ControllerBase
{
    public sealed record ChatRequest(
        string UserMessage,
        IReadOnlyList<ChatTurn>? History);

    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserMessage))
            return BadRequest("Mensagem não pode ser vazia.");

        var command = new ChatCommand(
            request.UserMessage,
            request.History ?? []);

        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}
