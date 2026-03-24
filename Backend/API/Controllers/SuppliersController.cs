using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WhatsAppParser.Infrastructure.Data;

namespace WhatsAppParser.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SuppliersController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public SuppliersController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetSuppliers()
    {
        var suppliers = await _dbContext.Suppliers
            .Select(s => new {
                s.Id,
                s.Name,
                s.PhoneNumber,
                s.ReliabilityScore,
                TotalMessages = s.RawMessages.Count(),
                TotalPricesLogged = s.PriceHistories.Count()
            })
            .OrderByDescending(s => s.TotalPricesLogged)
            .ToListAsync();

        return Ok(suppliers);
    }
}
