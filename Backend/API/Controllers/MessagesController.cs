using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WhatsAppParser.API.Controllers.Requests;
using WhatsAppParser.Application.Interfaces;
using WhatsAppParser.Domain.Entities;
using WhatsAppParser.Infrastructure.Data;

namespace WhatsAppParser.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessagesController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMessageParser _messageParser;

    public MessagesController(ApplicationDbContext dbContext, IMessageParser messageParser)
    {
        _dbContext = dbContext;
        _messageParser = messageParser;
    }

    [HttpPost]
    public async Task<IActionResult> Ingest([FromBody] IngestMessageRequest request)
    {
        // 1. Resolve Supplier
        Supplier? supplier = null;
        if (!string.IsNullOrEmpty(request.SupplierPhoneNumber) || !string.IsNullOrEmpty(request.SupplierName))
        {
            supplier = await _dbContext.Suppliers
                .FirstOrDefaultAsync(s => s.PhoneNumber == request.SupplierPhoneNumber || s.Name == request.SupplierName);

            if (supplier == null)
            {
                supplier = new Supplier
                {
                    Name = string.IsNullOrEmpty(request.SupplierName) ? "Unknown Supplier" : request.SupplierName,
                    PhoneNumber = request.SupplierPhoneNumber
                };
                _dbContext.Suppliers.Add(supplier);
            }
        }

        // 2. Save Raw Message
        var rawMessage = new RawMessage
        {
            OriginalText = request.RawText,
            Supplier = supplier,
            ProcessedSuccessfully = false
        };
        _dbContext.RawMessages.Add(rawMessage);

        try
        {
            // 3. Parse Message
            var parsedResults = _messageParser.ParseMessage(request.RawText);

            if (!parsedResults.Any())
            {
                rawMessage.ErrorMessage = "No valid products found in message.";
                await _dbContext.SaveChangesAsync();
                return Ok(new { Message = "Message ingested but no products parsed.", ParsedCount = 0 });
            }

            // 4. Save Products and Prices
            foreach (var result in parsedResults)
            {
                var normalizedName = $"{(result.Brand.ToString().ToUpperInvariant())} {result.Model.ToUpperInvariant()} {result.StorageCapacity}".Trim();
                
                var product = await _dbContext.Products
                    .FirstOrDefaultAsync(p => p.NormalizedName == normalizedName && p.Condition == result.Condition);

                if (product == null)
                {
                    product = new Product
                    {
                        Brand = result.Brand,
                        Model = result.Model,
                        StorageCapacity = result.StorageCapacity,
                        Color = result.Color,
                        Condition = result.Condition,
                        NormalizedName = normalizedName
                    };
                    _dbContext.Products.Add(product);
                }
                else if (string.IsNullOrEmpty(product.Color) && !string.IsNullOrEmpty(result.Color))
                {
                    // Update color if we parsed a new one
                    product.Color = result.Color;
                }

                var priceHistory = new PriceHistory
                {
                    Product = product,
                    Supplier = supplier,
                    RawMessage = rawMessage,
                    Price = result.Price,
                    DateLogged = DateTime.UtcNow
                };
                _dbContext.PriceHistories.Add(priceHistory);
            }

            rawMessage.ProcessedSuccessfully = true;
            await _dbContext.SaveChangesAsync();

            return Ok(new { Message = "Message fully ingested and parsed.", ParsedCount = parsedResults.Count() });
        }
        catch (Exception ex)
        {
            rawMessage.ErrorMessage = ex.Message;
            await _dbContext.SaveChangesAsync();
            return StatusCode(500, new { Message = "Error parsing message.", Error = ex.Message });
        }
    }
}
