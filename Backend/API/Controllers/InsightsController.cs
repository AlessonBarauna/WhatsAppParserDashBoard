using Microsoft.AspNetCore.Mvc;
using WhatsAppParser.Application.Interfaces;

namespace WhatsAppParser.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InsightsController : ControllerBase
{
    private readonly IPricingEngine _pricingEngine;

    public InsightsController(IPricingEngine pricingEngine)
    {
        _pricingEngine = pricingEngine;
    }

    [HttpGet]
    public async Task<IActionResult> GetInsights()
    {
        var insights = await _pricingEngine.GetInsightsAsync();
        return Ok(insights);
    }
}
