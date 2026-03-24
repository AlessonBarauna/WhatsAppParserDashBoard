using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WhatsAppParser.Application.Common;
using WhatsAppParser.Application.Interfaces;

namespace WhatsAppParser.Application.Features.Chat.Commands;

public sealed class ChatCommandHandler(
    IApplicationDbContext db,
    IPricingEngine pricingEngine,
    IAiAssistant aiAssistant,
    ILogger<ChatCommandHandler> logger)
    : MediatR.IRequestHandler<ChatCommand, Result<string>>
{
    public async Task<Result<string>> Handle(ChatCommand request, CancellationToken cancellationToken)
    {
        var systemPrompt = await BuildSystemPromptAsync(cancellationToken);

        var messages = request.History
            .Append(new ChatTurn("user", request.UserMessage))
            .ToList();

        try
        {
            var reply = await aiAssistant.CompleteAsync(systemPrompt, messages, cancellationToken);
            return Result<string>.Success(reply);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao chamar AI assistant");
            return Result<string>.Failure("Falha ao se comunicar com a IA. Verifique a chave de API.");
        }
    }

    private async Task<string> BuildSystemPromptAsync(CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddDays(-7);

        // Catalog: latest price per supplier + product (last 7 days)
        var histories = await db.PriceHistories
            .Include(h => h.Product)
            .Include(h => h.Supplier)
            .Where(h => h.DateLogged >= since)
            .OrderByDescending(h => h.DateLogged)
            .Take(300)
            .ToListAsync(ct);

        var catalog = histories
            .GroupBy(h => new { h.SupplierId, h.ProductId })
            .Select(g => g.OrderByDescending(h => h.DateLogged).First())
            .OrderBy(h => h.Product.Model)
            .ThenBy(h => h.Supplier?.Name ?? "Desconhecido")
            .ToList();

        var insights = (await pricingEngine.GetInsightsAsync()).ToList();

        var suppliers = await db.Suppliers
            .Include(s => s.RawMessages)
            .Include(s => s.PriceHistories)
            .OrderByDescending(s => s.ReliabilityScore)
            .Take(20)
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("Você é um assistente de negócios especializado em revenda de smartphones no Brasil.");
        sb.AppendLine("Responda sempre em português brasileiro de forma direta e objetiva.");
        sb.AppendLine("Use formatação markdown quando útil (tabelas, listas, negrito).");
        sb.AppendLine();

        // Catalog section
        sb.AppendLine("## Catálogo de Fornecedores (últimos 7 dias)");
        sb.AppendLine();

        if (catalog.Count == 0)
        {
            sb.AppendLine("*(Nenhum produto registrado nos últimos 7 dias)*");
        }
        else
        {
            sb.AppendLine("| Modelo | Storage | Condição | Preço | Fornecedor | Data |");
            sb.AppendLine("|--------|---------|----------|-------|------------|------|");
            foreach (var h in catalog)
            {
                var supplier = h.Supplier?.Name ?? "Desconhecido";
                var model = h.Product.Model;
                var storage = h.Product.StorageCapacity ?? "—";
                var condition = h.Product.Condition.ToString();
                var price = h.Price.ToString("C2", new System.Globalization.CultureInfo("pt-BR"));
                var date = h.DateLogged.ToString("dd/MM");
                sb.AppendLine($"| {model} | {storage} | {condition} | {price} | {supplier} | {date} |");
            }
        }

        sb.AppendLine();

        // Insights section
        sb.AppendLine("## Análise de Preços (últimos 30 dias)");
        sb.AppendLine();

        if (insights.Count == 0)
        {
            sb.AppendLine("*(Sem dados suficientes para análise)*");
        }
        else
        {
            sb.AppendLine("| Modelo | Storage | Menor Preço | Preço Médio | Sugestão Revenda | Lucro Est. | Qtd |");
            sb.AppendLine("|--------|---------|-------------|-------------|-----------------|------------|-----|");
            var culture = new System.Globalization.CultureInfo("pt-BR");
            foreach (var i in insights)
            {
                var storage = i.StorageCapacity ?? "—";
                var low = i.LowestPrice.ToString("C0", culture);
                var avg = i.AveragePrice.ToString("C0", culture);
                var suggest = i.SuggestedResalePrice.ToString("C0", culture);
                var profit = i.ProfitMargin.ToString("C0", culture);
                sb.AppendLine($"| {i.Model} | {storage} | {low} | {avg} | {suggest} | {profit} | {i.ListingCount} |");
            }
        }

        sb.AppendLine();

        // Suppliers section
        sb.AppendLine("## Fornecedores Cadastrados");
        sb.AppendLine();

        if (suppliers.Count == 0)
        {
            sb.AppendLine("*(Nenhum fornecedor cadastrado ainda)*");
        }
        else
        {
            sb.AppendLine("| Nome | Score | Total Mensagens | Produtos Registrados |");
            sb.AppendLine("|------|-------|----------------|---------------------|");
            foreach (var s in suppliers)
            {
                sb.AppendLine($"| {s.Name} | {s.ReliabilityScore}/100 | {s.RawMessages.Count} | {s.PriceHistories.Count} |");
            }
        }

        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine("Com base nestes dados, ajude o usuário a:");
        sb.AppendLine("- Identificar os melhores preços e fornecedores para cada produto");
        sb.AppendLine("- Calcular preços de revenda com margem personalizada");
        sb.AppendLine("- Redigir mensagens de venda para clientes");
        sb.AppendLine("- Comparar produtos e condições");
        sb.AppendLine("- Analisar confiabilidade dos fornecedores");

        return sb.ToString();
    }
}
