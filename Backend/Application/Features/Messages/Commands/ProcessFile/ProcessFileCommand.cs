using MediatR;
using WhatsAppParser.Application.Common;

namespace WhatsAppParser.Application.Features.Messages.Commands.ProcessFile;

public sealed record ProcessFileCommand(string FileContent) : IRequest<Result<ProcessFileResponse>>;

public sealed record ProcessFileResponse(int TotalMessages, int TotalProductsParsed, int FailedMessages);
