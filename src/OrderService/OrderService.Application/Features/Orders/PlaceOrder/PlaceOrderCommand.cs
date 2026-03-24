using MediatR;
using OrderService.Application.Features.Orders.Models;

namespace OrderService.Application.Features.Orders.PlaceOrder;

public sealed record PlaceOrderCommand(int TotalItems) : IRequest<OrderDto>;
