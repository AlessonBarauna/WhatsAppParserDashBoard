using MediatR;
using Microsoft.AspNetCore.Mvc;
using WhatsAppParser.Application.Features.Suppliers.Queries.GetSuppliers;

namespace WhatsAppParser.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SuppliersController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetSuppliers(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSuppliersQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(500, result.Error);
    }
}
