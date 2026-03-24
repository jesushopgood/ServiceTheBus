using common.Messaging;
using OrderService.Application.Common.Interfaces;

namespace OrderService.Infrastructure.Messaging;

public sealed class QueueBackedOrderQueuePublisher(IQueueService queueService) : IOrderQueuePublisher
{
    public Task PublishAsync(OrderPlacedMessage order, CancellationToken cancellationToken)
    {
        return queueService.PublishOrderAsync(order, cancellationToken);
    }
}
