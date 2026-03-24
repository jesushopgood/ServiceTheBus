using common.Messaging;
using SupplierENG.Application.Common.Interfaces;

namespace SupplierENG.Infrastructure.Messaging;

public sealed class QueueBackedQuotePublisher(IQueueService queueService) : IQuotePublisher
{
    public Task PublishAsync(SupplierQuoteMessage quote, CancellationToken cancellationToken)
    {
        return queueService.PublishSupplierQuoteAsync(quote, cancellationToken);
    }
}
