using AutoMapper;
using MediatR;
using OrderService.Application.Common.Interfaces;
using OrderService.Application.Features.Products.Models;

namespace OrderService.Application.Features.Products.GetProducts;

public sealed class GetProductsQueryHandler(IProductRepository productRepository, IMapper mapper)
    : IRequestHandler<GetProductsQuery, IReadOnlyList<ProductDto>>
{
    public async Task<IReadOnlyList<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await productRepository.GetAllAsync(cancellationToken);
        return mapper.Map<IReadOnlyList<ProductDto>>(products);
    }
}
