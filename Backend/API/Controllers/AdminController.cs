using MediatR;
using Microsoft.AspNetCore.Mvc;
using WhatsAppParser.Application.Features.Admin.Commands;

namespace WhatsAppParser.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Deletes ALL ingested data so you can re-upload with the corrected parser.
    /// </summary>
    [HttpDelete("reset")]
    public async Task<IActionResult> Reset(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ResetDataCommand(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}
