using MediatR;
using Microsoft.AspNetCore.Mvc;
using WhatsAppParser.Application.Features.Messages.Commands.IngestMessage;
using WhatsAppParser.API.Controllers.Requests;

namespace WhatsAppParser.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessagesController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Ingest([FromBody] IngestMessageRequest request, CancellationToken cancellationToken)
    {
        var command = new IngestMessageCommand(request.RawText, request.SupplierName, request.SupplierPhoneNumber);
        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}
