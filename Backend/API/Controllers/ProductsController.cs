using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WhatsAppParser.Infrastructure.Data;

namespace WhatsAppParser.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public ProductsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        var products = await _dbContext.Products
            .Include(p => p.PriceHistories)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
            
        // Map to a response DTO ideally, using anonymous object for now
        var result = products.Select(p => new {
            p.Id,
            p.Brand,
            BrandName = p.Brand.ToString(),
            p.Model,
            p.StorageCapacity,
            p.Color,
            p.Condition,
            ConditionString = p.Condition.ToString(),
            p.NormalizedName,
            LatestPrice = p.PriceHistories.OrderByDescending(ph => ph.DateLogged).FirstOrDefault()?.Price
        });

        return Ok(result);
    }
}
