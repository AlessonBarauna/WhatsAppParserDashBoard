using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WhatsAppParser.Application.Common;
using WhatsAppParser.Application.Interfaces;

namespace WhatsAppParser.Application.Features.Admin.Commands;

public sealed class ResetDataCommandHandler(
    IApplicationDbContext db,
    ILogger<ResetDataCommandHandler> logger)
    : IRequestHandler<ResetDataCommand, Result<ResetDataResponse>>
{
    public async Task<Result<ResetDataResponse>> Handle(
        ResetDataCommand request,
        CancellationToken cancellationToken)
    {
        // Order matters: delete dependents before parents
        var priceHistories = await db.PriceHistories.CountAsync(cancellationToken);
        await db.PriceHistories.ExecuteDeleteAsync(cancellationToken);

        var rawMessages = await db.RawMessages.CountAsync(cancellationToken);
        await db.RawMessages.ExecuteDeleteAsync(cancellationToken);

        var products = await db.Products.CountAsync(cancellationToken);
        await db.Products.ExecuteDeleteAsync(cancellationToken);

        var suppliers = await db.Suppliers.CountAsync(cancellationToken);
        await db.Suppliers.ExecuteDeleteAsync(cancellationToken);

        logger.LogWarning(
            "Data reset: {PH} price histories, {P} products, {RM} raw messages, {S} suppliers deleted",
            priceHistories, products, rawMessages, suppliers);

        return Result<ResetDataResponse>.Success(
            new ResetDataResponse(priceHistories, products, rawMessages, suppliers));
    }
}
