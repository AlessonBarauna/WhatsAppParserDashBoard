using MediatR;
using WhatsAppParser.Application.Common;
using WhatsAppParser.Application.Interfaces;
using WhatsAppParser.Domain.Entities;

namespace WhatsAppParser.Application.Features.Messages.Commands.IngestMessage;

public sealed class IngestMessageCommandHandler(
    IApplicationDbContext dbContext,
    ISupplierRepository supplierRepository,
    IRawMessageRepository rawMessageRepository,
    IProductRepository productRepository,
    IPriceHistoryRepository priceHistoryRepository,
    IMessageParser parser
) : IRequestHandler<IngestMessageCommand, Result<IngestMessageResponse>>
{
    public async Task<Result<IngestMessageResponse>> Handle(
        IngestMessageCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Resolve or create Supplier
        Supplier? supplier = null;
        if (!string.IsNullOrEmpty(request.SupplierPhoneNumber) || !string.IsNullOrEmpty(request.SupplierName))
        {
            supplier = await supplierRepository.FindByPhoneOrNameAsync(
                request.SupplierPhoneNumber,
                request.SupplierName,
                cancellationToken);

            if (supplier is null)
            {
                supplier = new Supplier
                {
                    Name = string.IsNullOrEmpty(request.SupplierName) ? "Unknown Supplier" : request.SupplierName,
                    PhoneNumber = request.SupplierPhoneNumber
                };
                supplierRepository.Add(supplier);
            }
        }

        // 2. Persist raw message
        var rawMessage = new RawMessage
        {
            OriginalText = request.RawText,
            Supplier = supplier,
            ProcessedSuccessfully = false
        };
        rawMessageRepository.Add(rawMessage);

        // 3. Parse
        var parsedResults = parser.ParseMessage(request.RawText)
            .Where(r => r.IsValid)
            .ToList();

        if (parsedResults.Count == 0)
        {
            rawMessage.ErrorMessage = "No valid products found in message.";
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<IngestMessageResponse>.Success(
                new IngestMessageResponse("Message ingested but no products parsed.", 0));
        }

        // 4. Create or update Products and log PriceHistory
        foreach (var result in parsedResults)
        {
            var normalizedName = $"{result.Brand.ToString().ToUpperInvariant()} {result.Model.ToUpperInvariant()} {result.StorageCapacity}".Trim();

            var product = await productRepository.FindByNormalizedNameAndConditionAsync(
                normalizedName, result.Condition, cancellationToken);

            if (product is null)
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
                productRepository.Add(product);
            }
            else if (string.IsNullOrEmpty(product.Color) && !string.IsNullOrEmpty(result.Color))
            {
                product.Color = result.Color;
            }

            priceHistoryRepository.Add(new PriceHistory
            {
                Product = product,
                Supplier = supplier,
                RawMessage = rawMessage,
                Price = result.Price,
                DateLogged = DateTime.UtcNow
            });
        }

        rawMessage.ProcessedSuccessfully = true;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<IngestMessageResponse>.Success(
            new IngestMessageResponse("Message fully ingested and parsed.", parsedResults.Count));
    }
}
