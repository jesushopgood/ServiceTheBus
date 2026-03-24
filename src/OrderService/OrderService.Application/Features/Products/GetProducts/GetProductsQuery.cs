using MediatR;
using OrderService.Application.Features.Products.Models;

namespace OrderService.Application.Features.Products.GetProducts;

public sealed record GetProductsQuery : IRequest<IReadOnlyList<ProductDto>>;
