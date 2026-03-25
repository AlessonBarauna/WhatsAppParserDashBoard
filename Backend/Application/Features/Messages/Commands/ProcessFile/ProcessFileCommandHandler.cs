using System.Text.RegularExpressions;
using MediatR;
using Microsoft.Extensions.Logging;
using WhatsAppParser.Application.Common;
using WhatsAppParser.Application.Features.Messages.Commands.IngestMessage;

namespace WhatsAppParser.Application.Features.Messages.Commands.ProcessFile;

public sealed partial class ProcessFileCommandHandler(ISender sender, ILogger<ProcessFileCommandHandler> logger)
    : IRequestHandler<ProcessFileCommand, Result<ProcessFileResponse>>
{
    // [01/12/2024, 14:30:00] Sender Name: message text
    [GeneratedRegex(@"^\[(\d{1,2}/\d{1,2}/\d{2,4}),\s+\d{1,2}:\d{2}(?::\d{2})?(?:\s*[AP]M)?\]\s+([^:]+):\s+(.+)$")]
    private static partial Regex FormatBracketsRegex();

    // 01/12/2024 14:30 - Sender Name: message text
    [GeneratedRegex(@"^(\d{1,2}/\d{1,2}/\d{2,4})\s+\d{1,2}:\d{2}(?:\s*[AP]M)?\s+-\s+([^:]+):\s+(.+)$")]
    private static partial Regex FormatDashRegex();

    public async Task<Result<ProcessFileResponse>> Handle(ProcessFileCommand request, CancellationToken cancellationToken)
    {
        var messages = ParseWhatsAppExport(request.FileContent);

        if (messages.Count == 0)
            return Result<ProcessFileResponse>.Failure("No WhatsApp messages could be parsed from the file.");

        int totalProductsParsed = 0;
        int failed = 0;

        foreach (var (senderName, text) in messages)
        {
            // SupplierName overrides per-message sender (community/business exports)
            var resolvedSupplier = request.SupplierName;
            var command = new IngestMessageCommand(text, resolvedSupplier, null);
            var result = await sender.Send(command, cancellationToken);

            if (result.IsSuccess)
                totalProductsParsed += result.Value!.ParsedCount;
            else
            {
                failed++;
                logger.LogWarning("Failed to ingest message from {Sender}: {Error}", senderName, result.Error);
            }
        }

        logger.LogInformation(
            "File processed: {Total} messages, {Products} products parsed, {Failed} failures",
            messages.Count, totalProductsParsed, failed);

        return Result<ProcessFileResponse>.Success(
            new ProcessFileResponse(messages.Count, totalProductsParsed, failed));
    }

    // Nomes do lado "você" em exportações do WhatsApp — ignorar essas mensagens
    private static readonly HashSet<string> OwnerNames = new(StringComparer.OrdinalIgnoreCase)
        { "Você", "Voce", "Vocé", "You", "you" };

    private static List<(string Sender, string Text)> ParseWhatsAppExport(string fileContent)
    {
        var result = new List<(string, string)>();
        string? currentSender = null;
        var currentLines = new List<string>();

        foreach (var line in fileContent.Split('\n'))
        {
            var trimmed = line.TrimEnd('\r');

            var match = FormatBracketsRegex().Match(trimmed)
                        is { Success: true } m1 ? m1
                        : FormatDashRegex().Match(trimmed) is { Success: true } m2 ? m2
                        : null;

            if (match is not null)
            {
                // Flush previous message (skip file-owner messages)
                if (currentSender is not null && currentLines.Count > 0
                    && !OwnerNames.Contains(currentSender))
                    result.Add((currentSender, string.Join("\n", currentLines)));

                // Format 1 has 3 groups; both formats have sender as group 2 and text as group 3
                currentSender = match.Groups[2].Value.Trim();
                currentLines = [match.Groups[3].Value.Trim()];
            }
            else if (currentSender is not null && !string.IsNullOrWhiteSpace(trimmed))
            {
                // Continuation of a multi-line message
                currentLines.Add(trimmed);
            }
        }

        // Flush last message (skip file-owner messages)
        if (currentSender is not null && currentLines.Count > 0
            && !OwnerNames.Contains(currentSender))
            result.Add((currentSender, string.Join("\n", currentLines)));

        return result;
    }
}
