using OrderService.Application.Common.Interfaces;
using OrderService.Domain.Entities;
using OrderService.Infrastructure.Persistence;
using OrderService.Infrastructure.Persistence.Entities;

namespace OrderService.Infrastructure.Repositories;

public sealed class EfOrderRepository(OrderServiceDbContext dbContext) : IOrderRepository
{
    public async Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        var entity = new OrderEntity
        {
            OrderId = order.OrderId,
            OrderDate = order.OrderDate,
            TotalPrice = order.TotalPrice,
            Items = order.Items.Select(item => new OrderItemEntity
            {
                OrderId = order.OrderId,
                Sku = item.Sku,
                Description = item.Description,
                BasePrice = item.BasePrice,
                Quantity = item.Quantity,
                LineTotal = item.LineTotal
            }).ToList()
        };

        await dbContext.Orders.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
