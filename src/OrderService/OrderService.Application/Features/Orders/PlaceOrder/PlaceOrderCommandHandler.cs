using AutoMapper;
using common.Messaging;
using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Common.Interfaces;
using OrderService.Application.Features.Orders.Models;
using OrderService.Domain.Entities;

namespace OrderService.Application.Features.Orders.PlaceOrder;

public sealed class PlaceOrderCommandHandler(
    IProductRepository productRepository,
    IOrderRepository orderRepository,
    IOrderQueuePublisher orderQueuePublisher,
    IMapper mapper,
    ILogger<PlaceOrderCommandHandler> logger)
    : IRequestHandler<PlaceOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation($"PlaceOrderCommandHandler::Handle");
        var products = await productRepository.GetAllAsync(cancellationToken);

        var selectedProducts = products
            .OrderBy(_ => Random.Shared.Next())
            .Take(request.TotalItems)
            .ToList();

        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            Items = selectedProducts.Select(product => new OrderItem
            {
                Sku = product.Sku,
                Description = product.Description,
                BasePrice = product.BasePrice,
                Quantity = Random.Shared.Next(1, 11)
            }).ToList()
        };

        var orderDto = mapper.Map<OrderDto>(order);
        var orderPlacedMessage = mapper.Map<OrderPlacedMessage>(orderDto);
        await orderRepository.AddAsync(order, cancellationToken);
        logger.LogInformation($"PlaceOrderCommandHandler::Order Placed");
        await orderQueuePublisher.PublishAsync(orderPlacedMessage, cancellationToken);
        logger.LogInformation($"PlaceOrderCommandHandler::Added to Queue");


        return orderDto;
    }
}
