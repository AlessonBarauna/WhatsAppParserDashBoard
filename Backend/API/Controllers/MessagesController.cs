using MediatR;
using Microsoft.AspNetCore.Mvc;
using WhatsAppParser.Application.Features.Messages.Commands.IngestMessage;
using WhatsAppParser.Application.Features.Messages.Commands.ProcessFile;
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

    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromForm] string supplierName,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file provided.");

        using var reader = new StreamReader(file.OpenReadStream());
        var fileContent = await reader.ReadToEndAsync(cancellationToken);

        var command = new ProcessFileCommand(fileContent, supplierName);
        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}
