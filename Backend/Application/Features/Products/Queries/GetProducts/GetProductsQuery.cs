using MediatR;
using WhatsAppParser.Application.Common;

namespace WhatsAppParser.Application.Features.Products.Queries.GetProducts;

public sealed record GetProductsQuery : IRequest<Result<IReadOnlyList<ProductDto>>>;
