namespace OrderService.Application.Features.Orders.Models;

public sealed record OrderDto(
    Guid OrderId,
    DateTime OrderDate,
    int TotalItems,
    decimal TotalPrice,
    IReadOnlyList<OrderItemDto> Items);
