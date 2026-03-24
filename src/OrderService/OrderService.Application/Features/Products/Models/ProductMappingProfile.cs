using AutoMapper;
using OrderService.Domain.Entities;

namespace OrderService.Application.Features.Products.Models;

public sealed class ProductMappingProfile : Profile
{
    public ProductMappingProfile()
    {
        CreateMap<Product, ProductDto>();
    }
}
