using AutoMapper;
using MediatR;
using OrderService.Application.Common.Interfaces;
using OrderService.Application.Features.Products.Models;

namespace OrderService.Application.Features.Products.GetById;

public sealed class GetProductByIdQueryHandler(IProductRepository productRepository, IMapper mapper)
    : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(request.Id, cancellationToken);
        return product is null ? null : mapper.Map<ProductDto>(product);
    }
}
