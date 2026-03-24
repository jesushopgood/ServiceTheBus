using common.Messaging;
using OrderService.Application.Features.Orders.Models;

namespace OrderService.Application.Common.Interfaces;

public interface IOrderQueuePublisher
{
    Task PublishAsync(OrderPlacedMessage order, CancellationToken cancellationToken);
}
