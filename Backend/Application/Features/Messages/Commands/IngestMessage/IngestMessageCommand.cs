using MediatR;
using WhatsAppParser.Application.Common;

namespace WhatsAppParser.Application.Features.Messages.Commands.IngestMessage;

public sealed record IngestMessageCommand(
    string RawText,
    string? SupplierName,
    string? SupplierPhoneNumber
) : IRequest<Result<IngestMessageResponse>>;

public sealed record IngestMessageResponse(string Message, int ParsedCount);
