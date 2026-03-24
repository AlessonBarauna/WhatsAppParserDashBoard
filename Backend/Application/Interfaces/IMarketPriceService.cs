namespace WhatsAppParser.Application.Interfaces;

public interface IMarketPriceService
{
    /// <summary>Returns median market price in BRL from Mercado Livre, or null if unavailable.</summary>
    Task<decimal?> GetReferencePriceAsync(string productModel, string? storage, CancellationToken ct = default);
}
