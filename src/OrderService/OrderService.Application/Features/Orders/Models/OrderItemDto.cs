namespace OrderService.Application.Features.Orders.Models;

public sealed record OrderItemDto(
    string Sku,
    string Description,
    decimal BasePrice,
    int Quantity,
    decimal LineTotal);
