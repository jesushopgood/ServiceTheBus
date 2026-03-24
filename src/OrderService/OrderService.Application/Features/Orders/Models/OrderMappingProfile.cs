using AutoMapper;
using common.Messaging;
using OrderService.Domain.Entities;

namespace OrderService.Application.Features.Orders.Models;

public sealed class OrderMappingProfile : Profile
{
    public OrderMappingProfile()
    {
        CreateMap<Order, OrderDto>()
            .ForCtorParam(nameof(OrderDto.TotalItems), opt => opt.MapFrom(src => src.Items.Count));

        CreateMap<OrderItem, OrderItemDto>();
        CreateMap<OrderDto, OrderPlacedMessage>();
        CreateMap<OrderItemDto, OrderPlacedItemMessage>();
    }
}
