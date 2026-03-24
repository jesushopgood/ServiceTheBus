using OrderService.Domain.Entities;

namespace OrderService.Application.Common.Interfaces;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken cancellationToken);
}
