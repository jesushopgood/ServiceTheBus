using MediatR;
using OrderService.Application.Features.Products.Models;

namespace OrderService.Application.Features.Products.GetById;

public sealed record GetProductByIdQuery(int Id) : IRequest<ProductDto?>;
