using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using WhatsAppParser.Application.Interfaces;

namespace WhatsAppParser.Infrastructure.Services;

public sealed class MercadoLivreService(HttpClient httpClient, ILogger<MercadoLivreService> logger)
    : IMarketPriceService
{
    // Mercado Livre Brasil site id
    private const string BaseUrl = "https://api.mercadolibre.com/sites/MLB/search";

    public async Task<decimal?> GetReferencePriceAsync(
        string productModel,
        string? storage,
        CancellationToken ct = default)
    {
        try
        {
            var query = Uri.EscapeDataString(storage is null ? productModel : $"{productModel} {storage}");
            var url = $"{BaseUrl}?q={query}&limit=20&condition=new";

            var response = await httpClient.GetFromJsonAsync<MlSearchResponse>(url, ct);
            if (response?.Results is null or { Count: 0 }) return null;

            // Filter: only items priced between R$50 and R$150,000
            var prices = response.Results
                .Where(r => r.Price is > 50 and < 150_000)
                .Select(r => r.Price)
                .OrderBy(p => p)
                .ToList();

            if (prices.Count == 0) return null;

            // Return median price
            var median = prices[prices.Count / 2];
            logger.LogInformation("ML reference price for '{Model}': R${Price:N2} ({Count} listings)",
                productModel, median, prices.Count);

            return Math.Round(median, 2);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not fetch Mercado Livre price for '{Model}'", productModel);
            return null;
        }
    }

    private sealed class MlSearchResponse
    {
        [JsonPropertyName("results")]
        public List<MlItem>? Results { get; set; }
    }

    private sealed class MlItem
    {
        [JsonPropertyName("price")]
        public decimal Price { get; set; }
    }
}
