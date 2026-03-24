using MediatR;
using Microsoft.AspNetCore.Mvc;
using WhatsAppParser.Application.Features.Insights.Queries.GetInsights;

namespace WhatsAppParser.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InsightsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetInsights(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInsightsQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(500, result.Error);
    }
}
